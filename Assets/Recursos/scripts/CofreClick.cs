using UnityEngine;
using System.Collections;

public class CofreClick : MonoBehaviour
{
    Animator anim;

    private bool abierto = false;
    private Quaternion rotacionInicial;

    public Light luz;

    public GameObject tarjeta;
    public Transform camaraJugador;

    public bool EstaAbierto { get { return abierto; } }

    void Start()
    {
        // brillo suave y efecto al pasar el mouse
        if (GetComponent<KiboElOrigen.KiboBrilloCofre>() == null) gameObject.AddComponent<KiboElOrigen.KiboBrilloCofre>();

        anim = GetComponent<Animator>();

        rotacionInicial = transform.rotation;

        if (luz != null) luz.intensity = 0f;
        /*Asignar*/
        if (tarjeta != null) tarjeta.SetActive(false);
    }

    void OnMouseDown()
    {
        if (abierto) return;

        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
            return;

        // No abrir mientras hay una ventana abierta o el clic fue sobre la interfaz
        if (KiboElOrigen.KiboJuego.EntradaBloqueada)
            return;
        if (KiboElOrigen.KiboUtil.PunteroSobreUI())
            return;

        abierto = true;

        if (anim != null) anim.SetTrigger("Abrir");

        Invoke("EncenderLuz", 0.9f);

        Invoke("MostrarTarjeta", 1f);
    }

    void EncenderLuz()
    {
        if (luz != null) luz.intensity = 3f;
    }

    [Header("Tarjeta del minijuego")]
    [Tooltip("Qué minijuego da este cofre. Alternar = una vez el rompecabezas y otra el del TransMilenio")]
    public TipoMinijuego minijuego = TipoMinijuego.Alternar;
    [Tooltip("Distancia de la tarjeta frente a la cámara")]
    public float distanciaTarjeta = 2f;
    [Tooltip("(Ya no se usa: la tarjeta sale centrada)")]
    public float alturaTarjeta = 0f;
    [Tooltip("(Ya no se usa: la tarjeta sale centrada)")]
    public float subirTarjeta = 0f;

    static int contadorAlternar;

    Transform CamaraActual()
    {
        // Siempre la cámara que está mirando el jugador
        if (Camera.main != null) return Camera.main.transform;
        return camaraJugador;
    }

    void MostrarTarjeta()
    {
        if (tarjeta == null || CamaraActual() == null) return;

        // Elige el minijuego de este cofre
        TipoMinijuego elegido = minijuego;
        if (elegido == TipoMinijuego.Alternar)
        {
            elegido = contadorAlternar % 2 == 0 ? TipoMinijuego.Transmilenio : TipoMinijuego.Rompecabezas;
            contadorAlternar++;
        }
        AbrirMinijuego.Siguiente = elegido;

        // La tarjeta 3D ya no se usa (se movía y empujaba al personaje).
        // Ahora sale una tarjeta 2D centrada en la pantalla con el botón JUGAR.
        tarjeta.SetActive(false);
        var abrir = tarjeta.GetComponent<AbrirMinijuego>();
        if (abrir == null) abrir = tarjeta.GetComponentInChildren<AbrirMinijuego>(true);
        GameObject blur = abrir != null ? abrir.panelBlur : null;

        if (KiboElOrigen.KiboUI.Instancia != null)
            KiboElOrigen.KiboUI.Instancia.MostrarTarjetaMinijuego(elegido, () => AbrirMinijuego.Abrir(elegido, blur), ReiniciarCofre);
        else
            AbrirMinijuego.Abrir(elegido, blur);
    }

    public void ReiniciarCofre()
    {
        abierto = false;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        transform.rotation = rotacionInicial;

        if (luz != null)
        {
            if (luz != null) luz.intensity = 0f;
        }

        if (tarjeta != null)
        {
            tarjeta.SetActive(false);
        }
    }

}