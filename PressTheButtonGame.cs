using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;
using CanvasScaler = UnityEngine.UI.CanvasScaler;
using GraphicRaycaster = UnityEngine.UI.GraphicRaycaster;
using Image = UnityEngine.UI.Image;
using Outline = UnityEngine.UI.Outline;

namespace Platybytes.Minijuegos
{
    public enum Direction { Up = 0, Left = 1, Right = 2, Down = 3 }

    /// <summary>
    /// PLATYBYTES - Minijuego 6.1 "Press the Button!" (prueba de concepto).
    /// Arriba aparece una flecha: hay que pulsar la MISMA dirección en la cruceta de abajo,
    /// ignorando el color de la flecha. Un fallo = alarma = pierdes.
    /// Se construye toda la UI por código: basta con añadir este componente a un GameObject vacío.
    /// </summary>
    [DisallowMultipleComponent]
    public class PressTheButtonGame : MonoBehaviour
    {
        enum State { Intro, Playing, Over }

        class ArrowSlot
        {
            public RectTransform Root;
            public Image Fill;
        }

        [Header("Reglas (GDD 6.1)")]
        [SerializeField] float timeLimit = 60f;
        [Tooltip("Aciertos necesarios para desactivar el panel. El GDD no fija el número: ajústalo.")]
        [SerializeField] int targetScore = 30;
        [Tooltip("Si está activo, tardar demasiado en una flecha cuenta como error. El GDD no lo especifica.")]
        [SerializeField] bool timeoutIsError = true;

        [Header("Dificultad")]
        [Tooltip("Segundos para responder a cada flecha al empezar.")]
        [SerializeField] float startReactionTime = 2.2f;
        [Tooltip("Segundos para responder a cada flecha en la dificultad máxima.")]
        [SerializeField] float minReactionTime = 0.7f;
        [Tooltip("Probabilidad máxima de que el color de la flecha engañe (sea el de otra dirección).")]
        [Range(0f, 1f)] [SerializeField] float maxColorMismatch = 0.85f;

        [Header("Opcional: arte propio")]
        [Tooltip("Sprite de flecha apuntando hacia ARRIBA. Si está vacío se genera uno por código.")]
        [SerializeField] Sprite arrowSprite;

        /// <summary>Se lanza al terminar: (victoria, puntuación). Para enlazarlo con el flujo de minijuegos.</summary>
        public event System.Action<bool, int> Finished;
        /// <summary>Se lanza al pulsar "Siguiente prueba" tras ganar.</summary>
        public event System.Action NextRequested;

        // ---- Constantes y estilo (paleta base del GDD) ----
        const string RecordKey = "PressTheButton.Record";
        const int QueueSize = 3; // flecha actual + 2 de previsualización

        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
        static readonly Vector2 PadAnchor = new Vector2(0.5f, 0.26f);
        static readonly Vector2 StripAnchor = new Vector2(0.35f, 0.68f);

        static readonly Color[] PadColors =
        {
            new Color32(230, 57, 70, 255),   // Arriba  - rojo
            new Color32(76, 175, 80, 255),   // Izquierda - verde
            new Color32(45, 93, 216, 255),   // Derecha - azul
            new Color32(255, 195, 60, 255),  // Abajo   - amarillo
        };
        static readonly Color OutlineColor = new Color32(30, 30, 30, 255);
        static readonly Color BackgroundColor = new Color32(43, 48, 56, 255);
        static readonly Color PanelColor = new Color32(88, 94, 104, 255);
        static readonly Color WinOverlay = new Color(0.04f, 0.25f, 0.12f, 0.96f);
        static readonly Color LoseOverlay = new Color(0.30f, 0.04f, 0.07f, 0.96f);

        // ---- Estado ----
        State state = State.Intro;
        int score;
        int record;
        float timeLeft;
        float reactionLeft;
        float reactionWindow;
        int shownSeconds = -1;

        readonly Direction[] queueDir = new Direction[QueueSize];
        readonly Color[] queueColor = new Color[QueueSize];
        readonly ArrowSlot[] slots = new ArrowSlot[QueueSize];

        // ---- Referencias a la UI creada por código ----
        TextMeshProUGUI scoreText, timeText, recordText;
        GameObject barRoot;
        RectTransform barFill;
        Image barFillImage;
        GameObject introPanel, resultPanel;
        Image resultBg;
        TextMeshProUGUI resultTitle, resultMessage, resultScore;
        Button nextButton;

        // =====================================================================
        //  Ciclo de vida
        // =====================================================================

