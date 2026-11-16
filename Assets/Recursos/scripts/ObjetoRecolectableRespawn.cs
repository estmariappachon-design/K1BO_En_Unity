using UnityEngine;

public class ObjetoRecolectableRespawn : MonoBehaviour
{
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private Transform padreInicial;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
        padreInicial = transform.parent;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void OcultarObjeto()
    {
        gameObject.SetActive(false);
    }

    public void ReiniciarObjeto()
    {
        transform.SetParent(padreInicial);
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
            col.enabled = true;

        gameObject.SetActive(true);
    }
}