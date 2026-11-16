using UnityEngine;

public class HighlightInteractuable : MonoBehaviour
{
    private Renderer rend;
    private Color colorOriginal;

    [Header("Color de brillo")]
    public Color colorBrillo = Color.white;
    public float intensidad = 0.5f;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            rend.material.EnableKeyword("_EMISSION");
            colorOriginal = rend.material.GetColor("_EmissionColor");
        }
    }

    void OnMouseEnter()
    {
        if (rend != null)
        {
            rend.material.SetColor("_EmissionColor", colorBrillo * intensidad);
        }
    }

    void OnMouseExit()
    {
        if (rend != null)
        {
            rend.material.SetColor("_EmissionColor", colorOriginal);
        }
    }
}