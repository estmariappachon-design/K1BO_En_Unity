using UnityEngine;

public class InstruccionesUI : MonoBehaviour
{
    public GameObject panelInstrucciones;

    void Start()
    {
        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);
    }

    public void AbrirInstrucciones()
    {
        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(true);
    }

    public void CerrarInstrucciones()
    {
        if (panelInstrucciones != null)
            panelInstrucciones.SetActive(false);
    }
}