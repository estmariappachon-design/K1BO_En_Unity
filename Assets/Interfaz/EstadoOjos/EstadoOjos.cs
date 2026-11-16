using UnityEngine;
using UnityEngine.UI;

public class EstadoOjos : MonoBehaviour
{
    [Header("Referencia a la energía")]
    public PlayerAnimations energia;

    [Header("Imagen UI")]
    public Image imagenOjos;

    [Header("Sprites de ojos")]
    public Sprite ojosNormales;
    public Sprite ojosCansados;
    public Sprite ojosMuyCansados;

    void Update()
    {
        if (energia != null)
        {
            CambiarEstadoOjos();
        }
    }

    void CambiarEstadoOjos()
    {
        float porcentajeEnergia = energia.energiaActual / energia.energiaMaxima;

        if (porcentajeEnergia >= 0.66f)
        {
            imagenOjos.sprite = ojosNormales;
        }
        else if (porcentajeEnergia >= 0.33f)
        {
            imagenOjos.sprite = ojosCansados;
        }
        else
        {
            imagenOjos.sprite = ojosMuyCansados;
        }
    }
}