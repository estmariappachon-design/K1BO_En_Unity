using UnityEngine;

namespace KiboElOrigen
{
    // Cielo con nubes de día y estrellas de noche. Sigue al sol del DayNightCycle.
    public class KiboCielo : MonoBehaviour
    {
        // 0 = día, 1 = noche (lo usan las luces de los postes)
        public static float FactorNoche;

        [Tooltip("Material del cielo (shader Kibo/Cielo)")]
        public Material materialCielo;
        [Tooltip("Luz del sol (si está vacío se busca sola)")]
        public Light sol;
        [Range(0f, 1f)] public float cantidadNubes = 0.55f;
        public float velocidadNubes = 0.012f;
        [Range(0f, 3f)] public float brilloEstrellas = 1.3f;
        [Tooltip("Luces calculadas a la vez (más = postes mejor iluminados)")]
        public int lucesPorPixel = 8;

        Material instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { FactorNoche = 0f; }

        void Start()
        {
            if (sol == null)
            {
                var ciclo = Object.FindAnyObjectByType<DayNightCycle>();
                if (ciclo != null) sol = ciclo.sun;
            }
            if (sol == null) sol = RenderSettings.sun;
            if (sol == null)
                foreach (var l in KiboUtil.Buscar<Light>())
                    if (l.type == LightType.Directional) { sol = l; break; }

            var m = materialCielo;
            if (m == null)
            {
                var sh = Shader.Find("Kibo/Cielo");
                if (sh != null) m = new Material(sh);
            }
            if (m != null)
            {
                instancia = new Material(m);
                RenderSettings.skybox = instancia;
            }
            if (QualitySettings.pixelLightCount < lucesPorPixel) QualitySettings.pixelLightCount = lucesPorPixel;
            Actualizar();
        }

        void Update() { Actualizar(); }

        void Actualizar()
        {
            float n = 0f;
            Vector3 haciaSol = new Vector3(0, 0.6f, 0.8f);
            if (sol != null)
            {
                haciaSol = -sol.transform.forward;
                // Altura del sol: arriba = día, debajo del horizonte = noche
                float t = Mathf.Clamp01((haciaSol.y + 0.15f) / 0.5f);
                n = 1f - t * t * (3f - 2f * t);
            }
            FactorNoche = n;
            if (instancia != null)
            {
                instancia.SetFloat("_Noche", n);
                instancia.SetVector("_SolDir", haciaSol);
                instancia.SetFloat("_Nubes", cantidadNubes);
                instancia.SetFloat("_VelNubes", velocidadNubes);
                instancia.SetFloat("_Estrellas", brilloEstrellas);
            }
        }

        void OnDestroy()
        {
            if (instancia != null) Destroy(instancia);
        }
    }
}
