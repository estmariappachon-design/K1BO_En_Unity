using UnityEngine;

namespace KiboElOrigen
{
    // Luz de poste: se prende sola cuando oscurece
    public class KiboLuzNoche : MonoBehaviour
    {
        public float intensidadMaxima = 2.4f;
        [Tooltip("Qué tan oscuro debe estar para prenderse (0 = día, 1 = noche)")]
        [Range(0f, 1f)] public float umbral = 0.45f;
        public Color colorLuz = new Color(1f, 0.82f, 0.52f);

        Light luz;
        Renderer bombillo;
        MaterialPropertyBlock bloque;
        float actual;

        void Awake()
        {
            luz = GetComponent<Light>();
            bombillo = GetComponentInChildren<Renderer>();
            bloque = new MaterialPropertyBlock();
            if (luz != null) luz.color = colorLuz;
        }

        void Update()
        {
            float objetivo = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(umbral - 0.1f, umbral + 0.15f, KiboCielo.FactorNoche));
            actual = Mathf.MoveTowards(actual, objetivo, Time.deltaTime * 0.8f);
            if (luz != null)
            {
                luz.intensity = intensidadMaxima * actual;
                luz.enabled = actual > 0.01f;
            }
            if (bombillo != null)
            {
                bombillo.GetPropertyBlock(bloque);
                bloque.SetColor("_EmissionColor", colorLuz * (0.15f + 3f * actual));
                bloque.SetColor("_Color", Color.Lerp(new Color(0.75f, 0.72f, 0.65f), colorLuz, actual));
                bombillo.SetPropertyBlock(bloque);
            }
        }
    }
}
