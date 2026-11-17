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

    [Tooltip("Cuánto sube el residuo al abrir el cofre")]
    public float altura = 1f;
    [Tooltip("(Ya no se usa) antes limitaba la subida")]
    public float alturaMaxima = 1.1f;
    [Tooltip("Qué tan arriba de la tapa del cofre queda flotando el residuo (metros)")]
    public float alturaSobreElCofre = 0.8f;
    public float duracion = 1f;
    public float velocidadRotacion = 120f;

    public float retraso = 0.5f;

    bool yaSalio = false;

    void OnMouseDown()
    {
        if (yaSalio || botella == null)
            return;

        // No abrir mientras hay una ventana abierta o el clic fue sobre la interfaz
        if (KiboElOrigen.KiboJuego.EntradaBloqueada)
            return;
        if (KiboElOrigen.KiboUtil.PunteroSobreUI())
            return;

        yaSalio = true;

        Invoke("IniciarFlotacion", retraso);
    }

    void IniciarFlotacion()
    {
        StartCoroutine(FlotarBotella());
    }

    // Siempre por encima del cofre y del terreno, sin importar dónde empiece el residuo
    Vector3 PuntoFinal()
    {
        Transform r = botella.transform;

        // parte de arriba del cofre (sin contar el residuo)
        float tope = transform.position.y;
        foreach (var rend in GetComponentsInChildren<Renderer>())
        {
            if (rend.transform.IsChildOf(r)) continue;
            if (rend is ParticleSystemRenderer) continue;
            tope = Mathf.Max(tope, rend.bounds.max.y);
        }

        // mitad de la altura del residuo
        float mitad = 0.15f;
        Vector3 centroResiduo = r.position;
        var rr = r.GetComponentInChildren<Renderer>();
        if (rr != null)
        {
            mitad = rr.bounds.extents.y;
            centroResiduo = rr.bounds.center;
        }
        float desfase = r.position.y - centroResiduo.y; // pivote vs centro del dibujo

        Vector3 destino = new Vector3(transform.position.x, tope + alturaSobreElCofre + mitad + desfase, transform.position.z);

        // por si el terreno está más alto en ese punto
        RaycastHit hit;
        if (Physics.Raycast(destino + Vector3.up * 10f, Vector3.down, out hit, 30f, ~0, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(r))
        {
            float minimo = hit.point.y + 0.6f + mitad + desfase;
            if (destino.y < minimo) destino.y = minimo;
        }
        return destino;
    }

    IEnumerator FlotarBotella()
    {
        botella.SetActive(true);

        Vector3 inicio = botella.transform.position;
        Vector3 destino = PuntoFinal();

        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;

            // sube rápido y frena suave al final
            float k = Mathf.Clamp01(t / duracion);
            float suave = 1f - Mathf.Pow(1f - k, 3f);

            botella.transform.position = Vector3.Lerp(inicio, destino, suave);

            botella.transform.Rotate(
                0,
                velocidadRotacion * Time.deltaTime,
                0
            );

            yield return null;
        }

        botella.transform.position = destino;
    }
}
