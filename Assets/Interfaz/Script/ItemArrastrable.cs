using UnityEngine;
using UnityEngine.EventSystems;

public class ItemArrastrable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData itemData;
    public InventarioFullUI inventarioFull;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector3 posicionInicial;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        posicionInicial = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        inventarioFull.AgregarItem(itemData);
        transform.position = posicionInicial;
    }
}