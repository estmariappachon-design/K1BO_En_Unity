using UnityEngine;
using UnityEngine.UI;

public class FichaRompecabezas : MonoBehaviour
{
    public int numeroCorrecto;
    public int posicionActual;

    private Button boton;
    private RompecabezasManager manager;

    void Start()
    {
        boton = GetComponent<Button>();
        manager = FindAnyObjectByType<RompecabezasManager>();
        if (boton != null) { 
            boton.onClick.AddListener(() =>
            {
                manager.IntentarMoverFicha(this);
            });
    
        }
        
    }
}