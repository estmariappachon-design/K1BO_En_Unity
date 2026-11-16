using UnityEngine;
using UnityEngine.EventSystems;

public class DragItemBarra : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SlotBarraInventario slotBarra;
    public InventarioFullUI inventarioFull;

    [Header("Panel Inventario Full")]
    public GameObject panelInventarioFull;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 posicionInicial;
    private Vector2 offset;
    private Transform padreOriginal;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotBarra.EstaVacio()) return;

        posicionInicial = rectTransform.anchoredPosition;
        padreOriginal = transform.parent;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        Vector2 mouseLocal;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mouseLocal
        );

        offset = rectTransform.anchoredPosition - mouseLocal;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (slotBarra.EstaVacio()) return;

        Vector2 mouseLocal;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out mouseLocal
        );

        rectTransform.anchoredPosition = mouseLocal + offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (slotBarra.EstaVacio()) return;

        bool inventarioFullAbierto = panelInventarioFull != null && panelInventarioFull.activeInHierarchy;

        if (inventarioFullAbierto)
        {
            inventarioFull.AgregarItem(slotBarra.itemActual);
            slotBarra.VaciarSlot();
        }
        else
        {
            Debug.Log("Inventario Full cerrado: el item vuelve a la barra");
        }

        transform.SetParent(padreOriginal, true);
        rectTransform.anchoredPosition = posicionInicial;
    }
}