        void Awake()
        {
            Application.targetFrameRate = 60;
            record = PlayerPrefs.GetInt(RecordKey, 0);

            // Se comprueba "instance" primero: si faltan los TMP Essentials, acceder a
            // defaultFontAsset sin esta comprobación lanzaría una NullReferenceException.
            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("PressTheButton: faltan los TMP Essentials. " +
                               "Importa: Window > TextMeshPro > Import TMP Essential Resources " +
                               "y vuelve a pulsar Play.");
                enabled = false;
                return;
            }

            if (arrowSprite == null) arrowSprite = MakeArrowSprite();

            BuildUI();
            ShowIntro();
        }

        void Update()
        {
            if (state != State.Playing) return;

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                RefreshTime();
                Finish(false, "¡SE ACABÓ EL TIEMPO!", "No has conseguido desactivar el panel a tiempo.");
                return;
            }

            if (timeoutIsError)
            {
                reactionLeft -= Time.deltaTime;
                if (reactionLeft <= 0f)
                {
                    Finish(false, "¡DEMASIADO LENTO!",
                        "Has tardado demasiado. La alarma se ha activado y los guardias te han descubierto.");
                    return;
                }
                UpdateReactionBar();
            }

            RefreshTime();
        }

        // =====================================================================
        //  Lógica de juego
        // =====================================================================

        void ShowIntro()
        {
            state = State.Intro;
            timeLeft = timeLimit;
            shownSeconds = -1;
            RefreshTime();
            RefreshScore();
            introPanel.SetActive(true);
            resultPanel.SetActive(false);
        }

        void StartRound()
        {
            score = 0;
            timeLeft = timeLimit;
            shownSeconds = -1;
            reactionWindow = startReactionTime;
            reactionLeft = reactionWindow;

            for (int i = 0; i < QueueSize; i++) Enqueue(i);

            introPanel.SetActive(false);
            resultPanel.SetActive(false);
            barRoot.SetActive(timeoutIsError);

            RefreshSlots();
            RefreshScore();
            RefreshTime();
            UpdateReactionBar();

            state = State.Playing;
        }

        void OnPadPressed(Direction pressed)
        {
            if (state != State.Playing) return;

            if (pressed != queueDir[0])
            {
                Finish(false, "¡ERROR!",
                    "Has pulsado una dirección incorrecta. La alarma se ha activado y los guardias te han descubierto.");
                return;
            }

            score++;
            RefreshScore();

            if (score >= targetScore)
            {
                Finish(true, "¡PANEL DESACTIVADO!", "Has completado la prueba. La puerta se ha desbloqueado.");
                return;
            }

            Advance();
        }

        void Advance()
        {
            for (int i = 0; i < QueueSize - 1; i++)
            {
                queueDir[i] = queueDir[i + 1];
                queueColor[i] = queueColor[i + 1];
            }
            Enqueue(QueueSize - 1);

            reactionWindow = Mathf.Lerp(startReactionTime, minReactionTime, Difficulty01());
            reactionLeft = reactionWindow;

            RefreshSlots();
            UpdateReactionBar();
        }

        void Finish(bool won, string title, string message)
        {
            state = State.Over;

            if (score > record)
            {
                record = score;
                PlayerPrefs.SetInt(RecordKey, record);
                PlayerPrefs.Save();
            }
            RefreshScore();

            resultBg.color = won ? WinOverlay : LoseOverlay;
            resultTitle.text = title;
            resultTitle.color = won ? new Color(0.6f, 1f, 0.7f) : Color.white;
            resultMessage.text = message;
            resultScore.text = "PUNTUACIÓN  " + score + "     RÉCORD  " + record;
            nextButton.gameObject.SetActive(won);
            resultPanel.SetActive(true);

            Finished?.Invoke(won, score);
        }

        void OnNextPressed()
        {
            Debug.Log("PressTheButton: se ha pedido la siguiente prueba (aún no hay flujo de minijuegos enlazado).");
            NextRequested?.Invoke();
        }

        float Difficulty01()
        {
            return Mathf.Clamp01(score / (float)Mathf.Max(1, targetScore));
        }

        // Genera una flecha nueva. Con más dificultad, su color es cada vez más a menudo el de OTRA dirección.
        void Enqueue(int index)
        {
            var dir = (Direction)UnityEngine.Random.Range(0, 4);
            int colorIndex = (int)dir;

            if (UnityEngine.Random.value < Difficulty01() * maxColorMismatch)
            {
                colorIndex = (colorIndex + UnityEngine.Random.Range(1, 4)) % 4;
            }

            queueDir[index] = dir;
            queueColor[index] = PadColors[colorIndex];
        }

        // =====================================================================
        //  Refresco de la UI
        // =====================================================================

        void RefreshSlots()
        {
            for (int i = 0; i < QueueSize; i++)
            {
                Color c = queueColor[i];
                if (i > 0) c.a = 0.65f;
                slots[i].Fill.color = c;
                slots[i].Root.localRotation = Quaternion.Euler(0f, 0f, RotationFor(queueDir[i]));
            }
        }

        void RefreshScore()
        {
            scoreText.text = "PUNTOS\n" + score;
            recordText.text = "RÉCORD\n" + record;
        }

        void RefreshTime()
        {
            int s = Mathf.CeilToInt(timeLeft);
            if (s == shownSeconds) return;
            shownSeconds = s;
            timeText.text = "TIEMPO\n" + (s / 60) + ":" + (s % 60).ToString("00");
        }

        void UpdateReactionBar()
        {
            float r = Mathf.Clamp01(reactionLeft / Mathf.Max(0.01f, reactionWindow));
            barFill.anchorMax = new Vector2(r, 1f);
            barFillImage.color = r < 0.3f ? PadColors[0] : PadColors[3];
        }

        static float RotationFor(Direction d)
        {
            switch (d)
            {
                case Direction.Left: return 90f;
                case Direction.Down: return 180f;
                case Direction.Right: return -90f;
                default: return 0f;
            }
        }

        // =====================================================================
        //  Construcción de la UI (uGUI + TextMeshPro)
        // =====================================================================

        void BuildUI()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("PressTheButton_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f); // móvil en vertical
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            // Fondo y panel de seguridad metálico
            AddImage(Stretch("Background", root), BackgroundColor);
            var panel = NewRect("SecurityPanel", root, PadAnchor, Vector2.zero, new Vector2(1000f, 1000f));
            AddImage(panel, PanelColor);
            AddOutline(panel.gameObject, 8f);

            // HUD: puntos arriba a la izquierda, tiempo en el centro, récord arriba a la derecha
            scoreText = NewText("ScoreText", root, new Vector2(0f, 1f), new Vector2(40f, -60f), new Vector2(380f, 170f),
                "", 54f, TextAlignmentOptions.TopLeft, Color.white, new Vector2(0f, 1f));
            timeText = NewText("TimeText", root, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(380f, 170f),
                "", 54f, TextAlignmentOptions.Top, Color.white, new Vector2(0.5f, 1f));
            recordText = NewText("RecordText", root, new Vector2(1f, 1f), new Vector2(-40f, -60f), new Vector2(380f, 170f),
                "", 54f, TextAlignmentOptions.TopRight, PadColors[3], new Vector2(1f, 1f));

            // Pista breve
            NewText("Hint", root, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(960f, 140f),
                "Pulsa la dirección de la flecha.\n¡Ignora su color!", 46f, TextAlignmentOptions.Center,
                new Color(0.9f, 0.9f, 0.9f));

            // Tira de flechas: actual (grande) + 2 siguientes
            float[] sizes = { 340f, 170f, 120f };
            float[] xs = { 0f, 305f, 485f };
            for (int i = 0; i < QueueSize; i++)
            {
                slots[i] = CreateArrow("Arrow_" + i, root, StripAnchor, new Vector2(xs[i], 0f), sizes[i], Color.white);
            }

            // Barra de tiempo de reacción
            var bar = NewRect("ReactionBar", root, new Vector2(0.5f, 0.565f), Vector2.zero, new Vector2(760f, 28f));
            AddImage(bar, OutlineColor);
            barRoot = bar.gameObject;
            var fill = Stretch("Fill", bar);
            barFillImage = AddImage(fill, PadColors[3]);
            barFill = fill;

            // Cruceta de cuatro colores
            CreatePadButton(root, Direction.Up, new Vector2(0f, 310f));
            CreatePadButton(root, Direction.Left, new Vector2(-310f, 0f));
            CreatePadButton(root, Direction.Right, new Vector2(310f, 0f));
            CreatePadButton(root, Direction.Down, new Vector2(0f, -310f));

            BuildIntroPanel(root);
            BuildResultPanel(root);
        }

        void BuildIntroPanel(Transform root)
        {
            var rt = Stretch("IntroPanel", root);
            AddImage(rt, new Color(0.08f, 0.09f, 0.12f, 0.96f), null, true); // bloquea toques al fondo
            introPanel = rt.gameObject;

            NewText("Title", rt, Mid, new Vector2(0f, 520f), new Vector2(980f, 260f),
                "¡PULSA EL BOTÓN!", 120f, TextAlignmentOptions.Center, PadColors[3]);
            NewText("Subtitle", rt, Mid, new Vector2(0f, 300f), new Vector2(900f, 200f),
                "Desactiva el sistema de seguridad para seguir con tu fuga.", 54f,
                TextAlignmentOptions.Center, Color.white);
            NewText("Instructions", rt, Mid, new Vector2(0f, 20f), new Vector2(900f, 360f),
                "Instrucciones:\n• Fíjate en la dirección de la flecha de arriba.\n• Ignora su color.\n" +
                "• Pulsa la misma dirección en la cruceta de abajo.", 50f,
                TextAlignmentOptions.MidlineLeft, Color.white);
            NewButton("StartButton", rt, new Vector2(0f, -420f), new Vector2(760f, 200f),
                PadColors[3], "¡EMPEZAR!", 80f, OutlineColor, StartRound);
        }

        void BuildResultPanel(Transform root)
        {
            var rt = Stretch("ResultPanel", root);
            resultBg = AddImage(rt, LoseOverlay, null, true);
            resultPanel = rt.gameObject;

            resultTitle = NewText("Title", rt, Mid, new Vector2(0f, 480f), new Vector2(980f, 260f),
                "", 100f, TextAlignmentOptions.Center, Color.white);
            resultMessage = NewText("Message", rt, Mid, new Vector2(0f, 190f), new Vector2(900f, 300f),
                "", 54f, TextAlignmentOptions.Center, Color.white);
            resultScore = NewText("Score", rt, Mid, new Vector2(0f, -80f), new Vector2(980f, 160f),
                "", 60f, TextAlignmentOptions.Center, PadColors[3]);
            NewButton("RetryButton", rt, new Vector2(0f, -330f), new Vector2(760f, 180f),
                PadColors[3], "REINTENTAR", 70f, OutlineColor, StartRound);
            nextButton = NewButton("NextButton", rt, new Vector2(0f, -560f), new Vector2(760f, 180f),
                PadColors[1], "SIGUIENTE PRUEBA", 62f, Color.white, OnNextPressed);
        }

        void CreatePadButton(Transform parent, Direction dir, Vector2 offset)
        {
            var rt = NewRect("Pad_" + dir, parent, PadAnchor, offset, new Vector2(300f, 300f));
            var img = AddImage(rt, PadColors[(int)dir], null, true);
            AddOutline(rt.gameObject);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnPadPressed(dir));

            var arrow = CreateArrow("Arrow", rt, Mid, Vector2.zero, 170f, Color.white);
            arrow.Root.localRotation = Quaternion.Euler(0f, 0f, RotationFor(dir));
        }

        // Flecha con contorno grueso (estilo cartoon del GDD)
        ArrowSlot CreateArrow(string name, Transform parent, Vector2 anchor, Vector2 pos, float size, Color fillColor)
        {
            var root = NewRect(name, parent, anchor, pos, new Vector2(size, size));
            var outline = NewRect("Outline", root, Mid, Vector2.zero, new Vector2(size * 1.14f, size * 1.14f));
            AddImage(outline, OutlineColor, arrowSprite);
            var fill = NewRect("Fill", root, Mid, Vector2.zero, new Vector2(size, size));
            var fillImg = AddImage(fill, fillColor, arrowSprite);
            return new ArrowSlot { Root = root, Fill = fillImg };
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // =====================================================================
        //  Utilidades de UI
        // =====================================================================

        static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? Mid;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image AddImage(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        static void AddOutline(GameObject go, float distance = 6f)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = OutlineColor;
            o.effectDistance = new Vector2(distance, -distance);
        }

        static TextMeshProUGUI NewText(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            string text, float fontSize, TextAlignmentOptions align, Color color, Vector2? pivot = null)
        {
            var rt = NewRect(name, parent, anchor, pos, size, pivot);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.outlineWidth = 0.2f;
            t.outlineColor = OutlineColor;
            return t;
        }

        static Button NewButton(string name, Transform parent, Vector2 pos, Vector2 size, Color color,
            string label, float fontSize, Color labelColor, UnityAction onClick)
        {
            var rt = NewRect(name, parent, Mid, pos, size);
            var img = AddImage(rt, color, null, true);
            AddOutline(rt.gameObject);

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            NewText("Label", rt, Mid, Vector2.zero, size, label, fontSize, TextAlignmentOptions.Center, labelColor);
            return btn;
        }

        // Flecha blanca apuntando hacia arriba, generada por código para no depender de assets.
        static Sprite MakeArrowSprite()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - 63.5f);
                    bool shaft = y >= 8 && y < 64 && dx <= 16f;
                    bool head = y >= 64 && y <= 120 && dx <= (120f - y) * (54f / 56f);
                    pixels[y * size + x] = (shaft || head)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
