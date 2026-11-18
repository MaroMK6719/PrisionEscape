using System.Collections.Generic;
using UnityEngine;

namespace Platybytes.Llave
{
    // Efectos de sonido provisionales de Rescata la llave generados por código (pitidos sencillos).
    // Cuando tengáis los sonidos de Freesound/Kenney, se sustituyen cargando AudioClips reales.
    public static class Sonidos
    {
        static AudioSource fuente;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static float Volumen = 0.8f;
        public static bool VibracionActivada = true;

        static AudioSource Fuente
        {
            get
            {
                if (fuente == null)
                {
                    // Vive en la escena del minijuego y se destruye con ella
                    fuente = new GameObject("Sonidos Rescata la llave").AddComponent<AudioSource>();
                    fuente.playOnAwake = false;
                }
                return fuente;
            }
        }

        public static void Golpe() { Tocar("golpe", 0.18f, 220f, 90f, true); Vibrar(); }
        public static void Rebote() { Tocar("rebote", 0.08f, 300f, 700f, false); }
        public static void Estrella() { Tocar("estrella", 0.25f, 990f, 1980f, false); }
        public static void Cerradura() { Tocar("cerradura", 0.3f, 523f, 1046f, true); }
        public static void Victoria() { Tocar("victoria", 0.6f, 523f, 1568f, false); }

        // Sirena de alarma: dos tonos alternos (sonido común de "¡Pillado!")
        public static void Alarma()
        {
            if (!clips.ContainsKey("alarma"))
            {
                const int frecuenciaMuestreo = 44100;
                float duracion = 0.9f;
                int total = (int)(frecuenciaMuestreo * duracion);
                float[] datos = new float[total];
                float fase = 0f;
                for (int i = 0; i < total; i++)
                {
                    float t = (float)i / frecuenciaMuestreo;
                    float frecuencia = (Mathf.Repeat(t, 0.3f) < 0.15f) ? 660f : 880f;
                    fase += 2f * Mathf.PI * frecuencia / frecuenciaMuestreo;
                    datos[i] = Mathf.Sign(Mathf.Sin(fase)) * 0.25f * (1f - t / duracion);
                }
                AudioClip clip = AudioClip.Create("alarma", total, 1, frecuenciaMuestreo, false);
                clip.SetData(datos, 0);
                clips["alarma"] = clip;
            }
            Fuente.PlayOneShot(clips["alarma"], Volumen);
            Vibrar();
        }

        static void Tocar(string nombre, float duracion, float frecuenciaInicial, float frecuenciaFinal, bool cuadrada)
        {
            if (!clips.TryGetValue(nombre, out AudioClip clip))
            {
                const int frecuenciaMuestreo = 44100;
                int total = (int)(frecuenciaMuestreo * duracion);
                float[] datos = new float[total];
                float fase = 0f;
                for (int i = 0; i < total; i++)
                {
                    float progreso = (float)i / total;
                    float frecuencia = Mathf.Lerp(frecuenciaInicial, frecuenciaFinal, progreso);
                    fase += 2f * Mathf.PI * frecuencia / frecuenciaMuestreo;
                    float onda = Mathf.Sin(fase);
                    if (cuadrada) onda = Mathf.Sign(onda) * 0.6f;
                    datos[i] = onda * 0.35f * (1f - progreso);
                }
                clip = AudioClip.Create(nombre, total, 1, frecuenciaMuestreo, false);
                clip.SetData(datos, 0);
                clips[nombre] = clip;
            }
            Fuente.PlayOneShot(clip, Volumen);
        }

        public static void Vibrar()
        {
            if (!VibracionActivada) return;
    #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
    #endif
        }
    }
}
