using UnityEngine;
using UnityEngine.SceneManagement;

public class AbrirMinijuego : MonoBehaviour
{
    public GameObject panelBlur;

    void OnMouseDown()
    {
        // Evita abrir múltiples veces el minijuego
        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
            return;

        // Oculta el cubo
        gameObject.SetActive(false);

        // Abre el minijuego
        IrAlRompecabezas();
    }

    public void IrAlRompecabezas()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EntrarMinijuego();
        }

        if (panelBlur != null)
        {
            panelBlur.SetActive(true);
        }

        SceneManager.LoadScene("Minijuego_Rompecabezas", LoadSceneMode.Additive);
    }
}