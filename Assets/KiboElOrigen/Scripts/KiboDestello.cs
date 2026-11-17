using UnityEngine;

namespace KiboElOrigen
{
    // Apaga poco a poco la luz del efecto de teletransporte
    public class KiboDestello : MonoBehaviour
    {
        public float duracion = 0.6f;
        Light luz;
        float inicial, t;

        void Awake()
        {
            luz = GetComponent<Light>();
            if (luz != null) inicial = luz.intensity;
        }

        void Update()
        {
            if (luz == null) return;
            t += Time.unscaledDeltaTime;
            luz.intensity = Mathf.Lerp(inicial, 0f, t / duracion);
            if (t >= duracion) luz.enabled = false;
        }
    }
}
