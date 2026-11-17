using UnityEngine;
using UnityEngine.UI;

public class SlotBarraInventario : MonoBehaviour
{
    public Image iconoItem;

    public ItemData itemActual;

    public bool EstaVacio()
    {
        return itemActual == null;
    }

    public void ColocarItem(ItemData item)
    {
        itemActual = item;

        if (iconoItem == null) return;
        iconoItem.sprite = item.icono;
        iconoItem.color = new Color(1, 1, 1, 1);
    }

    public void VaciarSlot()
    {
        itemActual = null;

        if (iconoItem == null) return;
        iconoItem.sprite = null;
        iconoItem.color = new Color(1, 1, 1, 0);
    }
}