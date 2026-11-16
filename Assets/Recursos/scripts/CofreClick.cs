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

        luz.intensity = 0f;
        /*Asignar*/
        if (tarjeta != null) tarjeta.SetActive(false);
    }

    void OnMouseDown()
    {
        if (abierto) return;

        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
            return;

        abierto = true;

        anim.SetTrigger("Abrir");

        Invoke("EncenderLuz", 0.9f);

        Invoke("MostrarTarjeta", 1f);
    }

    void EncenderLuz()
    {
        luz.intensity = 3f;
    }

    void MostrarTarjeta()
    {
        tarjeta.SetActive(true);

        StartCoroutine(AnimarTarjeta());
    }

    IEnumerator AnimarTarjeta()
    {
        // posición frente a la cámara
        Vector3 posicionFinal =
        camaraJugador.position +
        camaraJugador.forward * 2f +
        Vector3.up * 0.2f;

        tarjeta.transform.position = posicionFinal;

        // empieza girada
        tarjeta.transform.rotation =
            Quaternion.Euler(0f, 720f, 0f);

        // rotación final mirando a la cámara
        Quaternion rotacionFinal =
            Quaternion.LookRotation(camaraJugador.forward) *
            Quaternion.Euler(0, 180, 0);

        float tiempo = 0f;
        float duracion = 1.2f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;

            float t = tiempo / duracion;

            // giro suave
            tarjeta.transform.rotation =
                Quaternion.Slerp(
                    tarjeta.transform.rotation,
                    rotacionFinal,
                    t * 0.2f
                );

            // pequeño rebote/flotación
            tarjeta.transform.position =
                posicionFinal +
                Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.15f;

            // efecto pop de escala
            float escala =
                Mathf.Lerp(0.2f, 1f, t);

            tarjeta.transform.localScale =
                new Vector3(escala, escala, escala);

            yield return null;
        }

        tarjeta.transform.position = posicionFinal;

        tarjeta.transform.rotation = rotacionFinal;

        tarjeta.transform.localScale = Vector3.one;
    }

    public void ReiniciarCofre()
    {
        abierto = false;

        anim.Rebind();
        anim.Update(0f);

        transform.rotation = rotacionInicial;

        if (luz != null)
        {
            luz.intensity = 0f;
        }

        if (tarjeta != null)
        {
            tarjeta.SetActive(false);
        }
    }

}