using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Construye el modelo 3D de una caneca (lo usan el menú "Crear Todo" y el juego si no hay canecas)
    public static class KiboCanecaModelo
    {
        public const float R_ABAJO = 0.30f, R_ARRIBA = 0.36f, ALTO = 1.0f, GROSOR = 0.025f;
        public const float TAPA_REPOSO = 14f;

        public class Materiales
        {
            public Material cuerpo, tapa, detalle, metal;
        }

        public static Materiales MaterialesRuntime(TipoCaneca tipo)
        {
            Color c = InfoCaneca.ColorModelo(tipo);
            return new Materiales
            {
                cuerpo = MaterialBasico(c, 0.45f),
                tapa = MaterialBasico(c * 0.82f + new Color(0, 0, 0, 1) * 0.18f, 0.4f),
                detalle = MaterialBasico(new Color(0.15f, 0.15f, 0.16f), 0.25f),
                metal = MaterialBasico(new Color(0.62f, 0.64f, 0.66f), 0.7f, 0.6f)
            };
        }

        public static Material MaterialBasico(Color c, float suavidad, float metalico = 0f)
        {
            var sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Diffuse");
            var m = new Material(sh);
            c.a = 1f;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
            return m;
        }

        public static GameObject Construir(TipoCaneca tipo, Mesh cuerpo, Mesh aro, Materiales mats)
        {
            string col = InfoCaneca.Titulo(tipo);
            var root = new GameObject("Caneca " + col);
            var can = root.AddComponent<Caneca>();
            can.tipo = tipo;
            can.alto = ALTO;
            can.version = Caneca.VERSION_ACTUAL;

            // Colisión para chocar y para hacer clic
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, ALTO * 0.55f, 0.02f);
            box.size = new Vector3(R_ARRIBA * 2f + 0.06f, ALTO * 1.12f, R_ARRIBA * 2f + 0.14f);

            var goC = new GameObject("Cuerpo");
            goC.transform.SetParent(root.transform, false);
            goC.AddComponent<MeshFilter>().sharedMesh = cuerpo;
            goC.AddComponent<MeshRenderer>().sharedMaterial = mats.cuerpo;

            var goA = new GameObject("Borde");
            goA.transform.SetParent(root.transform, false);
            goA.transform.localPosition = new Vector3(0, ALTO, 0);
            goA.AddComponent<MeshFilter>().sharedMesh = aro;
            goA.AddComponent<MeshRenderer>().sharedMaterial = mats.tapa;

            // Franjas decorativas
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? 0.18f : 0.84f;
                float r = Mathf.Lerp(R_ABAJO, R_ARRIBA, y / ALTO) + 0.006f;
                var f = Primitiva(PrimitiveType.Cylinder, "Franja", root.transform, mats.tapa);
                f.localPosition = new Vector3(0, y, 0);
                f.localScale = new Vector3(r * 2f, 0.018f, r * 2f);
            }

            // Tapa con bisagra atrás (+Z). Rotación positiva en X levanta el frente.
            var bisagra = new GameObject("Bisagra");
            bisagra.transform.SetParent(root.transform, false);
            bisagra.transform.localPosition = new Vector3(0, ALTO + 0.012f, R_ARRIBA + 0.01f);
            bisagra.transform.localRotation = Quaternion.Euler(TAPA_REPOSO, 0, 0);
            var tapa = Primitiva(PrimitiveType.Cylinder, "Tapa", bisagra.transform, mats.tapa);
            tapa.localPosition = new Vector3(0, 0.02f, -R_ARRIBA - 0.01f);
            tapa.localScale = new Vector3(R_ARRIBA * 2f + 0.06f, 0.02f, R_ARRIBA * 2f + 0.06f);
            var cupula = Primitiva(PrimitiveType.Sphere, "Cupula", bisagra.transform, mats.tapa);
            cupula.localPosition = new Vector3(0, 0.035f, -R_ARRIBA - 0.01f);
            cupula.localScale = new Vector3(R_ARRIBA * 1.9f, 0.08f, R_ARRIBA * 1.9f);
            var agarre = Primitiva(PrimitiveType.Cube, "Agarre", bisagra.transform, mats.detalle);
            agarre.localPosition = new Vector3(0, 0.03f, -R_ARRIBA * 2f - 0.05f);
            agarre.localScale = new Vector3(0.16f, 0.035f, 0.05f);

            // Asa trasera
            var asa = Primitiva(PrimitiveType.Cube, "Asa", root.transform, mats.detalle);
            asa.localPosition = new Vector3(0, ALTO - 0.1f, R_ARRIBA + 0.05f);
            asa.localScale = new Vector3(0.36f, 0.04f, 0.06f);

            // Ruedas y eje
            for (int i = -1; i <= 1; i += 2)
            {
                var rueda = Primitiva(PrimitiveType.Cylinder, "Rueda", root.transform, mats.detalle);
                rueda.localPosition = new Vector3(i * 0.23f, 0.075f, R_ABAJO + 0.03f);
                rueda.localRotation = Quaternion.Euler(0, 0, 90f);
                rueda.localScale = new Vector3(0.15f, 0.03f, 0.15f);
                var tapon = Primitiva(PrimitiveType.Cylinder, "Tapon", rueda, mats.metal);
                tapon.localPosition = new Vector3(0, i * 0.6f, 0);
                tapon.localScale = new Vector3(0.45f, 0.4f, 0.45f);
            }
            var eje = Primitiva(PrimitiveType.Cube, "Eje", root.transform, mats.metal);
            eje.localPosition = new Vector3(0, 0.075f, R_ABAJO + 0.03f);
            eje.localScale = new Vector3(0.44f, 0.03f, 0.03f);

            // Pedal al frente
            var pedal = Primitiva(PrimitiveType.Cube, "Pedal", root.transform, mats.detalle);
            pedal.localPosition = new Vector3(0, 0.05f, -R_ABAJO - 0.06f);
            pedal.localScale = new Vector3(0.2f, 0.035f, 0.12f);

            CrearEtiqueta(root.transform, tipo);
            Caneca.CrearFlotante(root.transform, ALTO);
            return root;
        }

        static Transform Primitiva(PrimitiveType tipo, string nombre, Transform padre, Material m)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.transform.SetParent(padre, false);
            return go.transform;
        }

        // Placa frontal con el ícono y la categoría
        static void CrearEtiqueta(Transform padre, TipoCaneca tipo)
        {
            float y = 0.5f;
            float r = Mathf.Lerp(R_ABAJO, R_ARRIBA, y / ALTO) + 0.012f;
            float inclinacion = Mathf.Atan((R_ARRIBA - R_ABAJO) / ALTO) * Mathf.Rad2Deg;

            var go = new GameObject("Etiqueta", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var esc = go.AddComponent<CanvasScaler>();
            esc.dynamicPixelsPerUnit = 10f;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(400, 330);
            rt.localScale = Vector3.one * 0.001f;
            rt.localPosition = new Vector3(0, y, -r);
            rt.localRotation = Quaternion.Euler(-inclinacion, 0, 0);

            var placa = KiboEstilo.Lleno("Placa", go.transform);
            var img = KiboEstilo.Imagen(placa, "redondo", new Color(1f, 0.98f, 0.94f), true, 0.5f);
            var borde = placa.gameObject.AddComponent<Outline>();
            borde.effectColor = KiboEstilo.Cafe;
            borde.effectDistance = new Vector2(6, -6);
            img.raycastTarget = false;

            var ico = KiboEstilo.Rect("Icono", go.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(170, 170));
            KiboEstilo.Imagen(ico, InfoCaneca.Icono(tipo), Color.white);

            var tx = KiboEstilo.Rect("Categoria", go.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(-30, 110));
            var t = KiboEstilo.Texto(tx, InfoCaneca.Categoria(tipo), 54, KiboEstilo.Cafe, TextAnchor.MiddleCenter, true, false);
            KiboEstilo.AutoTam(t, 20, 54);
        }

        // ------------------------------------------------------------------
        //  Mallas
        // ------------------------------------------------------------------
        class Constructor
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector3> n = new List<Vector3>();
            public List<Vector2> uv = new List<Vector2>();
            public List<int> t = new List<int>();

            public void Tri(int a, int b, int c, Vector3 normal)
            {
                Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (Vector3.Dot(cr, normal) >= 0) { t.Add(a); t.Add(b); t.Add(c); }
                else { t.Add(a); t.Add(c); t.Add(b); }
            }

            public Mesh Crear(string nombre)
            {
                var m = new Mesh { name = nombre };
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        public static Mesh MallaCuerpo()
        {
            const int seg = 48;
            var c = new Constructor();
            Pared(c, R_ABAJO, 0f, R_ARRIBA, ALTO, true, seg);
            Pared(c, R_ABAJO - GROSOR, GROSOR, R_ARRIBA - GROSOR, ALTO, false, seg);
            Anillo(c, R_ARRIBA - GROSOR, R_ARRIBA, ALTO, seg);
            Disco(c, R_ABAJO, 0f, false, seg);
            Disco(c, R_ABAJO - GROSOR, GROSOR, true, seg);
            return c.Crear("Cuerpo_Caneca");
        }

        public static Mesh MallaBorde()
        {
            return MallaToro(R_ARRIBA - GROSOR * 0.5f, GROSOR * 1.1f, 48, 10);
        }

        static void Pared(Constructor c, float r0, float y0, float r1, float y1, bool haciaFuera, int seg)
        {
            int b = c.v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                Vector3 dir = new Vector3(cs, 0, sn);
                Vector3 nn = new Vector3(cs * (y1 - y0), r0 - r1, sn * (y1 - y0)).normalized;
                if (!haciaFuera) nn = -nn;
                c.v.Add(dir * r0 + Vector3.up * y0); c.n.Add(nn); c.uv.Add(new Vector2(i / (float)seg, 0));
                c.v.Add(dir * r1 + Vector3.up * y1); c.n.Add(nn); c.uv.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int i0 = b + i * 2, i1 = i0 + 1, i2 = i0 + 2, i3 = i0 + 3;
                Vector3 nn = c.n[i0] + c.n[i2];
                c.Tri(i0, i1, i2, nn);
                c.Tri(i2, i1, i3, nn);
            }
        }

        static void Anillo(Constructor c, float rIn, float rOut, float y, int seg)
        {
            int b = c.v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                c.v.Add(dir * rIn + Vector3.up * y); c.n.Add(Vector3.up); c.uv.Add(new Vector2(i / (float)seg, 0));
                c.v.Add(dir * rOut + Vector3.up * y); c.n.Add(Vector3.up); c.uv.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int i0 = b + i * 2;
                c.Tri(i0, i0 + 1, i0 + 2, Vector3.up);
                c.Tri(i0 + 2, i0 + 1, i0 + 3, Vector3.up);
            }
        }

        static void Disco(Constructor c, float r, float y, bool arriba, int seg)
        {
            Vector3 nn = arriba ? Vector3.up : Vector3.down;
            int centro = c.v.Count;
            c.v.Add(Vector3.up * y); c.n.Add(nn); c.uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cs = Mathf.Cos(a), sn = Mathf.Sin(a);
                c.v.Add(new Vector3(cs * r, y, sn * r)); c.n.Add(nn); c.uv.Add(new Vector2(cs * 0.5f + 0.5f, sn * 0.5f + 0.5f));
            }
            for (int i = 0; i < seg; i++) c.Tri(centro, centro + 1 + i, centro + 2 + i, nn);
        }

        static Mesh MallaToro(float R, float r, int segMayor, int segMenor)
        {
            var c = new Constructor();
            for (int i = 0; i <= segMayor; i++)
            {
                float a = i / (float)segMayor * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 centro = dir * R;
                for (int j = 0; j <= segMenor; j++)
                {
                    float b = j / (float)segMenor * Mathf.PI * 2f;
                    Vector3 nn = dir * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                    c.v.Add(centro + nn * r); c.n.Add(nn); c.uv.Add(new Vector2(i / (float)segMayor, j / (float)segMenor));
                }
            }
            int fila = segMenor + 1;
            for (int i = 0; i < segMayor; i++)
                for (int j = 0; j < segMenor; j++)
                {
                    int a0 = i * fila + j, a1 = a0 + 1, b0 = a0 + fila, b1 = b0 + 1;
                    c.Tri(a0, b0, a1, c.n[a0]);
                    c.Tri(a1, b0, b1, c.n[a1]);
                }
            return c.Crear("Borde_Caneca");
        }
    }
}
