using UnityEngine;

namespace KiboElOrigen
{
    // Música de Kibo el Origen (archivos en KiboElOrigen/Resources/KiboMusica).
    //  • Poner("musica_menu")   → música en bucle (baja la música ambiente del nivel)
    //  • Stinger("musica_ganaste") → melodía corta de ganaste/perdiste
    //  • Volver()               → quita la música y vuelve la del nivel
    public class KiboMusica : MonoBehaviour
    {
        static KiboMusica instancia;

        AudioSource musica, corta;
        string actual;
        float factorMusica, objetivoMusica;
        float factorAmbiente = 1f;
        float finStinger;
        string despuesDelStinger;
        float tono = 1f;
        readonly System.Collections.Generic.List<AudioSource> ambiente = new System.Collections.Generic.List<AudioSource>();
        float proximaBusqueda;

        static KiboMusica I
        {
            get
            {
                if (instancia == null)
                {
                    var go = new GameObject("KIBO_Musica");
                    DontDestroyOnLoad(go);
                    instancia = go.AddComponent<KiboMusica>();
                }
                return instancia;
            }
        }

        void Awake()
        {
            musica = gameObject.AddComponent<AudioSource>();
            musica.loop = true;
            musica.playOnAwake = false;
            musica.spatialBlend = 0f;
            musica.ignoreListenerPause = true;
            corta = gameObject.AddComponent<AudioSource>();
            corta.loop = false;
            corta.playOnAwake = false;
            corta.spatialBlend = 0f;
            corta.ignoreListenerPause = true;
        }

        static AudioClip Clip(string nombre)
        {
            return Resources.Load<AudioClip>("KiboMusica/" + nombre);
        }

        // Música en bucle
        public static void Poner(string nombre)
        {
            var m = I;
            if (m.actual == nombre && m.musica.isPlaying) { m.objetivoMusica = 1f; return; }
            var clip = Clip(nombre);
            if (clip == null) return;
            m.actual = nombre;
            m.musica.clip = clip;
            m.musica.time = 0f;
            m.musica.pitch = m.tono = 1f;
            m.factorMusica = 0f;
            m.objetivoMusica = 1f;
            m.musica.Play();
        }

        public static void Detener()
        {
            if (instancia == null) return;
            instancia.actual = null;
            instancia.objetivoMusica = 0f;
        }

        // Quita la música de Kibo y vuelve la del nivel
        public static void Volver()
        {
            Detener();
            Tono(1f);
        }

        // Melodía corta (ganaste / perdiste). Si se indica 'luego', después suena esa música en bucle
        public static void Stinger(string nombre, string luego = null)
        {
            var clip = Clip(nombre);
            if (clip == null) return;
            var m = I;
            m.corta.Stop();
            m.corta.clip = clip;
            m.corta.volume = KiboAjustes.VolumenMusica;
            m.corta.Play();
            m.finStinger = Time.unscaledTime + clip.length;
            m.despuesDelStinger = luego;
        }

        // Más rápido = tono más alto (lo usa el minijuego del TransMilenio)
        public static void Tono(float t)
        {
            if (instancia == null) return;
            instancia.tono = Mathf.Clamp(t, 0.5f, 2f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool stinger = Time.unscaledTime < finStinger;
            if (!stinger && despuesDelStinger != null)
            {
                string s = despuesDelStinger;
                despuesDelStinger = null;
                Poner(s);
            }

            float vol = KiboAjustes.VolumenMusica;

            // Música de Kibo (se baja mientras suena un stinger)
            float meta = objetivoMusica * (stinger ? 0.12f : 1f);
            factorMusica = Mathf.MoveTowards(factorMusica, meta, dt * 1.6f);
            musica.volume = vol * factorMusica * 0.85f;
            musica.pitch = Mathf.MoveTowards(musica.pitch, tono, dt * 0.5f);
            if (objetivoMusica <= 0f && factorMusica <= 0.001f && musica.isPlaying) musica.Stop();
            corta.volume = vol;

            // Música ambiente del nivel (MusicaManager): se baja mientras suena la de Kibo
            bool silenciar = (musica.isPlaying && objetivoMusica > 0f) || stinger;
            factorAmbiente = Mathf.MoveTowards(factorAmbiente, silenciar ? 0f : 1f, dt * 1.6f);
            if (Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + 1f;
                ambiente.Clear();
                foreach (var mm in KiboUtil.Buscar<MusicaManager>())
                {
                    var a = mm.GetComponent<AudioSource>();
                    if (a != null) ambiente.Add(a);
                }
            }
            foreach (var a in ambiente)
                if (a != null) a.volume = vol * factorAmbiente;
        }
    }
}
