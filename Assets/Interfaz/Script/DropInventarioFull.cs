using UnityEngine;
using UnityEngine.EventSystems;

public class DropInventarioFull : MonoBehaviour, IDropHandler
{
    public InventarioFullUI inventarioFull;

    public void OnDrop(PointerEventData eventData)
    {
        ItemArrastrable item = eventData.pointerDrag.GetComponent<ItemArrastrable>();

        if (item != null && inventarioFull != null)
        {
            inventarioFull.AgregarItem(item.itemData);
            Debug.Log("Item guardado en inventario full");
        }
    }
}