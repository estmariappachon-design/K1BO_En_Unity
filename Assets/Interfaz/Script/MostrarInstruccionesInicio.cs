using UnityEngine;

public class MostrarInstruccionesInicio : MonoBehaviour
{
    public GameObject panelInstrucciones;

    void Start()
    {
        Invoke(nameof(MostrarPanel), 0.2f);
    }

    void MostrarPanel()
    {
        if (panelInstrucciones != null)
        {
            panelInstrucciones.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No está asignado el Panel_Instrucciones");
        }
    }
}