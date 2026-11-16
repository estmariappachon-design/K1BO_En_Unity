using System.Collections;
using UnityEngine;

public class SpawnCajaTapa : MonoBehaviour
{
    public GameObject cajaTapa;

    public float altura = 0.5f;
    public float duracion = 1f;
    public float velocidadRotacion = 120f;

    public float retraso = 0.5f;

    bool yaSalio = false;

    Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = cajaTapa.transform.position;
    }

    void OnMouseDown()
    {
        if (yaSalio)
            return;

        yaSalio = true;

        posicionInicial = cajaTapa.transform.position;

        Invoke("IniciarFlotacion", retraso);
    }

    void IniciarFlotacion()
    {
        StartCoroutine(FlotarCajaTapa());
    }

    IEnumerator FlotarCajaTapa()
    {
        Vector3 destino = posicionInicial + Vector3.up * altura;

        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;

            cajaTapa.transform.position =
                Vector3.Lerp(posicionInicial, destino, t / duracion);

            cajaTapa.transform.Rotate(
                0,
                velocidadRotacion * Time.deltaTime,
                0
            );

            yield return null;
        }
    }
}