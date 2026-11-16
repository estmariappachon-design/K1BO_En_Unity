using UnityEngine;

public class InventarioUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelInventario;
    public GameObject panelConfiguracion;
    public GameObject panelLibro;

    [Header("Barra Inventario")]
    public SlotBarraInventario[] slotsBarra;

    public void CerrarTodasLasVentanas()
    {
        if (panelInventario != null) panelInventario.SetActive(false);
        if (panelConfiguracion != null) panelConfiguracion.SetActive(false);
        if (panelLibro != null) panelLibro.SetActive(false);
    }

    public void AbrirInventario()
    {
        CerrarTodasLasVentanas();

        if (panelInventario != null)
            panelInventario.SetActive(true);
    }

    public void CerrarInventario()
    {
        if (panelInventario != null)
            panelInventario.SetActive(false);
    }

    public void AbrirConfiguracion()
    {
        CerrarTodasLasVentanas();

        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(true);
    }

    public void CerrarConfiguracion()
    {
        if (panelConfiguracion != null)
            panelConfiguracion.SetActive(false);
    }

    public void AbrirLibro()
    {
        CerrarTodasLasVentanas();

        if (panelLibro != null)
            panelLibro.SetActive(true);
    }

    public void CerrarLibro()
    {
        if (panelLibro != null)
            panelLibro.SetActive(false);
    }

    public bool TieneEspacio()
    {
        foreach (SlotBarraInventario slot in slotsBarra)
        {
            if (slot != null && slot.EstaVacio())
                return true;
        }

        return false;
    }

    public void AgregarItem(ItemData item)
    {
        foreach (SlotBarraInventario slot in slotsBarra)
        {
            if (slot != null && slot.EstaVacio())
            {
                slot.ColocarItem(item);
                return;
            }
        }

        Debug.Log("Barra llena");
    }

    public void VaciarBarraInventario()
    {
        foreach (SlotBarraInventario slot in slotsBarra)
        {
            if (slot != null)
            {
                slot.VaciarSlot();
            }
        }
    }
}