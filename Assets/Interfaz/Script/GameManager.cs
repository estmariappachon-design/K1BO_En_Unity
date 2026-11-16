using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int vidasActuales = 3;
    public bool ganoRompecabezas = false;
    public bool estaEnMinijuego = false;
    public bool juegoIniciado = false;

    [Header("Game Over")]
    public GameObject panelGameOver;
    public Transform player;
    public Transform puntoInicio;

    public PlayerAnimations playerAnim;
    public InventarioFullUI inventarioFullUI;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip sonidoPerderVida;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void PerderVida()
    {
        vidasActuales--;

        if (audioSource != null && sonidoPerderVida != null)
        {
            audioSource.PlayOneShot(sonidoPerderVida);
        }

        if (vidasActuales < 0)
            vidasActuales = 0;

        Debug.Log("Vidas restantes: " + vidasActuales);

        if (vidasActuales <= 0)
        {
            Debug.Log("Se acabaron las vidas. Intentando mostrar Game Over");
            MostrarGameOver();
        }
    }

    public void GanarRompecabezas()
    {
        ganoRompecabezas = true;
        estaEnMinijuego = false;
    }

    public void EntrarMinijuego()
    {
        estaEnMinijuego = true;
    }

    public void SalirMinijuego()
    {
        estaEnMinijuego = false;
    }

    public void IniciarJuego()
    {
        juegoIniciado = true;
        Debug.Log("Juego iniciado: la energía ya puede bajar");
    }

    public void PausarJuego()
    {
        juegoIniciado = false;
        Debug.Log("Juego pausado: la energía no baja");
    }

    public void MostrarGameOver()
    {
        Debug.Log("Entró a MostrarGameOver");

        if (panelGameOver == null)
        {
            GameObject[] todosLosObjetos = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (GameObject obj in todosLosObjetos)
            {
                if (obj.name == "Panel_GameOver")
                {
                    panelGameOver = obj;
                    break;
                }
            }
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
            Debug.Log("Panel_GameOver activado");
        }
        else
        {
            Debug.LogError("No encontró Panel_GameOver ni siquiera inactivo");
        }

        Time.timeScale = 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene")
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            foreach (GameObject root in rootObjects)
            {
                Transform[] children = root.GetComponentsInChildren<Transform>(true);

                foreach (Transform child in children)
                {
                    if (child.name == "Panel_GameOver")
                    {
                        panelGameOver = child.gameObject;
                        break;
                    }
                }

                if (panelGameOver != null)
                    break;
            }

            if (panelGameOver != null)
            {
                panelGameOver.SetActive(false);
                Debug.Log("Panel_GameOver encontrado en MainScene");
            }
            else
            {
                Debug.LogError("No se encontró Panel_GameOver en MainScene");
            }
        }
    }

    public void ReiniciarJugador()
    {
        Time.timeScale = 1f;

        vidasActuales = 3;
        estaEnMinijuego = false;
        ganoRompecabezas = false;

        Debug.Log("Vidas reiniciadas en GameManager: " + vidasActuales);

        if (player != null && puntoInicio != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.position = puntoInicio.position;
                rb.rotation = puntoInicio.rotation;
            }
            else
            {
                player.position = puntoInicio.position;
                player.rotation = puntoInicio.rotation;
            }
        }

        if (playerAnim != null)
        {
            playerAnim.ReiniciarEstadoJugador();
            playerAnim.SincronizarVidasDesdeGameManager();
        }
        else
        {
            Debug.LogError("No está asignado PlayerAnimations en GameManager");
        }

        InventarioUI inventarioUI = FindAnyObjectByType<InventarioUI>();
        if (inventarioUI != null)
        {
            inventarioUI.VaciarBarraInventario();
        }

        if (inventarioFullUI != null)
        {
            inventarioFullUI.VaciarInventarioFull();
        }
        else
        {
            Debug.LogError("No está asignado InventarioFullUI en GameManager");
        }

        ObjetoRecolectableRespawn[] objetos =
            Resources.FindObjectsOfTypeAll<ObjetoRecolectableRespawn>();

        foreach (ObjetoRecolectableRespawn obj in objetos)
        {
            if (obj.gameObject.scene.IsValid())
            {
                obj.ReiniciarObjeto();
            }
        }

        CofreClick[] cofres =
            Resources.FindObjectsOfTypeAll<CofreClick>();

        foreach (CofreClick cofre in cofres)
        {
            if (cofre.gameObject.scene.IsValid())
            {
                cofre.ReiniciarCofre();
            }
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }
    }
}