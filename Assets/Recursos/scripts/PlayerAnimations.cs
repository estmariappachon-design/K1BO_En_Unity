using UnityEngine;
using UnityEngine.UI;

public class PlayerAnimations : MonoBehaviour
{
    private Animator anim;
    public AnimacionesOjos animacionOjosUI;

    [Header("Efectos")]
    public ParticleSystem particles;
    public Transform model;

    [Header("Movimiento Vertical")]
    public float speed = 5f;

    [Header("Bloquear movimiento al dormir")]
    public MonoBehaviour[] scriptsMovimiento;

    [Header("Vidas")]
    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonVacio;
    public int vidasActuales = 3;

    [Header("Energia")]
    public Slider sliderEnergia;
    public Image fillEnergia;

    public float energiaMaxima = 100f;
    public float energiaActual = 100f;

    private bool puedePerderVidaPorEnergia = true;
    private Coroutine rutinaReinicioEnergia;

    [Header("Sonidos Energía")]
    public AudioSource audioSource;
    public AudioClip sonidoEnergiaBaja;
    public float porcentajeAvisoEnergia = 0.2f;

    private bool yaSonoAvisoEnergia = false;

    // 100 / 120 = dura 2 minutos
    public float perdidaEnergiaPorSegundo = 0.555f;

    // Más bajo = recarga más lento
    public float recuperacionEnergiaPorSegundo = 0.6f;

    private float originalY;
    private bool isSleeping = false;
    private bool estaEnMinijuego = false;

    private float tiempoProteccionReinicio = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();

        if (model != null)
            originalY = model.localPosition.y;

        if (particles == null)
            particles = GetComponentInChildren<ParticleSystem>();

        energiaActual = energiaMaxima;
        ActualizarBarraEnergia();

        if (GameManager.Instance != null)
        {
            vidasActuales = GameManager.Instance.vidasActuales;
        }
        else
        {
            vidasActuales = corazones.Length;
        }

