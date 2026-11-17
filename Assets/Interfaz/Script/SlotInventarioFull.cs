using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotInventarioFull : MonoBehaviour
{
    public Image iconoItem;
    public TMP_Text textoCantidad;

    private ItemData itemActual;
    private int cantidad = 0;

    public ItemData Item { get { return itemActual; } }
    public int Cantidad { get { return cantidad; } }

    // Saca un residuo de esta casilla (se usa al echarlo en una caneca)
    public void QuitarUno()
    {
        if (itemActual == null) return;
        cantidad--;
        if (cantidad <= 0)
        {
            VaciarSlot();
            return;
        }
        if (textoCantidad != null) textoCantidad.text = cantidad.ToString();
    }

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

        if (iconoItem != null)
        {
            iconoItem.sprite = item.icono;
            iconoItem.color = Color.white;
        }

        if (textoCantidad != null)
        {
            textoCantidad.text = cantidad.ToString();
            textoCantidad.gameObject.SetActive(true);
        }
    }

    public void VaciarSlot()
    {
        itemActual = null;
        cantidad = 0;

        if (iconoItem != null)
        {
            iconoItem.sprite = null;
            iconoItem.color = new Color(1, 1, 1, 0);
        }

        if (textoCantidad != null)
        {
            textoCantidad.text = "";
            textoCantidad.gameObject.SetActive(false);
        }
    }
}






