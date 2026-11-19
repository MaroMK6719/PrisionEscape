using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platybytes.Llave;
using Platybytes.Minijuegos;

namespace Platybytes.Comun
{
    // Lleva la partida completa: el orden de las pruebas, las 3 vidas compartidas y los puntos totales.
    //
    // Orden: Menú -> Press the Button! -> Rescata la llave -> Menú (con el resultado).
    // Se crea al pulsar JUGAR y desaparece al acabar la partida. Si se abre un minijuego directamente
    // (o con los botones de "Probar"), no existe y cada minijuego funciona solo, en modo de prueba.
    public class FlujoPartida : MonoBehaviour
    {
        // Nombres de las escenas (tienen que estar en Build Profiles > Scene List)
        public const string EscenaMenu = "Menu";
        public const string EscenaPressTheButton = "PressTheButton";
        public const string EscenaRescataLaLlave = "RescataLaLlave";

        public const int VidasIniciales = 3;
        const string ClaveRecord = "PrisonEscape.Record";

        // Resultado de la última partida, para que lo enseñe el menú
        public static bool HayResultado;
        public static bool UltimaFugaConseguida;
        public static int UltimosPuntos;
        public static bool UltimoRecordNuevo;

        public static int Record
        {
            get { return PlayerPrefs.GetInt(ClaveRecord, 0); }
        }

        static FlujoPartida instancia;

        int vidas;
        int puntos;
        int puntosPressTheButton; // se suman al pasar a la siguiente prueba

        // ---------------- Desde el menú ----------------

        public static void EmpezarPartida()
        {
            if (instancia == null)
            {
                GameObject go = new GameObject("Flujo de la partida");
                instancia = go.AddComponent<FlujoPartida>();
                DontDestroyOnLoad(go);
            }

            instancia.vidas = VidasIniciales;
            instancia.puntos = 0;
            instancia.puntosPressTheButton = 0;
            HayResultado = false;
            Cargar(EscenaPressTheButton);
        }

        // Abre un minijuego suelto, sin partida (modo de prueba)
        public static void ProbarMinijuego(string escena)
        {
            if (instancia != null) Destroy(instancia.gameObject);
            instancia = null;
            ConexionPartida.Vidas = VidasIniciales;
            Cargar(escena);
        }

        static void Cargar(string escena)
        {
            // El laberinto se pone solo en horizontal; todo lo demás va en vertical
            if (escena != EscenaRescataLaLlave) Screen.orientation = ScreenOrientation.Portrait;
            SceneManager.LoadScene(escena);
        }

        // ---------------- Conexión con los minijuegos ----------------

        void OnEnable()
        {
            SceneManager.sceneLoaded += AlCargarEscena;
            ConexionPartida.AlTerminar += AlTerminarRescataLaLlave;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            ConexionPartida.AlTerminar -= AlTerminarRescataLaLlave;
        }

        void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (escena.name == EscenaPressTheButton)
            {
                // El minijuego de Víctor avisa con dos eventos: Finished y NextRequested
                PressTheButtonGame juego = FindFirstObjectByType<PressTheButtonGame>();
                if (juego != null)
                {
                    juego.Finished += AlTerminarPressTheButton;
                    juego.NextRequested += AlPedirSiguientePrueba;
                }
                else
                {
                    Debug.LogWarning("FlujoPartida: no encuentro PressTheButtonGame en la escena " + escena.name);
                }
            }
            else if (escena.name == EscenaRescataLaLlave)
            {
                // Le pasamos al laberinto las vidas que le quedan al jugador
                ConexionPartida.Vidas = vidas;
            }
        }

        // Prueba 1: Press the Button!
        void AlTerminarPressTheButton(bool gano, int puntosPrueba)
        {
            if (gano)
            {
                // Se guardan y se suman al pulsar "Siguiente prueba" (por si repite la prueba antes)
                puntosPressTheButton = puntosPrueba;
                return;
            }

            vidas--;
            Debug.Log("FlujoPartida: fallo en Press the Button. Vidas: " + vidas);
            if (vidas <= 0)
            {
                // Sin vidas: se deja ver el mensaje de "¡ERROR!" un momento y se vuelve al menú
                StartCoroutine(TerminarPartidaConRetraso(false, 2.5f));
            }
            // Con vidas, el jugador puede usar el botón de reintentar del propio minijuego
        }

        void AlPedirSiguientePrueba()
        {
            puntos += puntosPressTheButton;
            Cargar(EscenaRescataLaLlave);
        }

        // Prueba 2: Rescata la llave
        void AlTerminarRescataLaLlave(bool superado, int puntosPrueba)
        {
            vidas = ConexionPartida.Vidas; // los guardias pueden haber quitado vidas
            puntos += puntosPrueba;

            if (superado)
            {
                TerminarPartida(true);
            }
            else if (vidas > 0)
            {
                Cargar(EscenaRescataLaLlave); // repetir la prueba con las vidas que quedan
            }
            else
            {
                TerminarPartida(false);
            }
        }

        // ---------------- Final ----------------

        IEnumerator TerminarPartidaConRetraso(bool fuga, float segundos)
        {
            yield return new WaitForSeconds(segundos);
            TerminarPartida(fuga);
        }

        void TerminarPartida(bool fuga)
        {
            HayResultado = true;
            UltimaFugaConseguida = fuga;
            UltimosPuntos = puntos;
            UltimoRecordNuevo = puntos > Record;
            if (UltimoRecordNuevo)
            {
                PlayerPrefs.SetInt(ClaveRecord, puntos);
                PlayerPrefs.Save();
            }

            instancia = null;
            Destroy(gameObject);
            Cargar(EscenaMenu);
        }
    }
}
