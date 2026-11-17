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

    void Start()
    {
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
    [Tooltip("Distancia de la tarjeta frente a la cámara")]
    public float distanciaTarjeta = 2f;
    [Tooltip("Altura de la tarjeta respecto al centro de la pantalla")]
    public float alturaTarjeta = 0f;
    [Tooltip("Sube la tarjeta en la pantalla (más alto = más arriba)")]
    public float subirTarjeta = 0.5f;

    Transform CamaraActual()
    {
        // Siempre la cámara que está mirando el jugador
        if (Camera.main != null) return Camera.main.transform;
        return camaraJugador;
    }

    void MostrarTarjeta()
    {
        if (tarjeta == null || CamaraActual() == null) return;
        tarjeta.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(AnimarTarjeta());
    }

    Vector3 PosicionFrente(Transform cam)
    {
        return cam.position + cam.forward * distanciaTarjeta + cam.up * (alturaTarjeta + subirTarjeta);
    }

    Quaternion RotacionFrente(Transform cam)
    {
        // mirando a la cámara (igual que antes)
        return Quaternion.LookRotation(cam.forward, cam.up) * Quaternion.Euler(0, 180, 0);
    }

    IEnumerator AnimarTarjeta()
    {
        Transform cam = CamaraActual();
        Quaternion giroInicial = Quaternion.Euler(0f, 540f, 0f);

        float tiempo = 0f;
        float duracion = 1f;

        // Aparece girando y creciendo, SIEMPRE frente a la cámara
        while (tiempo < duracion && tarjeta.activeInHierarchy)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            float suave = 1f - Mathf.Pow(1f - t, 3f);

            cam = CamaraActual();
            Quaternion final = RotacionFrente(cam);
            tarjeta.transform.rotation = Quaternion.Slerp(final * giroInicial, final, suave);
            tarjeta.transform.position = PosicionFrente(cam) + cam.up * (Mathf.Sin(t * Mathf.PI) * 0.12f);
            float escala = Mathf.Lerp(0.2f, 1f, suave);
            tarjeta.transform.localScale = new Vector3(escala, escala, escala);

            yield return null;
        }

        tarjeta.transform.localScale = Vector3.one;

        // Se queda frente a la cámara aunque la cámara se mueva, hasta que se haga clic en ella
        while (tarjeta.activeInHierarchy)
        {
            cam = CamaraActual();
            float flota = Mathf.Sin(Time.time * 2f) * 0.03f;
            tarjeta.transform.position = Vector3.Lerp(tarjeta.transform.position, PosicionFrente(cam) + cam.up * flota, 12f * Time.deltaTime);
            tarjeta.transform.rotation = Quaternion.Slerp(tarjeta.transform.rotation, RotacionFrente(cam), 12f * Time.deltaTime);
            yield return null;
        }
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