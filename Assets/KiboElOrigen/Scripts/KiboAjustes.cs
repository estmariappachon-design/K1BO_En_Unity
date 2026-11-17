using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Ajustes del juego guardados en PlayerPrefs (volumen, brillo, pantalla, calidad)
    public static class KiboAjustes
    {
        const string K_GENERAL = "Kibo_VolGeneral", K_MUSICA = "Kibo_VolMusica", K_EFECTOS = "Kibo_VolEfectos";
        const string K_BRILLO = "Brillo"; // misma clave que usa BrilloUI
        const string K_CALIDAD = "Kibo_Calidad";

        public static float VolumenGeneral
        {
            get { return PlayerPrefs.GetFloat(K_GENERAL, 1f); }
            set { PlayerPrefs.SetFloat(K_GENERAL, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); }
        }

        public static float VolumenMusica
        {
            get { return PlayerPrefs.GetFloat(K_MUSICA, 0.8f); }
            set { PlayerPrefs.SetFloat(K_MUSICA, Mathf.Clamp01(value)); AplicarMusica(); }
        }

        public static float VolumenEfectos
        {
            get { return PlayerPrefs.GetFloat(K_EFECTOS, 1f); }
            set { PlayerPrefs.SetFloat(K_EFECTOS, Mathf.Clamp01(value)); }
        }

        public static float Brillo
        {
            get { return PlayerPrefs.GetFloat(K_BRILLO, 1f); }
            set { PlayerPrefs.SetFloat(K_BRILLO, Mathf.Clamp01(value)); AplicarBrillo(); }
        }

        public static int Calidad
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(K_CALIDAD, QualitySettings.GetQualityLevel()), 0, Mathf.Max(0, QualitySettings.names.Length - 1)); }
            set
            {
                int v = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                PlayerPrefs.SetInt(K_CALIDAD, v);
                QualitySettings.SetQualityLevel(v, true);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            AplicarTodo();
        }

        static void AlCargarEscena(Scene s, LoadSceneMode m) { AplicarTodo(); }

        public static void AplicarTodo()
        {
            AudioListener.volume = VolumenGeneral;
            if (PlayerPrefs.HasKey(K_CALIDAD) && QualitySettings.GetQualityLevel() != Calidad) QualitySettings.SetQualityLevel(Calidad, true);
            AplicarMusica();
            AplicarBrillo();
        }

        static void AplicarMusica()
        {
            foreach (var m in KiboUtil.Buscar<MusicaManager>())
            {
                var a = m.GetComponent<AudioSource>();
                if (a != null) a.volume = VolumenMusica;
            }
        }

        static void AplicarBrillo()
        {
            foreach (var b in KiboUtil.Buscar<BrilloUI>())
            {
                if (b.sliderBrillo != null) b.sliderBrillo.SetValueWithoutNotify(Brillo);
                if (b.overlayBrillo != null) b.CambiarBrillo(Brillo);
            }
        }

        // ------------------------------------------------------------------
        //  Panel de ajustes (se usa en el menú de inicio y en la pausa)
        // ------------------------------------------------------------------
        public static GameObject CrearPanel(Transform canvas, System.Action alCerrar, float escala = 1f)
        {
            var capa = KiboEstilo.Lleno("KIBO_Ajustes", canvas);
            KiboEstilo.Oscurecer(capa, 0.55f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -30), new Vector2(1000, 800), "AJUSTES", true);
            panel.localScale = Vector3.one * escala;
            var c = KiboEstilo.Contenido(panel);

            System.Action cerrar = () =>
            {
                PlayerPrefs.Save();
                Object.Destroy(capa.gameObject);
                if (alCerrar != null) alCerrar();
            };
            KiboEstilo.BotonX(panel, new Vector2(-70, -70), 80, () => cerrar());

            float y = 250, paso = 80, xEt = -215, xBar = 170;
            Fila(c, "VOLUMEN GENERAL", ref y, paso, xEt, xBar, VolumenGeneral, v => VolumenGeneral = v);
            Fila(c, "MÚSICA", ref y, paso, xEt, xBar, VolumenMusica, v => VolumenMusica = v);
            Fila(c, "EFECTOS", ref y, paso, xEt, xBar, VolumenEfectos, v => { VolumenEfectos = v; KiboSonidos.Clic(); });
            Fila(c, "BRILLO", ref y, paso, xEt, xBar, Brillo, v => Brillo = Mathf.Max(0.05f, v));

            // Pantalla completa
            KiboEstilo.Etiqueta(c, "PANTALLA COMPLETA", new Vector2(xEt, y), new Vector2(360, 60), 40);
            TMP_Text txPantalla = null;
            var bPantalla = KiboEstilo.Boton(c, "Btn_Pantalla", Screen.fullScreen ? "SÍ" : "NO", new Vector2(xBar, y), new Vector2(200, 72), null, 40);
            txPantalla = bPantalla.GetComponentInChildren<TMP_Text>();
            bPantalla.onClick.AddListener(() =>
            {
                bool nuevo = !Screen.fullScreen;
                Screen.fullScreen = nuevo;
                txPantalla.text = nuevo ? "SÍ" : "NO";
            });
            y -= paso;

            // Calidad
            KiboEstilo.Etiqueta(c, "CALIDAD", new Vector2(xEt, y), new Vector2(360, 60), 40);
            var bCal = KiboEstilo.Boton(c, "Btn_Calidad", NombreCalidad(Calidad), new Vector2(xBar, y), new Vector2(340, 72), null, 38);
            var txCal = bCal.GetComponentInChildren<TMP_Text>();
            bCal.onClick.AddListener(() =>
            {
                int n = QualitySettings.names.Length;
                if (n == 0) return;
                Calidad = (Calidad + 1) % n;
                txCal.text = NombreCalidad(Calidad);
            });

            KiboEstilo.Boton(c, "Btn_Listo", "LISTO", new Vector2(0, -250), new Vector2(320, 90), () => cerrar(), 50);
            return capa.gameObject;
        }

        static void Fila(RectTransform c, string nombre, ref float y, float paso, float xEt, float xBar, float valor, UnityEngine.Events.UnityAction<float> alCambiar)
        {
            KiboEstilo.Etiqueta(c, nombre, new Vector2(xEt, y), new Vector2(360, 60), 40);
            KiboEstilo.Deslizador(c, "Slider_" + nombre, new Vector2(xBar, y), 420, valor, alCambiar);
            y -= paso;
        }

        static string NombreCalidad(int i)
        {
            var n = QualitySettings.names;
            if (i < 0 || i >= n.Length) return "NORMAL";
            string s = n[i].ToUpperInvariant();
            switch (s)
            {
                case "VERY LOW": return "MUY BAJA";
                case "LOW": return "BAJA";
                case "MEDIUM": return "MEDIA";
                case "HIGH": return "ALTA";
                case "VERY HIGH": return "MUY ALTA";
                case "ULTRA": return "ULTRA";
                default: return s;
            }
        }
    }
}
