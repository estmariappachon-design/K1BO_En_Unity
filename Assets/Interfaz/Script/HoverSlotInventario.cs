using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSlotInventario : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float escalaHover = 1.12f;
    public float velocidad = 10f;

    private Vector3 escalaOriginal;
    private Vector3 escalaObjetivo;

    void Start()
    {
        escalaOriginal = transform.localScale;
        escalaObjetivo = escalaOriginal;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidad
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        escalaObjetivo = escalaOriginal * escalaHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        escalaObjetivo = escalaOriginal;
    }
}