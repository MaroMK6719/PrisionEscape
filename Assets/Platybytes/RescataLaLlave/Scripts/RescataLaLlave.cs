using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Platybytes.Llave
{
    // 6.2 Rescata la llave
    // Guiar la bola con la llave hasta la cerradura inclinando el móvil, esquivando a los guardias.
    // 5 laberintos en 60 segundos. Puntos: +12 por laberinto, +4 por estrella, -3 por guardia
    // y bonus por el tiempo sobrante (hasta +20). Máximo 100.
    //
    // Va en un único GameObject de la escena (lo crea el menú "Platybytes > Rescata la llave > Crear escena").
    // Todo lo demás (cámara, laberinto, bola, HUD) se construye por código al empezar.
    public class RescataLaLlave : MonoBehaviour
    {
        // ---------------- Ajustes de diseño (GDD 6.2) ----------------
        const float Duracion = 60f;
        const int PuntosLaberinto = 12;
        const int PuntosEstrella = 4;
        const int PuntosGolpe = -3;
        const int BonusTiempoMaximo = 20;

        // El GDD dice que si se acaba el tiempo se pierden todos los puntos de vida.
        // Ponlo a false si en las pruebas resulta demasiado duro (entonces solo quita una vida).
        static readonly bool TiempoAgotadoQuitaTodasLasVidas = true;

        // Control por inclinación
        const float Fuerza = 22f;            // empuje máximo sobre la bola
        const float Sensibilidad = 2.2f;     // cuanto más alto, menos hay que inclinar
        const float ZonaMuerta = 0.03f;      // ignora temblores pequeños de la mano
        static readonly bool InvertirX = false;  // por si en algún móvil los ejes salen al revés
        static readonly bool InvertirY = false;

        // Inclinación actual (-1..1 en cada eje). La usan la bola y las paredes móviles.
        public static Vector2 Inclinacion { get; private set; }
        public float FuerzaMaxima { get { return Fuerza; } }

        // ---------------- Estado ----------------
        Camera camara;
        Canvas canvas;
        Transform raizNivel;
        Bola bola;
        int nivelActual;
        int anchoLaberinto;
        int altoLaberinto;
        int puntos;
        float tiempoRestante;
        bool jugando;
        bool terminado;
        Vector2 inclinacionNeutra;
        bool calibrado;

        // Valores globales que este minijuego cambia y devuelve al salir (para no afectar a los demás)
        Vector2 gravedadOriginal;
        bool compensacionOriginal;

        // HUD
        Text textoPuntos;
        Text textoTiempo;
        Text textoNivel;
        Image[] iconosVidas;
        Image panelMensaje;
        Text textoMensaje;
        Image destello;

        // ---------------- Ciclo de vida ----------------

        void Start()
        {
            Application.targetFrameRate = 60; // el GDD pide 60 FPS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.fullScreen = true; // en el móvil oculta las barras del sistema

            gravedadOriginal = Physics2D.gravity;
            Physics2D.gravity = Vector2.zero; // la "gravedad" es la inclinación del móvil

            // Que el acelerómetro devuelva los ejes ya girados según la orientación de la pantalla
            compensacionOriginal = InputSystem.settings.compensateForScreenOrientation;
            InputSystem.settings.compensateForScreenOrientation = true;
            if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);

            if (ConexionPartida.Vidas <= 0) ConexionPartida.Vidas = ConexionPartida.VidasMaximas;

            Inclinacion = Vector2.zero;
            tiempoRestante = Duracion;
            // Fondo del mismo color que los muros: si sobra pantalla, parece que el muro exterior llega al borde
            camara = Graficos.CrearCamara(Paleta.Contorno);
            canvas = Graficos.CrearCanvas(false);
            CrearHud();
            ConstruirNivel(0);
            ActualizarHud();
            StartCoroutine(SecuenciaInicio());
        }

        void OnDestroy()
        {
            Physics2D.gravity = gravedadOriginal;
            InputSystem.settings.compensateForScreenOrientation = compensacionOriginal;
            Inclinacion = Vector2.zero;
        }

        IEnumerator SecuenciaInicio()
        {
            MostrarMensaje("¡INCLINA!", Paleta.Recompensa);
            yield return new WaitForSeconds(1.2f);
            OcultarMensaje();
            Calibrar(); // la postura con la que se sujeta el móvil al empezar es la "posición neutra"
            bola.Activar();
            jugando = true;
        }

        void Update()
        {
            if (!jugando) return;

            tiempoRestante -= Time.deltaTime;
            if (tiempoRestante <= 0f)
            {
                tiempoRestante = 0f;
                TiempoAgotado();
            }
            else
            {
                Inclinacion = LeerInclinacion();
            }
            ActualizarHud();
        }

        void LateUpdate()
        {
            AjustarCamara();
        }

        // ---------------- Entrada ----------------

        void Calibrar()
        {
            if (Accelerometer.current != null)
            {
                Vector3 a = Accelerometer.current.acceleration.ReadValue();
                inclinacionNeutra = new Vector2(a.x, a.y);
                calibrado = true;
            }
        }

        Vector2 LeerInclinacion()
        {
            Vector2 valor = Vector2.zero;

            if (Accelerometer.current != null && calibrado)
            {
                Vector3 a = Accelerometer.current.acceleration.ReadValue();
                valor = (new Vector2(a.x, a.y) - inclinacionNeutra) * Sensibilidad;
                if (InvertirX) valor.x = -valor.x;
                if (InvertirY) valor.y = -valor.y;
                if (valor.magnitude < ZonaMuerta) valor = Vector2.zero;
            }

            // Teclado para probar en el ordenador (tiene prioridad si se pulsa)
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                Vector2 teclas = Vector2.zero;
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) teclas.x -= 1;
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) teclas.x += 1;
                if (k.downArrowKey.isPressed || k.sKey.isPressed) teclas.y -= 1;
                if (k.upArrowKey.isPressed || k.wKey.isPressed) teclas.y += 1;
                if (teclas != Vector2.zero) valor = teclas.normalized * 0.7f;
            }

            return Vector2.ClampMagnitude(valor, 1f);
        }

        // ---------------- Construcción de niveles ----------------

        void ConstruirNivel(int indice)
        {
            nivelActual = indice;
            if (raizNivel != null) Destroy(raizNivel.gameObject);
            raizNivel = new GameObject("Laberinto " + (indice + 1)).transform;

            string[] filas = Laberintos.Todos[indice];
            altoLaberinto = filas.Length;
            anchoLaberinto = filas[0].Length;

            // Suelo de hormigón
            GameObject suelo = CrearSprite("Suelo", Graficos.Cuadrado(), Paleta.Hormigon, Vector2.zero, -10);
            suelo.transform.localScale = new Vector3(anchoLaberinto, altoLaberinto, 1);

            Vector2 inicio = Vector2.zero;

            for (int y = 0; y < altoLaberinto; y++)
            {
                for (int x = 0; x < anchoLaberinto; x++)
                {
                    Vector2 pos = Celda(x, y);
                    switch (filas[y][x])
                    {
                        case '#': CrearPared(pos, Graficos.ParedFija()); break;
                        case 'R': CrearPared(pos, Graficos.ParedRebote()).AddComponent<ParedRebote>(); break;
                        case 'S': CrearPared(pos, Graficos.ParedPegajosa()).AddComponent<ParedPegajosa>(); break;
                        case 'M': CrearParedMovil(pos, Vector2.right); break;
                        case 'V': CrearParedMovil(pos, Vector2.up); break;
                        case 'G': CrearGuardia(pos, false); break;
                        case 'P': CrearGuardia(pos, true); break;
                        case 'E': CrearEstrella(pos); break;
                        case 'C': CrearCerradura(pos); break;
                        case 'B': inicio = pos; break;
                    }
                }
            }

            if (bola == null)
            {
                GameObject go = CrearSprite("Bola", Graficos.BolaConLlave(), Color.white, inicio, 5);
                go.transform.SetParent(null); // la bola no se destruye al cambiar de laberinto
                go.transform.localScale = Vector3.one * 0.72f;
                bola = go.AddComponent<Bola>();
                bola.Configurar(this);
            }
            bola.Recolocar(inicio);

            textoNivel.text = "Laberinto " + (indice + 1) + " / " + Laberintos.Todos.Length;
            AjustarCamara();
        }

        // Convierte una celda del texto en una posición del mundo (centrado en el origen)
        Vector2 Celda(int x, int y)
        {
            return new Vector2(x - (anchoLaberinto - 1) / 2f, (altoLaberinto - 1) / 2f - y);
        }

        GameObject CrearSprite(string nombre, Sprite sprite, Color color, Vector2 pos, int orden)
        {
            GameObject go = new GameObject(nombre);
            go.transform.SetParent(raizNivel);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = orden;
            return go;
        }

        GameObject CrearPared(Vector2 pos, Sprite sprite)
        {
            GameObject go = CrearSprite("Pared", sprite, Color.white, pos, 0);
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        void CrearParedMovil(Vector2 pos, Vector2 eje)
        {
            GameObject go = CrearSprite("Pared móvil", Graficos.ParedMovil(), Color.white, pos, 1);
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<ParedMovil>().Configurar(eje, 1.2f);
        }

        void CrearGuardia(Vector2 pos, bool patrulla)
        {
            GameObject go = CrearSprite(patrulla ? "Guardia patrulla" : "Guardia", Graficos.GuardiaArriba(), Color.white, pos, 3);
            go.transform.localScale = Vector3.one * 0.85f;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.42f;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<Guardia>().Configurar(patrulla ? 2f : 0f, 1.6f);
        }

        void CrearEstrella(Vector2 pos)
        {
            GameObject go = CrearSprite("Estrella", Graficos.Estrella(), Color.white, pos, 2);
            go.transform.localScale = Vector3.one * 0.8f;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            go.AddComponent<EstrellaNivel>();
        }

        void CrearCerradura(Vector2 pos)
        {
            GameObject go = CrearSprite("Cerradura", Graficos.Cerradura(), Color.white, pos, 2);
            go.transform.localScale = Vector3.one * 0.85f;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f; // pequeño: hay que meter la bola de verdad en la cerradura
            go.AddComponent<CerraduraNivel>();
        }

        // Cámara cenital fija y ortográfica a pantalla completa: el laberinto ocupa todo el ancho o todo el alto
        // (lo que llegue antes) y el HUD queda encima del muro exterior, sin tapar los pasillos.
        void AjustarCamara()
        {
            if (camara == null || anchoLaberinto == 0) return;
            float aspecto = (float)Screen.width / Screen.height;
            camara.orthographicSize = Mathf.Max(altoLaberinto / 2f, anchoLaberinto / aspecto / 2f);
            camara.transform.position = new Vector3(0, 0, -10);
        }

        // ---------------- Eventos que avisa la bola ----------------

        public void GolpeDeGuardia()
        {
            if (!jugando) return;
            SumarPuntos(PuntosGolpe);
            Sonidos.Golpe();
            ConexionPartida.PerderVida();
            ActualizarHud();
            StartCoroutine(Destellar());

            if (ConexionPartida.Vidas <= 0) Terminar(false);
        }

        public void EstrellaRecogida(EstrellaNivel estrella)
        {
            if (!jugando) return;
            SumarPuntos(PuntosEstrella);
            Sonidos.Estrella();
            Destroy(estrella.gameObject);
        }

        public void CerraduraAlcanzada()
        {
            if (!jugando) return;
            SumarPuntos(PuntosLaberinto);
            Sonidos.Cerradura();

            if (nivelActual + 1 < Laberintos.Todos.Length)
            {
                ConstruirNivel(nivelActual + 1);
            }
            else
            {
                // Bonus proporcional al tiempo que sobra
                SumarPuntos(Mathf.RoundToInt(BonusTiempoMaximo * tiempoRestante / Duracion));
                Terminar(true);
            }
        }

        void TiempoAgotado()
        {
            if (TiempoAgotadoQuitaTodasLasVidas) ConexionPartida.PerderTodasLasVidas();
            else ConexionPartida.PerderVida();
            Terminar(false);
        }

        // ---------------- Puntos y final ----------------

        void SumarPuntos(int cantidad)
        {
            puntos = Mathf.Clamp(puntos + cantidad, 0, 100);
            ActualizarHud();
        }

        void Terminar(bool superado)
        {
            if (terminado) return;
            terminado = true;
            jugando = false;
            Inclinacion = Vector2.zero;
            bola.Detener();
            StartCoroutine(SecuenciaFinal(superado));
        }

        IEnumerator SecuenciaFinal(bool superado)
        {
            if (superado)
            {
                Sonidos.Victoria();
                MostrarMensaje("¡SUPERADO!", Paleta.Recompensa);
            }
            else
            {
                Sonidos.Alarma();
                MostrarMensaje("¡PILLADO!", Paleta.Peligro);
            }
            ActualizarHud();
            yield return new WaitForSeconds(1.8f);

            if (ConexionPartida.HayPartidaConectada)
            {
                // Partida completa: el GameManager del equipo decide a qué escena ir
                ConexionPartida.Terminar(superado, puntos);
            }
            else
            {
                // Modo de prueba (minijuego solo): resumen y botón para reintentar
                OcultarMensaje();
                MostrarPanelFinal(superado);
            }
        }

        // ---------------- HUD ----------------

        void CrearHud()
        {
            const float alto = 84f; // del alto de un muro: así la franja no tapa los pasillos

            // Franja superior semitransparente para que el HUD se lea siempre
            Image franja = Graficos.Imagen(canvas.transform, "Franja HUD", null, Paleta.ConAlfa(Paleta.Contorno, 0.55f));
            franja.rectTransform.anchorMin = new Vector2(0, 1);
            franja.rectTransform.anchorMax = new Vector2(1, 1);
            franja.rectTransform.pivot = new Vector2(0.5f, 1);
            franja.rectTransform.anchoredPosition = Vector2.zero;
            franja.rectTransform.sizeDelta = new Vector2(0, alto);
            franja.raycastTarget = false;

            // Puntos arriba a la izquierda, tiempo en el centro y vidas a la derecha (GDD 6.2)
            textoPuntos = Graficos.Texto(franja.transform, "0 pts", 54, Paleta.Recompensa, TextAnchor.MiddleLeft);
            Graficos.Colocar(textoPuntos.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(400, alto));

            textoTiempo = Graficos.Texto(franja.transform, "60", 60, Color.white);
            Graficos.Colocar(textoTiempo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, alto));

            iconosVidas = new Image[ConexionPartida.VidasMaximas];
            for (int i = 0; i < iconosVidas.Length; i++)
            {
                Image vida = Graficos.Imagen(franja.transform, "Vida " + (i + 1), null, Color.white);
                Graficos.Colocar(vida.rectTransform, new Vector2(1, 0.5f), new Vector2(-40 - i * 70, 0), new Vector2(56, 56));
                iconosVidas[i] = vida;
            }

            // "Laberinto X / 5" dentro de la franja, junto al tiempo, para no tapar el laberinto
            // Anclado a la derecha, justo a la izquierda de las vidas, para que nunca se monten
            textoNivel = Graficos.Texto(franja.transform, "", 36, Paleta.Hormigon, TextAnchor.MiddleRight);
            Graficos.Colocar(textoNivel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-240, 0), new Vector2(320, alto));

            Button calibrar = Graficos.Boton(canvas.transform, "CALIBRAR", Paleta.ConAlfa(Paleta.PanelMetal, 0.85f), Color.white, 30, Calibrar);
            Graficos.Colocar((RectTransform)calibrar.transform, new Vector2(0, 0), new Vector2(12, 12), new Vector2(170, 60));

            // Destello rojo suave al perder una vida (sin parpadeos rápidos, por fotosensibilidad)
            destello = Graficos.Panel(canvas.transform, "Destello", Paleta.ConAlfa(Paleta.Peligro, 0f));
            destello.raycastTarget = false;

            // Mensaje central: instrucción al empezar y resultado al acabar
            panelMensaje = Graficos.Panel(canvas.transform, "Mensaje", Paleta.ConAlfa(Paleta.Contorno, 0.6f));
            textoMensaje = Graficos.ConContorno(Graficos.Texto(panelMensaje.transform, "", 170, Paleta.Recompensa), 6f);
            Graficos.Estirar(textoMensaje.rectTransform);
            panelMensaje.gameObject.SetActive(false);
        }

        void ActualizarHud()
        {
            if (textoPuntos == null) return;
            textoPuntos.text = puntos + " pts";
            textoTiempo.text = Mathf.CeilToInt(tiempoRestante).ToString();
            textoTiempo.color = tiempoRestante <= 10f ? Paleta.Peligro : Color.white;

            for (int i = 0; i < iconosVidas.Length; i++)
            {
                // La vida perdida queda como círculo vacío: no se informa solo con el color
                bool tiene = i < ConexionPartida.Vidas;
                iconosVidas[i].sprite = tiene
                    ? Graficos.Circulo(Paleta.Peligro, Color.white, 0.12f)
                    : Graficos.Circulo(Color.clear, Color.white, 0.12f);
            }
        }

        void MostrarMensaje(string texto, Color color)
        {
            panelMensaje.gameObject.SetActive(true);
            panelMensaje.transform.SetAsLastSibling();
            textoMensaje.text = texto;
            textoMensaje.color = color;
        }

        void OcultarMensaje()
        {
            panelMensaje.gameObject.SetActive(false);
        }

        IEnumerator Destellar()
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                destello.color = Paleta.ConAlfa(Paleta.Peligro, 0.35f * (1f - t / 0.5f));
                yield return null;
            }
            destello.color = Paleta.ConAlfa(Paleta.Peligro, 0f);
        }

        void MostrarPanelFinal(bool superado)
        {
            Image fondo = Graficos.Panel(canvas.transform, "Resultado", Paleta.ConAlfa(Paleta.Contorno, 0.88f));

            Text titulo = Graficos.ConContorno(Graficos.Texto(fondo.transform, superado ? "¡LLAVE RESCATADA!" : "¡PILLADO!", 110, superado ? Paleta.Recompensa : Paleta.Peligro), 6f);
            Graficos.Colocar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(1600, 160));

            string resumen = "Puntos: " + puntos + " / 100\nLaberintos superados: " + (superado ? Laberintos.Todos.Length : nivelActual) + " / " + Laberintos.Todos.Length;
            Text texto = Graficos.Texto(fondo.transform, resumen, 56, Color.white);
            Graficos.Colocar(texto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1400, 180));

            Button reintentar = Graficos.Boton(fondo.transform, "REINTENTAR", Paleta.Recompensa, Paleta.Contorno, 64, () =>
            {
                ConexionPartida.Vidas = ConexionPartida.VidasMaximas;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
            Graficos.Colocar((RectTransform)reintentar.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(620, 170));

            Text nota = Graficos.Texto(fondo.transform, "Modo de prueba: al juntar los minijuegos, aquí se pasa a la siguiente prueba.", 30, Paleta.Hormigon, TextAnchor.MiddleCenter, false);
            Graficos.Colocar(nota.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1600, 60));
        }
    }
}
