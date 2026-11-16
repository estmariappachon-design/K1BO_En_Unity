using System.Collections;
using UnityEngine;

public class CofreResiduo : MonoBehaviour
{
    public float altura = 2f;
    public float duracion = 1f;
    public float velocidadRotacion = 120f;

    public void Flotar()
    {
        StartCoroutine(Subir());
    }

    IEnumerator Subir()
    {
        Vector3 inicio = transform.position;
        Vector3 destino = inicio + Vector3.up * altura;

        float t = 0;

        while (t < duracion)
        {
            t += Time.deltaTime;

            transform.position =
                Vector3.Lerp(inicio, destino, t / duracion);

            transform.Rotate(0, velocidadRotacion * Time.deltaTime, 0);

            yield return null;
        }
    }
}