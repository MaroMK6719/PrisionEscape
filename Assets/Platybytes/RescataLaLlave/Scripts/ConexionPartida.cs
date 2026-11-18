using System;

namespace Platybytes.Llave
{
    // Punto de unión entre "Rescata la llave" y la partida completa del equipo.
    //
    // Mientras se prueba este minijuego solo, no hace falta tocar nada: empieza con 3 vidas
    // y al terminar enseña un botón para reintentar.
    //
    // Al juntar los 5 minijuegos, el GameManager del equipo solo tiene que:
    //   1. Antes de cargar la escena:   ConexionPartida.Vidas = vidasQueLeQuedanAlJugador;
    //   2. Suscribirse una vez:          ConexionPartida.AlTerminar += MiMetodo;   // (bool superado, int puntos)
    //   3. En MiMetodo, leer ConexionPartida.Vidas para saber con cuántas vidas sale el jugador
    //      y cargar la siguiente escena (o repetir esta, o ir a la pantalla final).
    public static class ConexionPartida
    {
        public const int VidasMaximas = 3;

        // Vidas globales del jugador. Las lee y las descuenta este minijuego.
        public static int Vidas = VidasMaximas;

        // Opcional: avisa cada vez que un guardia quita una vida.
        public static event Action AlPerderVida;

        // Se llama al acabar el minijuego (después de "¡SUPERADO!" o "¡PILLADO!").
        // superado: true si se completan los 5 laberintos. puntos: de 0 a 100.
        public static event Action<bool, int> AlTerminar;

        // true si el GameManager del equipo está escuchando; si no, el minijuego funciona en modo de prueba.
        public static bool HayPartidaConectada
        {
            get { return AlTerminar != null; }
        }

        internal static void PerderVida()
        {
            if (Vidas > 0) Vidas--;
            if (AlPerderVida != null) AlPerderVida();
        }

        internal static void PerderTodasLasVidas()
        {
            while (Vidas > 0) PerderVida();
        }

        internal static void Terminar(bool superado, int puntos)
        {
            if (AlTerminar != null) AlTerminar(superado, puntos);
        }
    }
}
