using System.Collections;
using UnityEngine;

public class SpawnCajaTapa : MonoBehaviour
{
    public GameObject cajaTapa;

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
        if (yaSalio || cajaTapa == null)
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
        StartCoroutine(FlotarCajaTapa());
    }

    // Siempre por encima del cofre y del terreno, sin importar dónde empiece el residuo
    Vector3 PuntoFinal()
    {
        Transform r = cajaTapa.transform;

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

    [Header("Aparecer frente a la cámara")]
    [Tooltip("Distancia frente a la cámara donde aparece el residuo antes de irse al inventario")]
    public float distanciaFrenteCamara = 1.6f;
    [Tooltip("Segundos que se queda girando frente a la cámara")]
    public float tiempoMostrando = 0.6f;

    // El residuo aparece centrado frente a la cámara, gira y se va solo al inventario
    IEnumerator FlotarCajaTapa()
    {
        cajaTapa.SetActive(true);
        Transform r = cajaTapa.transform;
        Camera camara = Camera.main;

        if (camara == null)
        {
            r.position = PuntoFinal();
            yield break;
        }

        Vector3 escala0 = r.localScale;
        Vector3 desfase = Vector3.zero;          // pivote vs centro del dibujo
        var rend = r.GetComponentInChildren<Renderer>();
        if (rend != null) desfase = r.position - rend.bounds.center;

        float t = 0f;
        while (t < duracion + tiempoMostrando)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duracion));
            // crece con un pequeño rebote
            float s = k < 1f ? 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f) : 1f;
            s = Mathf.Max(0.01f, s);
            Transform cam = camara != null ? camara.transform : null;
            if (cam == null) break;
            r.localScale = escala0 * s;
            r.position = cam.position + cam.forward * distanciaFrenteCamara + desfase * s;
            r.Rotate(0f, velocidadRotacion * 2f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        r.localScale = escala0;

        // Se va solo al inventario (con el efecto de teletransporte)
        var recoger = Object.FindAnyObjectByType<PlayerPickUp>();
        if (recoger != null && recoger.RecogerAutomatico(cajaTapa))
            yield break;

        // Si la barra está llena, queda flotando sobre el cofre para recogerlo después
        r.position = PuntoFinal();
    }
}
