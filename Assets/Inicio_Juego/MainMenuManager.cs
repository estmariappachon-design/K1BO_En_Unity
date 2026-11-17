using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using KiboElOrigen;

public class MainMenuManager : MonoBehaviour
{
    public GameObject menuUI;
    public VideoPlayer videoPlayer;

    [Header("Botón Omitir (abajo a la derecha durante el video)")]
    [Tooltip("Si lo dejas vacío se crea solo al iniciar")]
    public Button botonOmitir;
    [Tooltip("Segundos de video antes de que aparezca el botón Omitir")]
    public float segundosParaOmitir = 3f;

    [Header("Menú")]
    [Tooltip("Crea los botones CONTINUAR (si hay partida guardada) y SALIR, y activa el botón de ajustes")]
    public bool crearBotonesExtra = true;
    [Tooltip("Escena del juego")]
    public string escenaJuego = "MainScene";

    public static bool ReproduciendoVideo { get; private set; }

    static int cuadroUltimoEscape = -1;
    bool cargando;
    GameObject panelAjustes;
    Canvas canvas;

    // ExitGame lo usa para no cerrar el juego cuando ESC ya cerró una ventana
    public static bool EscapeOcupado()
    {
        return ReproduciendoVideo || cuadroUltimoEscape == Time.frameCount || GameObject.Find("KIBO_Ajustes") != null;
    }

    void Awake()
    {
        ReproduciendoVideo = false;
        Time.timeScale = 1f;
        KiboJuego.UIAbierta = false;
    }

    void Start()
    {
        canvas = BuscarCanvas();

        if (botonOmitir == null && canvas != null) botonOmitir = CrearBotonOmitir(canvas.transform);
        if (botonOmitir != null)
        {
            botonOmitir.onClick.RemoveListener(Omitir);
            botonOmitir.onClick.AddListener(Omitir);
            botonOmitir.gameObject.SetActive(false);
        }

        if (crearBotonesExtra) PrepararMenu();

        // Música del menú
        KiboMusica.Poner("musica_menu");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (panelAjustes != null && Input.GetKeyDown(KeyCode.Escape))
        {
            cuadroUltimoEscape = Time.frameCount;
            CerrarAjustes();
        }
    }

    Canvas BuscarCanvas()
    {
        if (menuUI != null)
        {
            var c = menuUI.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas;
        }
        return Object.FindAnyObjectByType<Canvas>();
    }

    // ------------------------------------------------------------------
    //  JUGAR (partida nueva): video de inicio y luego el juego
    // ------------------------------------------------------------------
    public void PlayGame()
    {
        if (cargando || ReproduciendoVideo) return;

        KiboGuardado.CargarAlIniciar = false;
        KiboGuardado.Borrar();

        bool hayVideo = videoPlayer != null && (videoPlayer.clip != null || !string.IsNullOrEmpty(videoPlayer.url));
        if (!hayVideo)
        {
            IrAlJuego();
            return;
        }

        if (menuUI != null) menuUI.SetActive(false);
        if (panelAjustes != null) CerrarAjustes();
        KiboMusica.Detener(); // el video tiene su propio sonido

        videoPlayer.loopPointReached -= EndVideo;
        videoPlayer.loopPointReached += EndVideo;
        videoPlayer.errorReceived -= ErrorVideo;
        videoPlayer.errorReceived += ErrorVideo;

        videoPlayer.Play();
        ReproduciendoVideo = true;
        StartCoroutine(MostrarOmitir());
    }

    public void ContinuarPartida()
    {
        if (cargando) return;
        KiboGuardado.CargarAlIniciar = true;
        IrAlJuego();
    }

    IEnumerator MostrarOmitir()
    {
        yield return new WaitForSecondsRealtime(segundosParaOmitir);
        if (!ReproduciendoVideo || botonOmitir == null) yield break;

        botonOmitir.gameObject.SetActive(true);
        botonOmitir.transform.SetAsLastSibling();

        // Entra deslizándose desde la derecha
        var rt = (RectTransform)botonOmitir.transform;
        Vector2 fin = rt.anchoredPosition;
        Vector2 ini = fin + new Vector2(420, 0);
        for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / 0.35f, 3f);
            rt.anchoredPosition = Vector2.Lerp(ini, fin, k);
            yield return null;
        }
        rt.anchoredPosition = fin;
    }

    public void Omitir()
    {
        if (!ReproduciendoVideo) return;
        if (videoPlayer != null) videoPlayer.Pause(); // deja la imagen mientras se oscurece
        IrAlJuego();
    }

    void EndVideo(VideoPlayer vp)
    {
        IrAlJuego();
    }

    void ErrorVideo(VideoPlayer vp, string mensaje)
    {
        Debug.LogWarning("No se pudo reproducir el video de inicio: " + mensaje);
        IrAlJuego();
    }

    void IrAlJuego()
    {
        if (cargando) return;
        cargando = true;
        ReproduciendoVideo = false;
        KiboMusica.Detener();
        if (botonOmitir != null) botonOmitir.interactable = false;
        KiboTransicion.Cargar(escenaJuego);
    }

    // ------------------------------------------------------------------
    //  Botones del menú
    // ------------------------------------------------------------------
    void PrepararMenu()
    {
        Transform padre = menuUI != null ? menuUI.transform : (canvas != null ? canvas.transform : null);
        if (padre == null) return;

        // Ajustes: el engranaje que estaba desactivado
        GameObject engranaje = null;
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
            if (t.name == "Btn_Configuracion") { engranaje = t.gameObject; break; }
        if (engranaje != null && engranaje.GetComponent<Button>() != null)
        {
            var b = engranaje.GetComponent<Button>();
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            b.onClick.AddListener(AbrirAjustes);
            engranaje.SetActive(true);
            if (engranaje.GetComponent<KiboBotonEfecto>() == null) engranaje.AddComponent<KiboBotonEfecto>();
        }
        else
        {
            var b = KiboEstilo.Boton(padre, "Btn_Ajustes_Kibo", "AJUSTES", Vector2.zero, new Vector2(260, 84), AbrirAjustes, 40);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-170, -80);
        }

        // Continuar (solo si hay una partida guardada)
        if (KiboGuardado.HayPartida)
        {
            var b = KiboEstilo.Boton(padre, "Btn_Continuar", "CONTINUAR", Vector2.zero, new Vector2(336, 96), ContinuarPartida, 50);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 292);
        }

        // Salir
        var bs = KiboEstilo.Boton(padre, "Btn_Salir", "SALIR", Vector2.zero, new Vector2(230, 76), Salir, 40);
        var rs = (RectTransform)bs.transform;
        rs.anchorMin = rs.anchorMax = new Vector2(0.5f, 0);
        rs.anchoredPosition = new Vector2(0, 52);
    }

    public void AbrirAjustes()
    {
        if (panelAjustes != null || canvas == null) return;
        panelAjustes = KiboAjustes.CrearPanel(canvas.transform, () => { panelAjustes = null; }, KiboEstilo.EscalaInterfaz);
        panelAjustes.transform.SetAsLastSibling();
    }

    void CerrarAjustes()
    {
        if (panelAjustes == null) return;
        Destroy(panelAjustes);
        panelAjustes = null;
        PlayerPrefs.Save();
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Botón OMITIR con el mismo estilo de madera (lo usa también el menú "Crear Todo")
    public static Button CrearBotonOmitir(Transform canvas)
    {
        var b = KiboEstilo.Boton(canvas, "Btn_Omitir", "OMITIR  >>", Vector2.zero, new Vector2(270, 84), null, 40);
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-50, 45);
        b.gameObject.SetActive(false);
        return b;
    }
}
