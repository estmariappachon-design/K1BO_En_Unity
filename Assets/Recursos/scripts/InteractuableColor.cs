using UnityEngine;

public class InteractuableColor : MonoBehaviour
{
    public Material matNormal;
    public Material matResaltado;

    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = matNormal;   // color normal al inicio
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.material = matResaltado;   // se ilumina
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rend.material = matNormal;     // vuelve a normal
        }
    }
}