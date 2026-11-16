using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotInventarioFull : MonoBehaviour
{
    public Image iconoItem;
    public TMP_Text textoCantidad;

    private ItemData itemActual;
    private int cantidad = 0;

    public bool EstaVacio()
    {
        return itemActual == null;
    }

    public bool TieneEsteItem(ItemData item)
    {
        return itemActual == item;
    }

    public void AgregarItem(ItemData item)
    {
        itemActual = item;
        cantidad++;

        iconoItem.sprite = item.icono;
        iconoItem.color = Color.white;

        textoCantidad.text = cantidad.ToString();
        textoCantidad.gameObject.SetActive(true);
    }

    public void VaciarSlot()
    {
        itemActual = null;
        cantidad = 0;

        iconoItem.sprite = null;
        iconoItem.color = new Color(1, 1, 1, 0);

        textoCantidad.text = "";
        textoCantidad.gameObject.SetActive(false);
    }
}






