using UnityEngine;
using UnityEngine.UI;

public class Salir : MonoBehaviour
{
    [SerializeField] private Button btnSalir;

    public void SalirDelJuego()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Detiene el Play Mode en el editor
        #else
            Application.Quit(); // Cierra la aplicacion en la build
        #endif
    }
}