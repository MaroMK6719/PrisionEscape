using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Platybytes.Llave
{
    // Sprites dibujados por código (placeholders con la paleta y el estilo del GDD) y ayudas para crear la UI.
    // Cuando tengáis el arte definitivo, solo hay que cambiar el sprite que devuelve cada función.
    public static class Graficos
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Font fuente;

        public static Font Fuente
        {
            get
            {
                if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return fuente;
            }
        }

        // ------------------------------------------------------------------
        //  Dibujo procedural
        // ------------------------------------------------------------------

        // Dibuja una textura de n x n píxeles. La función recibe coordenadas de -1 a 1 y devuelve el color.
        // Se toman 4 muestras por píxel para suavizar los bordes.
        static Sprite Dibujar(string clave, int n, Func<float, float, Color> pintar, float pixelesPorUnidad = -1, Vector4 borde9 = default)
        {
            if (cache.TryGetValue(clave, out Sprite guardado)) return guardado;

            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixeles = new Color[n * n];
            float[] desplazamientos = { 0.25f, 0.75f };

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Color suma = Color.clear;
                    foreach (float oy in desplazamientos)
                    {
                        foreach (float ox in desplazamientos)
                        {
                            float u = (x + ox) / n * 2f - 1f;
                            float v = (y + oy) / n * 2f - 1f;
                            Color c = pintar(u, v);
                            // Premultiplicamos para que el promedio de bordes no salga oscuro
                            suma += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    }
                    suma /= 4f;
                    if (suma.a > 0.001f) suma = new Color(suma.r / suma.a, suma.g / suma.a, suma.b / suma.a, suma.a);
                    pixeles[y * n + x] = suma;
                }
            }

            tex.SetPixels(pixeles);
            tex.Apply();

            if (pixelesPorUnidad <= 0) pixelesPorUnidad = n; // por defecto, 1 unidad de mundo
            Sprite s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), pixelesPorUnidad, 0, SpriteMeshType.FullRect, borde9);
            s.name = clave;
            cache[clave] = s;
            return s;
        }

        // Círculo relleno con contorno grueso (estilo cartoon)
        public static Sprite Circulo(Color relleno, Color contorno, float grosor = 0.14f)
        {
            string clave = "circulo" + relleno + contorno + grosor;
            return Dibujar(clave, 128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) return Color.clear;
                if (d > 1f - grosor) return contorno;
                return relleno;
            });
        }

        // Cuadrado lleno de 1 x 1 unidades (paredes, fondos)
        public static Sprite Cuadrado()
        {
            return Dibujar("cuadrado", 8, (u, v) => Color.white);
        }

        // Losetas de pared. Cada tipo tiene además un dibujo propio para no depender solo del color (accesibilidad).
        public static Sprite ParedFija()
        {
            return Dibujar("pared_fija", 64, (u, v) => Paleta.Contorno);
        }

        public static Sprite ParedRebote()
        {
            Color fondo = Paleta.Policia;
            return Dibujar("pared_rebote", 64, (u, v) =>
            {
                if (Mathf.Abs(u) > 0.86f || Mathf.Abs(v) > 0.86f) return Paleta.Contorno;
                // Rayas diagonales blancas
                float raya = Mathf.Repeat((u + v) * 2f, 1f);
                return raya < 0.35f ? Color.white : fondo;
            });
        }

        public static Sprite ParedPegajosa()
        {
            Color fondo = Paleta.Hex("#8A6D3B");
            return Dibujar("pared_pegajosa", 64, (u, v) =>
            {
                if (Mathf.Abs(u) > 0.86f || Mathf.Abs(v) > 0.86f) return Paleta.Contorno;
                // Puntos (lunares)
                float cu = Mathf.Repeat(u * 1.5f + 0.25f, 1f) - 0.5f;
                float cv = Mathf.Repeat(v * 1.5f + 0.25f, 1f) - 0.5f;
                return (cu * cu + cv * cv) < 0.06f ? Paleta.Recompensa : fondo;
            });
        }

        public static Sprite ParedMovil()
        {
            Color fondo = Paleta.Hex("#8C8C8C");
            return Dibujar("pared_movil", 64, (u, v) =>
            {
                if (Mathf.Abs(u) > 0.84f || Mathf.Abs(v) > 0.84f) return Paleta.Contorno;
                // Cruz de flechas simplificada: una cruz blanca
                if (Mathf.Abs(u) < 0.12f && Mathf.Abs(v) < 0.6f) return Color.white;
                if (Mathf.Abs(v) < 0.12f && Mathf.Abs(u) < 0.6f) return Color.white;
                return fondo;
            });
        }

        // Bola con la llave dentro
        public static Sprite BolaConLlave()
        {
            return Dibujar("bola_llave", 128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) return Color.clear;
                if (d > 0.86f) return Paleta.Contorno;
                // Llave: anillo a la izquierda, barra y dientes
                float ax = u + 0.32f;
                float anillo = Mathf.Sqrt(ax * ax + v * v);
                bool esLlave = anillo < 0.26f && anillo > 0.12f;
                if (u > -0.1f && u < 0.5f && Mathf.Abs(v) < 0.07f) esLlave = true;
                if (u > 0.26f && u < 0.34f && v < 0 && v > -0.22f) esLlave = true;
                if (u > 0.42f && u < 0.5f && v < 0 && v > -0.18f) esLlave = true;
                if (esLlave) return Paleta.Hex("#E59400");
                return Color.white;
            });
        }

        // Guardia visto desde arriba: cuerpo verde y gorra oscura
        public static Sprite GuardiaArriba()
        {
            Color gorra = Paleta.Hex("#3E5A33");
            return Dibujar("guardia", 128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) return Color.clear;
                if (d > 0.86f) return Paleta.Contorno;
                float dg = Mathf.Sqrt(u * u + (v - 0.1f) * (v - 0.1f));
                if (dg < 0.45f) return gorra;
                if (v > 0.45f && v < 0.62f && Mathf.Abs(u) < 0.35f) return gorra; // visera
                return Paleta.Guardia;
            });
        }

        public static Sprite Estrella()
        {
            // Polígono de 10 vértices alternando radio exterior e interior
            Vector2[] vertices = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float radio = (i % 2 == 0) ? 0.97f : 0.42f;
                float angulo = Mathf.PI / 2f + i * Mathf.PI / 5f;
                vertices[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo) - 0.06f) * radio;
            }

            return Dibujar("estrella", 128, (u, v) =>
            {
                Vector2 p = new Vector2(u, v);
                if (!DentroDePoligono(p, vertices)) return Color.clear;
                if (DistanciaABorde(p, vertices) < 0.1f) return Paleta.Contorno;
                return Paleta.Recompensa;
            });
        }

        static bool DentroDePoligono(Vector2 p, Vector2[] v)
        {
            bool dentro = false;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                if (((v[i].y > p.y) != (v[j].y > p.y)) &&
                    (p.x < (v[j].x - v[i].x) * (p.y - v[i].y) / (v[j].y - v[i].y) + v[i].x))
                {
                    dentro = !dentro;
                }
            }
            return dentro;
        }

        static float DistanciaABorde(Vector2 p, Vector2[] v)
        {
            float minimo = float.MaxValue;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i++)
            {
                Vector2 a = v[j];
                Vector2 b = v[i];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                minimo = Mathf.Min(minimo, Vector2.Distance(p, a + ab * t));
            }
            return minimo;
        }

        public static Sprite Cerradura()
        {
            return Dibujar("cerradura", 128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 1f) return Color.clear;
                if (d > 0.9f) return Paleta.Recompensa; // aro amarillo: "ve aquí"
                float dc = Mathf.Sqrt(u * u + (v - 0.18f) * (v - 0.18f));
                if (dc < 0.24f) return Color.white;
                if (Mathf.Abs(u) < 0.11f && v < 0.18f && v > -0.5f) return Color.white;
                return Paleta.Contorno;
            });
        }

        // Rectángulo con esquinas redondeadas preparado para 9-slice (botones y paneles)
        public static Sprite Redondeado()
        {
            const int n = 64;
            const float radio = 0.45f; // en coordenadas -1..1
            return Dibujar("redondeado", n, (u, v) =>
            {
                float qx = Mathf.Max(Mathf.Abs(u) - (1f - radio), 0f);
                float qy = Mathf.Max(Mathf.Abs(v) - (1f - radio), 0f);
                return (qx * qx + qy * qy) <= radio * radio ? Color.white : Color.clear;
            }, 100f, new Vector4(20, 20, 20, 20));
        }

        // ------------------------------------------------------------------
        //  Cámara
        // ------------------------------------------------------------------

        // En proyectos URP 2D, los sprites salen negros si la escena no tiene ninguna luz 2D.
        // Esto añade una "Global Light 2D" por código (sin depender de URP al compilar).
        // En proyectos sin URP no hace nada, porque allí los sprites ya se ven sin luces.
        public static void AsegurarLuzGlobal2D()
        {
            Type tipo = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (tipo == null) return;
            if (UnityEngine.Object.FindFirstObjectByType(tipo) != null) return;

            GameObject go = new GameObject("Luz global 2D");
            Component luz = go.AddComponent(tipo);
            System.Reflection.PropertyInfo propiedad = tipo.GetProperty("lightType");
            if (propiedad != null) propiedad.SetValue(luz, Enum.Parse(propiedad.PropertyType, "Global"));
        }

        public static Camera CrearCamara(Color fondo, float tamanoOrtografico = 5f)
        {
            AsegurarLuzGlobal2D();

            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = tamanoOrtografico;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = fondo;
            return cam;
        }

        // ------------------------------------------------------------------
        //  UI
        // ------------------------------------------------------------------

        public static Canvas CrearCanvas(bool vertical)
        {
            AsegurarEventSystem();

            GameObject go = new GameObject("Canvas");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler escalador = go.AddComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = vertical ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void AsegurarEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // Coloca un RectTransform anclado a un punto (0..1) de su padre
        public static RectTransform Colocar(RectTransform rt, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            rt.anchorMin = ancla;
            rt.anchorMax = ancla;
            rt.pivot = ancla;
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;
            return rt;
        }

        // Hace que el RectTransform ocupe todo su padre
        public static RectTransform Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform padre, string nombre, Color color)
        {
            Image img = Imagen(padre, nombre, null, color);
            Estirar(img.rectTransform);
            return img;
        }

        public static Text Texto(Transform padre, string contenido, int tamano, Color color, TextAnchor alineacion = TextAnchor.MiddleCenter, bool negrita = true)
        {
            GameObject go = new GameObject("Texto", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            Text t = go.AddComponent<Text>();
            t.font = Fuente;
            t.text = contenido;
            t.fontSize = tamano;
            t.color = color;
            t.alignment = alineacion;
            t.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Añade un contorno oscuro al texto para que se lea sobre cualquier fondo
        public static Text ConContorno(Text t, float grosor = 3f)
        {
            Outline o = t.gameObject.AddComponent<Outline>();
            o.effectColor = Paleta.Contorno;
            o.effectDistance = new Vector2(grosor, -grosor);
            return t;
        }

        public static Button Boton(Transform padre, string texto, Color fondo, Color colorTexto, int tamanoTexto, UnityAction alPulsar)
        {
            Image img = Imagen(padre, "Boton " + texto, Redondeado(), fondo);
            Button b = img.gameObject.AddComponent<Button>();
            ColorBlock colores = b.colors;
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            b.colors = colores;
            if (alPulsar != null) b.onClick.AddListener(alPulsar);

            Text t = Texto(img.transform, texto, tamanoTexto, colorTexto);
            Estirar(t.rectTransform);
            return b;
        }
    }
}
