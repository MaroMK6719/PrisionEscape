using UnityEngine;

namespace Platybytes.Llave
{
    // Paleta base del GDD (apartado 4). Regla: amarillo = "cógelo", rojo = "cuidado".
    public static class Paleta
    {
        public static readonly Color Hormigon   = Hex("#D9D9D9");
        public static readonly Color Contorno   = Hex("#1E1E1E");
        public static readonly Color Preso      = Hex("#F28C28");
        public static readonly Color Guardia    = Hex("#6B8E5A");
        public static readonly Color Policia    = Hex("#2F5DA8");
        public static readonly Color Recompensa = Hex("#FFC93C");
        public static readonly Color Peligro    = Hex("#E63946");

        // Colores de apoyo para fondos de menú y paneles
        public static readonly Color FondoOscuro = Hex("#23262B");
        public static readonly Color PanelMetal  = Hex("#3A3F47");
        public static readonly Color Blanco      = Color.white;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        public static Color ConAlfa(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
