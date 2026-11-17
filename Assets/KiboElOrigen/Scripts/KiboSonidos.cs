using UnityEngine;

namespace KiboElOrigen
{
    // Efectos de sonido generados por código (no necesitan archivos de audio)
    public static class KiboSonidos
    {
        static AudioSource fuente;
        static AudioClip clic, bien, mal, recoger, ganar, aviso;

        static AudioSource Fuente()
        {
            if (fuente != null) return fuente;
            if (!Application.isPlaying) return null;
            var go = new GameObject("KiboSonidos");
            Object.DontDestroyOnLoad(go);
            fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.ignoreListenerPause = true;
            return fuente;
        }

        static void Tocar(ref AudioClip clip, System.Func<AudioClip> crear, float volumen = 1f)
        {
            var f = Fuente();
            if (f == null) return;
            if (clip == null) clip = crear();
            f.PlayOneShot(clip, volumen * KiboAjustes.VolumenEfectos);
        }

        public static void Clic() { Tocar(ref clic, () => Tonos("clic", new[] { 900f }, 0.05f, 0.5f), 0.5f); }
        public static void Bien() { Tocar(ref bien, () => Tonos("bien", new[] { 660f, 880f, 1320f }, 0.09f, 0.55f)); }
        public static void Mal() { Tocar(ref mal, () => Tonos("mal", new[] { 220f, 165f }, 0.16f, 0.6f, true)); }
        public static void Recoger() { Tocar(ref recoger, () => Tonos("recoger", new[] { 520f, 780f }, 0.06f, 0.45f)); }
        public static void Aviso() { Tocar(ref aviso, () => Tonos("aviso", new[] { 440f, 440f }, 0.07f, 0.4f)); }
        public static void Ganar() { Tocar(ref ganar, () => Tonos("ganar", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.12f, 0.55f)); }

        // Secuencia de notas cortas con envolvente suave
        static AudioClip Tonos(string nombre, float[] notas, float duracion, float volumen, bool aspero = false)
        {
            const int frecuencia = 44100;
            int porNota = Mathf.CeilToInt(duracion * frecuencia);
            int total = porNota * notas.Length + frecuencia / 20;
            var datos = new float[total];
            for (int n = 0; n < notas.Length; n++)
            {
                float f = notas[n];
                for (int i = 0; i < porNota; i++)
                {
                    float t = i / (float)frecuencia;
                    float env = Mathf.Min(1f, i / 220f) * Mathf.Exp(-3.5f * i / porNota);
                    float onda = Mathf.Sin(2f * Mathf.PI * f * t);
                    if (aspero) onda = Mathf.Sign(onda) * 0.6f + onda * 0.4f;
                    else onda += 0.25f * Mathf.Sin(4f * Mathf.PI * f * t);
                    int k = n * porNota + i;
                    datos[k] += onda * env * volumen * 0.6f;
                }
            }
            var clip = AudioClip.Create("Kibo_" + nombre, total, 1, frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }
    }
}
