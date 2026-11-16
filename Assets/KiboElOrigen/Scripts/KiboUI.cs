using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace KiboElOrigen
{
    // Video de inicio -> botón Omitir a los 8 s -> Menú con botón Jugar -> HUD de mensajes.
    public class KiboUI : MonoBehaviour
    {
        public static KiboUI Instancia;

        [Header("Video de inicio")]
        public GameObject panelVideo;
        public VideoPlayer videoPlayer;
        public RawImage pantallaVideo;
        public Button botonOmitir;
        [Tooltip("Segundos antes de que aparezca el botón Omitir")]
        public float segundosParaOmitir = 8f;

        [Header("Menú de inicio")]
        public GameObject panelMenu;
        public Button botonJugar;

        [Header("Mensajes en pantalla")]
        public GameObject panelMensaje;
        public Text textoMensaje;
        public Text textoAyuda;
        public Text textoPuntaje;

        CanvasGroup grupoMensaje;
        Image fondoMensaje;
        Coroutine rutinaMensaje;
        RenderTexture texturaVideo;

        bool enMenu = true;
        bool cursorCapturado;
        CursorLockMode cursorJuego = CursorLockMode.None;
        bool cursorVisibleJuego = true;

        void Awake()
        {
            Instancia = this;
            if (botonJugar) botonJugar.onClick.AddListener(Jugar);
            if (botonOmitir) botonOmitir.onClick.AddListener(Omitir);
            if (panelMensaje)
            {
                grupoMensaje = panelMensaje.GetComponent<CanvasGroup>();
                if (!grupoMensaje) grupoMensaje = panelMensaje.AddComponent<CanvasGroup>();
                grupoMensaje.alpha = 0f;
                grupoMensaje.blocksRaycasts = false;
                fondoMensaje = panelMensaje.GetComponent<Image>();
            }
            if (textoAyuda) textoAyuda.text = "";
            if (textoPuntaje) textoPuntaje.text = "";
            if (panelMenu) panelMenu.SetActive(false);
            if (panelVideo) panelVideo.SetActive(false);
        }

        void Start()
        {
            Time.timeScale = 0f;
            enMenu = true;
            bool hayVideo = videoPlayer && (videoPlayer.clip != null || !string.IsNullOrEmpty(videoPlayer.url));
            if (hayVideo) IniciarVideo();
            else MostrarMenu();
        }

        void Update()
        {
            // Guarda cómo dejó el cursor tu controlador de jugador para devolverlo al jugar
            if (!cursorCapturado)
            {
                cursorJuego = Cursor.lockState;
                cursorVisibleJuego = Cursor.visible;
                cursorCapturado = true;
            }
            if (enMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // ---------- VIDEO ----------
        void IniciarVideo()
        {
            panelVideo.SetActive(true);
            if (botonOmitir) botonOmitir.gameObject.SetActive(false);

            int w = 1920, h = 1080;
            if (videoPlayer.clip != null && videoPlayer.clip.width > 0)
            {
                w = (int)videoPlayer.clip.width;
                h = (int)videoPlayer.clip.height;
            }
            texturaVideo = new RenderTexture(w, h, 0);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = texturaVideo;
            if (pantallaVideo)
            {
                pantallaVideo.texture = texturaVideo;
                var ajuste = pantallaVideo.GetComponent<AspectRatioFitter>();
                if (ajuste) ajuste.aspectRatio = w / (float)h;
            }
            videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += AlTerminarVideo;
            videoPlayer.errorReceived += AlErrorVideo;
            videoPlayer.Play();
            StartCoroutine(MostrarOmitir());
        }

        IEnumerator MostrarOmitir()
        {
            yield return new WaitForSecondsRealtime(segundosParaOmitir);
            if (panelVideo && panelVideo.activeSelf && botonOmitir) botonOmitir.gameObject.SetActive(true);
        }

        void AlTerminarVideo(VideoPlayer v) { TerminarVideo(); }

        void AlErrorVideo(VideoPlayer v, string error)
        {
            Debug.LogWarning("Kibo: no se pudo reproducir el video: " + error);
            TerminarVideo();
        }

        public void Omitir() { TerminarVideo(); }

        void TerminarVideo()
        {
            if (!panelVideo || !panelVideo.activeSelf) return;
            if (videoPlayer) videoPlayer.Stop();
            MostrarMenu();
        }

        // ---------- MENÚ ----------
        void MostrarMenu()
        {
            if (panelVideo) panelVideo.SetActive(false);
            if (panelMenu) panelMenu.SetActive(true);
            enMenu = true;
            Time.timeScale = 0f;
            if (!panelMenu) Jugar();
        }

        public void Jugar()
        {
            if (panelMenu) panelMenu.SetActive(false);
            enMenu = false;
            Time.timeScale = 1f;
            Cursor.lockState = cursorJuego;
            Cursor.visible = cursorVisibleJuego;
            if (KiboJuego.Instancia) KiboJuego.Instancia.Comenzar();
        }

        // ---------- MENSAJES ----------
        public void Mensaje(string texto, Color fondo, float segundos)
        {
            if (!textoMensaje || !grupoMensaje) return;
            textoMensaje.text = texto;
            if (fondoMensaje) fondoMensaje.color = fondo;
            if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
            rutinaMensaje = StartCoroutine(RutinaMensaje(segundos));
        }

        IEnumerator RutinaMensaje(float segundos)
        {
            float t = grupoMensaje.alpha;
            while (t < 1f) { t += Time.unscaledDeltaTime * 6f; grupoMensaje.alpha = t; yield return null; }
            grupoMensaje.alpha = 1f;
            yield return new WaitForSecondsRealtime(segundos);
            while (t > 0f) { t -= Time.unscaledDeltaTime * 2.5f; grupoMensaje.alpha = t; yield return null; }
            grupoMensaje.alpha = 0f;
        }

        public void Ayuda(string texto)
        {
            if (textoAyuda && textoAyuda.text != texto) textoAyuda.text = texto;
        }

        public void Puntaje(int bien, int fallos)
        {
            if (textoPuntaje) textoPuntaje.text = "Bien: " + bien + "     Fallos: " + fallos;
        }

        void OnDestroy()
        {
            if (texturaVideo) texturaVideo.Release();
        }
    }
}
