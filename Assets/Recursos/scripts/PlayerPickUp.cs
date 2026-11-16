using System.Collections;
using UnityEngine;

public class PlayerPickUp : MonoBehaviour
{
    public Animator anim;
    public Transform handPoint;
    public Transform hombro;
    public InventarioUI inventarioUI;

    public float distanciaRayo = 10f;

    private GameObject objetoTemporal;

    private Vector3 objetivoDireccion;
    private bool mirarObjetivo = false;

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, distanciaRayo))
            {
                if (hit.collider.CompareTag("ObjetoAgarrable"))
                {
                    // 🔥 verificar espacio antes de recoger
                    if (inventarioUI != null && !inventarioUI.TieneEspacio())
                    {
                        Debug.Log("Inventario lleno");
                        return;
                    }

                    objetivoDireccion = hit.point;
                    mirarObjetivo = true;

                    anim.ResetTrigger("Agarrar");
                    anim.SetTrigger("Agarrar");

                    objetoTemporal = hit.collider.gameObject;

                    StartCoroutine(EsperarYAgarrar());
                }
            }
        }
    }

    void LateUpdate()
    {
        if (mirarObjetivo && hombro != null)
        {
            // 🔥 dirección hacia el punto del click
            Vector3 direccion = objetivoDireccion - hombro.position;

            if (direccion.magnitude > 0.01f)
            {
                direccion.Normalize();

                // 🔥 importante: usar rotación RELATIVA
                Quaternion rotObjetivo = Quaternion.LookRotation(direccion, Vector3.up);

                // 🔥 mezcla con la animación (clave)
                hombro.rotation = Quaternion.Slerp(
                    hombro.rotation,
                    rotObjetivo,
                    3f * Time.deltaTime
                );
            }
        }
    }

    IEnumerator EsperarYAgarrar()
    {
        yield return new WaitForSeconds(1f);

        if (objetoTemporal != null)
        {
            Rigidbody rb = objetoTemporal.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }

            objetoTemporal.transform.SetParent(handPoint, true);
            objetoTemporal.transform.position = handPoint.position;
            objetoTemporal.transform.rotation = handPoint.rotation;
        }

        yield return new WaitForSeconds(0.4f);

        if (objetoTemporal != null)
        {
            ItemRecolectable item = objetoTemporal.GetComponent<ItemRecolectable>();

            if (item != null && inventarioUI != null)
            {
                inventarioUI.AgregarItem(item.itemData);
            }

            ObjetoRecolectableRespawn respawn = objetoTemporal.GetComponent<ObjetoRecolectableRespawn>();

            if (respawn != null)
            {
                objetoTemporal.transform.SetParent(null);
                respawn.OcultarObjeto();
            }
            else
            {
                Destroy(objetoTemporal);
            }

            objetoTemporal = null;
        }

        mirarObjetivo = false;
    }
}