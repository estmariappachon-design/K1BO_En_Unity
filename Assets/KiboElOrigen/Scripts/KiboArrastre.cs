using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Residuo que se arrastra desde la bandeja hasta una caneca en la ventana de clasificación
    public class KiboArrastre : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public ItemData item;
        public SlotBarraInventario slotBarra;      // si viene de la barra
        public SlotInventarioFull slotMochila;     // si viene de la maleta

        RectTransform rt;
        CanvasGroup grupo;
        Transform padreOriginal;
        int hermano;
        Vector2 posOriginal;
        bool arrastrando;
        bool bloqueado;

        void Awake()
        {
            rt = (RectTransform)transform;
            grupo = GetComponent<CanvasGroup>();
            if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnPointerEnter(PointerEventData e) { if (!arrastrando && !bloqueado) transform.localScale = Vector3.one * 1.1f; }
        public void OnPointerExit(PointerEventData e) { if (!arrastrando && !bloqueado) transform.localScale = Vector3.one; }

        public void OnBeginDrag(PointerEventData e)
        {
            if (bloqueado || KiboUI.Instancia == null) return;
            arrastrando = true;
            padreOriginal = transform.parent;
            hermano = transform.GetSiblingIndex();
            posOriginal = rt.anchoredPosition;
            // La capa de arrastre siempre encima de las ventanas, para que se vea el residuo
            KiboUI.Instancia.CapaArrastre.SetAsLastSibling();
            transform.SetParent(KiboUI.Instancia.CapaArrastre, true);
            transform.SetAsLastSibling();
            grupo.blocksRaycasts = false;
            transform.localScale = Vector3.one * 1.2f;
            KiboSonidos.Clic();
        }

        public void OnDrag(PointerEventData e)
        {
            if (!arrastrando) return;
            rt.position = e.position;
            if (KiboUI.Instancia != null) KiboUI.Instancia.ResaltarObjetivo(e.position);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!arrastrando) return;
            arrastrando = false;
            if (KiboUI.Instancia != null) KiboUI.Instancia.Soltar(this, e.position);
            else Volver();
        }

        // Regresa a su casilla
        public void Volver()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(RutinaVolver());
        }

        IEnumerator RutinaVolver()
        {
            bloqueado = true;
            if (padreOriginal != null)
            {
                transform.SetParent(padreOriginal, true);
                transform.SetSiblingIndex(hermano);
            }
            Vector2 fin = posOriginal;
            Vector2 inicio = rt.anchoredPosition;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / 0.25f, 3f);
                rt.anchoredPosition = Vector2.Lerp(inicio, fin, k);
                transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, k);
                yield return null;
            }
            rt.anchoredPosition = fin;
            transform.localScale = Vector3.one;
            grupo.blocksRaycasts = true;
            bloqueado = false;
        }

        // Entra en la caneca: vuela hacia ella y se encoge
        public void Entrar(Vector3 destino)
        {
            bloqueado = true;
            StartCoroutine(RutinaEntrar(destino));
        }

        IEnumerator RutinaEntrar(Vector3 destino)
        {
            Vector3 inicio = rt.position;
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.3f;
                rt.position = Vector3.Lerp(inicio, destino, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 60f;
                transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.1f, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
