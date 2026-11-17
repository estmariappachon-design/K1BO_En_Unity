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
            if (imagenBoton != null) imagenBoton.sprite = spriteOn;
            if (musicaManager != null) musicaManager.ReanudarMusica(); // suena
        }
        else
        {
            if (imagenBoton != null) imagenBoton.sprite = spriteOff;
            if (musicaManager != null) musicaManager.PausarMusica(); // para
        }
    }
}