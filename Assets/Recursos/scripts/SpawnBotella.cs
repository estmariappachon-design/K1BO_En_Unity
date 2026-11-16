//using System.Collections;
//using UnityEngine;

//public class SpawnBotella : MonoBehaviour
//{
//    public GameObject botella;

//    public float altura = 4f;
//    public float duracion = 1f;
//    public float velocidadRotacion = 120f;

//    bool yaSalio = false;

//    void OnMouseDown()
//    {
//        if (yaSalio)
//            return;

//        yaSalio = true;

//        StartCoroutine(FlotarBotella());
//    }

//    IEnumerator FlotarBotella()
//    {
//        botella.SetActive(true);

//        Vector3 inicio = botella.transform.position;
//        Vector3 destino = inicio + Vector3.up * altura;

//        float t = 0f;

//        while (t < duracion)
//        {
//            t += Time.deltaTime;

//            botella.transform.position =
//                Vector3.Lerp(inicio, destino, t / duracion);

//            botella.transform.Rotate(
//                0,
//                velocidadRotacion * Time.deltaTime,
//                0
//            );

//            yield return null;
//        }
//    }
//}

using System.Collections;
using UnityEngine;

public class SpawnBotella : MonoBehaviour
{
    public GameObject botella;

    public float altura = 4f;
    public float duracion = 1f;
    public float velocidadRotacion = 120f;

    public float retraso = 0.5f;

    bool yaSalio = false;

    void OnMouseDown()
    {
        if (yaSalio)
            return;

        yaSalio = true;

        Invoke("IniciarFlotacion", retraso);
    }

    void IniciarFlotacion()
    {
        StartCoroutine(FlotarBotella());
    }

    IEnumerator FlotarBotella()
    {
        botella.SetActive(true);

        Vector3 inicio = botella.transform.position;
        Vector3 destino = inicio + Vector3.up * altura;

        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;

            botella.transform.position =
                Vector3.Lerp(inicio, destino, t / duracion);

            botella.transform.Rotate(
                0,
                velocidadRotacion * Time.deltaTime,
                0
            );

            yield return null;
        }
    }
}