        ActualizarVidas();
    }

    void Update()
    {
        // ENTRAR A DORMIR
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (!isSleeping)
            {
                if (EstaQuieto())
                {
                    Dormir();
                }
                else
                {
                    Debug.Log("No puedes dormir mientras el personaje se está moviendo");
                }
            }
        }

        // DESPERTAR
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (isSleeping)
            {
                Despertar();
            }
        }

        // CONTROL ENERGÍA
        if (tiempoProteccionReinicio > 0)
        {
            tiempoProteccionReinicio -= Time.deltaTime;
        }
        else if (!estaEnMinijuego)
        {
            ActualizarEnergia();
        }

        // MOVIMIENTO VISUAL
        ActualizarMovimientoVertical();

        // SINCRONIZAR VIDAS
        SincronizarVidasDesdeGameManager();
    }

    void Dormir()
    {
        anim.SetTrigger("Dormir");
        animacionOjosUI.Dormir();

        if (particles != null)
            particles.Stop();

        isSleeping = true;
        BloquearMovimiento(true);
    }

    void Despertar()
    {
        anim.SetTrigger("Despertar");
        animacionOjosUI.Despertar();

        if (particles != null)
            particles.Play();

        isSleeping = false;
        BloquearMovimiento(false);
    }

    void ActualizarEnergia()
    {
        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
        {
            return;
        }

        if (isSleeping)
        {
            energiaActual += recuperacionEnergiaPorSegundo * Time.deltaTime;
        }
        else
        {
            energiaActual -= perdidaEnergiaPorSegundo * Time.deltaTime;
        }

        energiaActual = Mathf.Clamp(energiaActual, 0, energiaMaxima);

        float porcentajeEnergia = energiaActual / energiaMaxima;

        if (porcentajeEnergia <= porcentajeAvisoEnergia && !yaSonoAvisoEnergia && energiaActual > 0)
        {
            if (audioSource != null && sonidoEnergiaBaja != null)
            {
                audioSource.PlayOneShot(sonidoEnergiaBaja);
            }

            yaSonoAvisoEnergia = true;
        }

        if (porcentajeEnergia > porcentajeAvisoEnergia)
        {
            yaSonoAvisoEnergia = false;
        }

        if (energiaActual <= 0 && puedePerderVidaPorEnergia)
        {
            puedePerderVidaPorEnergia = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PerderVida();
            }

            if (GameManager.Instance != null && GameManager.Instance.vidasActuales > 0)
            {
                energiaActual = energiaMaxima;
            }
            else
            {
                energiaActual = 0;
                Debug.Log("Game Over");
            }
        }

        if (energiaActual > 1f)
        {
            puedePerderVidaPorEnergia = true;
        }

        ActualizarBarraEnergia();
    }

    void ActualizarBarraEnergia()
    {
        if (sliderEnergia != null)
        {
            sliderEnergia.value = energiaActual;
        }

        if (fillEnergia != null)
        {
            float porcentaje = energiaActual / energiaMaxima;

            // 🔥 Color amarillo original
            Color amarilloEnergia = new Color(1f, 0.8745099f, 0.0627451f);

            // 🔥 Transición suave amarillo → rojo
            fillEnergia.color = Color.Lerp(
                Color.red,
                amarilloEnergia,
                porcentaje
            );
        }
    }

    void ActualizarMovimientoVertical()
    {
        if (model == null) return;

        float targetY = isSleeping ? -0.05f : originalY;

        Vector3 pos = model.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * speed);
        model.localPosition = pos;
    }

    void BloquearMovimiento(bool bloquear)
    {
        foreach (MonoBehaviour script in scriptsMovimiento)
        {
            if (script != null)
                script.enabled = !bloquear;
        }
    }

    public void EntrarMinijuego()
    {
        estaEnMinijuego = true;
    }

    public void SalirMinijuego()
    {
        estaEnMinijuego = false;
    }

    void PerderVida()
    {
        vidasActuales--;

        if (vidasActuales < 0)
            vidasActuales = 0;

        ActualizarVidas();

        if (vidasActuales > 0)
        {
            energiaActual = energiaMaxima;
        }
        else
        {
            Debug.Log("Game Over");
            energiaActual = 0;
        }

        ActualizarBarraEnergia();
    }

    void ActualizarVidas()
    {
        for (int i = 0; i < corazones.Length; i++)
        {
            if (i < vidasActuales)
            {
                corazones[i].sprite = corazonLleno;
            }
            else
            {
                corazones[i].sprite = corazonVacio;
            }
        }
    }

    public void SincronizarVidasDesdeGameManager()
    {
        if (GameManager.Instance == null) return;

        if (vidasActuales != GameManager.Instance.vidasActuales)
        {
            vidasActuales = GameManager.Instance.vidasActuales;
            ActualizarVidas();

            Debug.Log("HUD actualizado con vidas: " + vidasActuales);
        }
    }

    public void ReiniciarEstadoJugador()
    {
        vidasActuales = 3;

        energiaActual = energiaMaxima;
        tiempoProteccionReinicio = 0.5f;
        puedePerderVidaPorEnergia = true;

        yaSonoAvisoEnergia = false;

        isSleeping = false;
        BloquearMovimiento(false);

        if (sliderEnergia != null)
        {
            sliderEnergia.minValue = 0;
            sliderEnergia.maxValue = energiaMaxima;
            sliderEnergia.SetValueWithoutNotify(energiaMaxima);
            sliderEnergia.value = energiaMaxima;
        }

        ActualizarBarraEnergia();
        ActualizarVidas();

        Debug.Log("Energía reiniciada a: " + energiaActual);
    }

    System.Collections.IEnumerator ReiniciarBarraEnergia()
    {
        yield return null;

        if (sliderEnergia != null)
        {
            sliderEnergia.minValue = 0;
            sliderEnergia.maxValue = energiaMaxima;

            sliderEnergia.value = energiaMaxima;

            sliderEnergia.normalizedValue = 1f;
        }

        ActualizarBarraEnergia();

        Debug.Log("Barra energía reiniciada");
    }

    bool EstaQuieto()
    {
        if (scriptsMovimiento == null || scriptsMovimiento.Length == 0)
            return true;

        foreach (MonoBehaviour script in scriptsMovimiento)
        {
            if (script == null) continue;

            PlayerMovement movimiento = script as PlayerMovement;

            if (movimiento != null)
            {
                return movimiento.EstaQuieto();
            }
        }

        return true;
    }

}
