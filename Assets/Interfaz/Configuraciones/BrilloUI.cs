using UnityEngine;
using UnityEngine.UI;

public class BrilloUI : MonoBehaviour
{
    public Slider sliderBrillo;
    public Image overlayBrillo;

    public float oscuridadMaxima = 0.7f;

    void Start()
    {
        sliderBrillo.value = PlayerPrefs.GetFloat("Brillo", 1f);
        CambiarBrillo(sliderBrillo.value);

        sliderBrillo.onValueChanged.AddListener(CambiarBrillo);
    }

    public void CambiarBrillo(float valor)
    {
        float alpha = (1f - valor) * oscuridadMaxima;

        Color color = overlayBrillo.color;
        color.a = alpha;
        overlayBrillo.color = color;

        PlayerPrefs.SetFloat("Brillo", valor);
    }
}