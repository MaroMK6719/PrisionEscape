using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Platybytes.Comun
{
    // Menú principal muy sencillo. Se construye todo por código: basta con un objeto vacío con este componente
    // (lo crea el menú del editor "Platybytes > Menú > Crear escena del menú").
    public class MenuPrincipal : MonoBehaviour
    {
        static readonly Color Fondo = new Color32(35, 38, 43, 255);
        static readonly Color Naranja = new Color32(242, 140, 40, 255);
        static readonly Color Amarillo = new Color32(255, 201, 60, 255);
        static readonly Color Rojo = new Color32(230, 57, 70, 255);
        static readonly Color Gris = new Color32(58, 63, 71, 255);
        static readonly Color Oscuro = new Color32(30, 30, 30, 255);

        Font fuente;
        Transform raiz;

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CrearCamara();
            CrearCanvas();

            // Título
            Texto("PLATYBYTES PRESENTA", 40, Color.white, new Vector2(0, 760), new Vector2(1000, 60));
            Texto("PRISON\nESCAPE", 180, Naranja, new Vector2(0, 540), new Vector2(1000, 380));

            // Resultado de la última partida
            if (FlujoPartida.HayResultado)
            {
                string titulo = FlujoPartida.UltimaFugaConseguida ? "¡HAS ESCAPADO!" : "¡PILLADO!";
                Texto(titulo, 80, FlujoPartida.UltimaFugaConseguida ? Amarillo : Rojo, new Vector2(0, 290), new Vector2(1000, 110));
                string detalle = "Puntos: " + FlujoPartida.UltimosPuntos + (FlujoPartida.UltimoRecordNuevo ? "   ¡NUEVO RÉCORD!" : "");
                Texto(detalle, 48, Color.white, new Vector2(0, 200), new Vector2(1000, 70));
            }
            else
            {
                Texto("Récord: " + FlujoPartida.Record + " pts", 48, Amarillo, new Vector2(0, 240), new Vector2(1000, 70));
            }

            // Botones
            Boton("JUGAR", Amarillo, Oscuro, 84, new Vector2(0, 30), new Vector2(760, 190), FlujoPartida.EmpezarPartida);

            Texto("Probar una prueba suelta", 38, new Color(1, 1, 1, 0.7f), new Vector2(0, -130), new Vector2(1000, 60));
            Boton("1 · Press the Button!", Gris, Color.white, 52, new Vector2(0, -240), new Vector2(760, 140),
                () => FlujoPartida.ProbarMinijuego(FlujoPartida.EscenaPressTheButton));
            Boton("2 · Rescata la llave", Gris, Color.white, 52, new Vector2(0, -400), new Vector2(760, 140),
                () => FlujoPartida.ProbarMinijuego(FlujoPartida.EscenaRescataLaLlave));

            Boton("SALIR", Oscuro, Color.white, 48, new Vector2(0, -640), new Vector2(420, 120), Salir);
        }

        void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------- Ayudas para construir la UI ----------------

        void CrearCamara()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Fondo;
            cam.orthographic = true;
        }

        void CrearCanvas()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            GameObject go = new GameObject("Canvas");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler escalador = go.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1080, 1920);
            escalador.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            raiz = go.transform;

            Image fondo = new GameObject("Fondo", typeof(RectTransform)).AddComponent<Image>();
            fondo.transform.SetParent(raiz, false);
            fondo.color = Fondo;
            RectTransform rt = fondo.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Text Texto(string contenido, int tamano, Color color, Vector2 posicion, Vector2 medida)
        {
            Text t = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(raiz, false);
            t.font = fuente;
            t.text = contenido;
            t.fontSize = tamano;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.rectTransform.anchoredPosition = posicion;
            t.rectTransform.sizeDelta = medida;
            return t;
        }

        void Boton(string etiqueta, Color colorFondo, Color colorTexto, int tamano, Vector2 posicion, Vector2 medida, UnityAction alPulsar)
        {
            Image img = new GameObject("Boton " + etiqueta, typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(raiz, false);
            img.color = colorFondo;
            img.rectTransform.anchoredPosition = posicion;
            img.rectTransform.sizeDelta = medida;

            Button boton = img.gameObject.AddComponent<Button>();
            boton.onClick.AddListener(alPulsar);

            Text t = Texto(etiqueta, tamano, colorTexto, Vector2.zero, medida);
            t.transform.SetParent(img.transform, false);
            t.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
