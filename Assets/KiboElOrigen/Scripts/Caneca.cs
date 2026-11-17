using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Caneca de reciclaje. Muévela libremente en la escena con la herramienta Mover (W).
    // En el juego: haz clic sobre ella (o sobre el letrero "CLICK") para abrir la ventana de clasificación.
    [DisallowMultipleComponent]
    public class Caneca : MonoBehaviour
    {
        public const int VERSION_ACTUAL = 3;

        [Tooltip("Color de la caneca")]
        public TipoCaneca tipo = TipoCaneca.Blanca;

        [Tooltip("Altura de la boca de la caneca (metros)")]
        public float alto = 1f;

        [Tooltip("Mostrar el letrero flotante 'CLICK' encima de la caneca")]
        public bool mostrarLetrero = true;

        [HideInInspector] public int version;

        public static readonly List<Caneca> Todas = new List<Caneca>();

        Transform flotante;
        Vector3 flotanteBase;
        CanvasGroup grupoFlotante;
        Transform bisagra;
        Quaternion bisagraBase;
        Vector3 escalaBase;
        float fase;
        bool animando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Todas.Clear(); }

        void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
        void OnDisable() { Todas.Remove(this); }

        void Awake()
        {
            escalaBase = transform.localScale;
            fase = Random.value * 6f;

            // Asegura que se pueda hacer clic sobre la caneca
            if (GetComponentInChildren<Collider>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0, alto * 0.55f, 0);
                box.size = new Vector3(0.8f, alto * 1.1f, 0.8f);
            }

            flotante = transform.Find("Flotante");
            if (flotante == null) flotante = CrearFlotante(transform, alto);
            flotanteBase = flotante.localPosition;
            grupoFlotante = flotante.GetComponent<CanvasGroup>();
            if (grupoFlotante == null) grupoFlotante = flotante.gameObject.AddComponent<CanvasGroup>();

            bisagra = transform.Find("Bisagra");
            if (bisagra != null) bisagraBase = bisagra.localRotation;
        }

        void LateUpdate()
        {
            if (flotante == null) return;
            bool ver = mostrarLetrero && !KiboJuego.EntradaBloqueada;
            float alfa = Mathf.MoveTowards(grupoFlotante.alpha, ver ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            grupoFlotante.alpha = alfa;
            flotante.gameObject.SetActive(alfa > 0.01f || ver);

            // Sube y baja
            float t = Time.unscaledTime * 2.6f + fase;
            flotante.localPosition = flotanteBase + Vector3.up * (Mathf.Sin(t) * 0.09f / Mathf.Max(0.01f, transform.lossyScale.y));

            // Mira siempre a la cámara
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir = flotante.position - cam.transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.0001f) flotante.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        // Efecto cuando entra un residuo correcto: abre la tapa y rebota
        public void Celebrar()
        {
            if (!isActiveAndEnabled || animando) return;
            StartCoroutine(RutinaCelebrar());
        }

        // Efecto cuando el residuo no era de esta caneca
        public void Sacudir()
        {
            if (!isActiveAndEnabled || animando) return;
            StartCoroutine(RutinaSacudir());
        }

        IEnumerator RutinaCelebrar()
        {
            animando = true;
            float d = 0.9f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                float k = t / d;
                float abre = Mathf.Sin(k * Mathf.PI);
                if (bisagra != null) bisagra.localRotation = bisagraBase * Quaternion.Euler(abre * 85f, 0, 0);
                float s = 1f + Mathf.Sin(k * Mathf.PI * 3f) * 0.06f * (1f - k);
                transform.localScale = new Vector3(escalaBase.x * (2f - s), escalaBase.y * s, escalaBase.z * (2f - s));
                yield return null;
            }
            if (bisagra != null) bisagra.localRotation = bisagraBase;
            transform.localScale = escalaBase;
            animando = false;
        }

        IEnumerator RutinaSacudir()
        {
            animando = true;
            Quaternion r0 = transform.localRotation;
            float d = 0.5f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                float k = t / d;
                transform.localRotation = r0 * Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 6f) * 7f * (1f - k));
                yield return null;
            }
            transform.localRotation = r0;
            animando = false;
        }

        // Letrero flotante "CLICK" con flecha hacia abajo
        public static Transform CrearFlotante(Transform padre, float alto)
        {
            var go = new GameObject("Flotante", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var esc = go.AddComponent<CanvasScaler>();
            esc.dynamicPixelsPerUnit = 10f;
            go.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(320, 300);
            rt.localScale = Vector3.one * 0.0032f;
            rt.localPosition = new Vector3(0, alto + 0.75f, 0);

            // Zona invisible para que también se pueda hacer clic sobre el letrero
            var zona = go.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.size = new Vector3(320, 300, 30);

            var tabla = KiboEstilo.Rect("Tabla", go.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(300, 120));
            KiboEstilo.Imagen(tabla, "boton", Color.white, true, 1f);
            var tx = KiboEstilo.Lleno("Texto", tabla);
            tx.offsetMin = new Vector2(10, 8);
            tx.offsetMax = new Vector2(-10, -4);
            KiboEstilo.Texto(tx, "CLICK", 76, Color.white, TextAnchor.MiddleCenter);

            var flecha = KiboEstilo.Rect("Flecha", go.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(150, 170));
            KiboEstilo.Imagen(flecha, "flecha", Color.white);
            return go.transform;
        }
    }
}
