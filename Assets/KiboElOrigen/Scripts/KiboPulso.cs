using UnityEngine;

namespace KiboElOrigen
{
    // Hace latir suavemente un objeto de la interfaz (funciona con el juego en pausa)
    public class KiboPulso : MonoBehaviour
    {
        public float velocidad = 3f;
        public float cantidad = 0.06f;
        Vector3 baseEscala;

        void Awake() { baseEscala = transform.localScale; }

        void Update()
        {
            transform.localScale = baseEscala * (1f + Mathf.Sin(Time.unscaledTime * velocidad) * cantidad);
        }
    }
}
