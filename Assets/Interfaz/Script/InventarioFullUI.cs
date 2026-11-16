using UnityEngine;

public class InventarioFullUI : MonoBehaviour
{
    public SlotInventarioFull[] slotsFull;

    public void AgregarItem(ItemData item)
    {
        // Primero busca si ya existe ese item
        foreach (SlotInventarioFull slot in slotsFull)
        {
            if (slot.TieneEsteItem(item))
            {
                slot.AgregarItem(item);
                return;
            }
        }

        // Si no existe, busca un slot vacío
        foreach (SlotInventarioFull slot in slotsFull)
        {
            if (slot.EstaVacio())
            {
                slot.AgregarItem(item);
                return;
            }
        }

        Debug.Log("Inventario Full lleno");
    }

    public void VaciarInventarioFull()
    {
        foreach (SlotInventarioFull slot in slotsFull)
        {
            if (slot != null)
            {
                slot.VaciarSlot();
            }
        }
    }

}
