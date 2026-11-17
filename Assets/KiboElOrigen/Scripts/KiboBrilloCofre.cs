using System.Collections.Generic;
using UnityEngine;

namespace KiboElOrigen
{
    // Los cofres cerrados brillan suavemente; al pasar el mouse brillan más y crecen un poquito.
    public class KiboBrilloCofre : MonoBehaviour
    {
        public Color colorBrillo = new Color(1f, 0.82f, 0.3f);
        [Range(0f, 1f)] public float brilloNormal = 0.22f;
        [Range(0f, 1f)] public float brilloEncima = 0.6f;
        public float escalaEncima = 1.06f;

        readonly List<Renderer> rends = new List<Renderer>();
        MaterialPropertyBlock bloque;
        CofreClick cofre;
        Light luz;
        Vector3 escala0;
        bool encima;
        float actual;

        void Start()
        {
            cofre = GetComponent<CofreClick>();
            escala0 = transform.localScale;
            bloque = new MaterialPropertyBlock();
            Bounds b = new Bounds(transform.position, Vector3.zero);
            bool hay = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<ItemRecolectable>() != null) continue; // el residuo de adentro no
                rends.Add(r);
                if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
            }

            // pequeña luz dorada encima del cofre
            var go = new GameObject("KIBO_BrilloCofre");
            go.transform.SetParent(transform, true);
            go.transform.position = b.center + Vector3.up * (b.extents.y + 0.4f);
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = colorBrillo;
            luz.range = 3f;
            luz.intensity = 0f;
            luz.shadows = LightShadows.None;
        }

        void OnMouseEnter() { encima = true; }
        void OnMouseExit() { encima = false; }
        void OnDisable() { encima = false; }

        void Update()
        {
            bool abierto = cofre != null && cofre.EstaAbierto;
            bool hover = encima && !abierto && !KiboJuego.EntradaBloqueada && !KiboUtil.PunteroSobreUI();
            float objetivo = abierto ? 0f : (hover ? brilloEncima : brilloNormal * (0.65f + 0.35f * Mathf.Sin(Time.time * 2.5f)));
            actual = Mathf.MoveTowards(actual, objetivo, Time.deltaTime * 2.5f);

            foreach (var r in rends)
            {
                if (r == null) continue;
                var m = r.sharedMaterial;
                if (m == null) continue;
                r.GetPropertyBlock(bloque);
                if (m.HasProperty("_Color"))
                {
                    Color o = m.GetColor("_Color");
                    Color c = Color.Lerp(o, colorBrillo, actual * 0.7f);
                    c.a = o.a;
                    bloque.SetColor("_Color", c);
                }
                if (m.HasProperty("_EmissionColor"))
                    bloque.SetColor("_EmissionColor", colorBrillo * (actual * 0.8f));
                r.SetPropertyBlock(bloque);
            }

            if (luz != null)
            {
                luz.intensity = actual * 3f;
                luz.enabled = actual > 0.01f;
            }

            Vector3 obj = escala0 * (hover ? escalaEncima : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, obj, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }
}
