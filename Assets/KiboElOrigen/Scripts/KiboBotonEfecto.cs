using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Pequeña animación al pasar el mouse y al hacer clic, con sonido
    public class KiboBotonEfecto : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public float escalaEncima = 1.07f;
        Vector3 baseEscala = Vector3.one;
        float objetivo = 1f;
        bool listo;

        void OnEnable()
        {
            if (!listo) { baseEscala = transform.localScale; listo = true; }
            objetivo = 1f;
            transform.localScale = baseEscala;
        }

        void Update()
        {
            float actual = transform.localScale.x / Mathf.Max(0.0001f, baseEscala.x);
            float n = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            transform.localScale = baseEscala * n;
        }

        bool Activo()
        {
            var s = GetComponent<Selectable>();
            return s == null || s.IsInteractable();
        }

        public void OnPointerEnter(PointerEventData e) { if (Activo()) objetivo = escalaEncima; }
        public void OnPointerExit(PointerEventData e) { objetivo = 1f; }
        public void OnPointerDown(PointerEventData e) { if (Activo()) objetivo = 0.94f; }
        public void OnPointerUp(PointerEventData e) { objetivo = 1f; }
        public void OnPointerClick(PointerEventData e) { if (Activo()) KiboSonidos.Clic(); }
    }
}
