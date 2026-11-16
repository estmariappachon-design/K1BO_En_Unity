using UnityEngine;
using UnityEngine.UI;

public class BotonOnOffUI : MonoBehaviour
{
    public Image imagenBoton;
    public Sprite spriteOn;
    public Sprite spriteOff;
    private bool estaOn = true;

    public MusicaManager musicaManager; // referencia al manager

    public void CambiarEstado()
    {
        estaOn = !estaOn;

        if (estaOn)
        {
            imagenBoton.sprite = spriteOn;
            musicaManager.ReanudarMusica(); // suena
        }
        else
        {
            imagenBoton.sprite = spriteOff;
            musicaManager.PausarMusica(); // ara
        }
    }
}