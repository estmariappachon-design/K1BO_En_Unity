using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace KiboElOrigen.Herramientas
{
    // Menú superior de Unity:  Kibo el Origen > Crear Todo
    public static class KiboCreador
    {
        const string RAIZ = "Assets/KiboElOrigen";
        const string MAT = RAIZ + "/Materiales";
        const string MOD = RAIZ + "/Modelos";
        const string PRE = RAIZ + "/Prefabs";

        // Medidas de la caneca (metros)
        const float R_ABAJO = 0.30f, R_ARRIBA = 0.36f, ALTO = 1.0f, GROSOR = 0.025f;

        static Font fuente;
        static Sprite spriteRedondo;

        // =====================================================================
        //  CREAR TODO
        // =====================================================================
        [MenuItem("Kibo el Origen/Crear Todo", false, 0)]
        public static void CrearTodo()
        {
            var escena = EditorSceneManager.GetActiveScene();
            if (!escena.IsValid())
            {
                EditorUtility.DisplayDialog("Kibo el Origen", "Abre primero la escena de tu juego.", "OK");
                return;
            }

            CrearCarpetas();
            CargarRecursos();
            Physics.SyncTransforms();

            Undo.IncrementCurrentGroup();
            int grupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Kibo: Crear Todo");

            var informe = new StringBuilder();
            Transform jugador = BuscarJugador();
            Vector3 basePos, adelante;
            Referencia(jugador, out basePos, out adelante);
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;

            // ---- 1. Canecas ----
            if (BuscarTodos<Caneca>().Length > 0)
            {
                informe.AppendLine("• Las canecas ya existían (no se duplicaron).");
            }
            else
            {
                var padre = new GameObject("KIBO_Canecas");
                Undo.RegisterCreatedObjectUndo(padre, "Canecas");
                Mesh cuerpo = MallaGuardada("Cuerpo_Caneca", () => MallaCuerpo(R_ABAJO, R_ARRIBA, ALTO, GROSOR, 40));
                Mesh aro = MallaGuardada("Borde_Caneca", () => MallaToro(R_ARRIBA - GROSOR * 0.5f, GROSOR * 0.9f, 40, 10));

                Vector3 centro = basePos + adelante * 4f;
                TipoCaneca[] tipos = { TipoCaneca.Blanca, TipoCaneca.Negra, TipoCaneca.Verde };
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = Suelo(centro + derecha * ((i - 1) * 1.3f), jugador);
                    GameObject c = ConstruirCaneca(tipos[i], cuerpo, aro);
                    c.transform.SetParent(padre.transform, false);
                    c.transform.position = p;
                    c.transform.rotation = Quaternion.LookRotation(adelante); // el frente mira al jugador
                    string ruta = PRE + "/Caneca_" + InfoCaneca.Titulo(tipos[i]) + ".prefab";
                    PrefabUtility.SaveAsPrefabAssetAndConnect(c, ruta, InteractionMode.AutomatedAction);
                }
                informe.AppendLine("• 3 canecas creadas (Blanca, Negra y Verde). Muévelas a donde quieras.");
                informe.AppendLine("  También quedaron como prefabs en " + PRE);
            }

            // ---- 2. Residuos ----
            var sinTipo = new List<string>();
            int detectados = DetectarResiduos(sinTipo);
            if (detectados > 0)
                informe.AppendLine("• " + detectados + " residuos de tu escena se configuraron automáticamente por su nombre.");
            if (sinTipo.Count > 0)
            {
                informe.AppendLine("• No supe en qué caneca van: " + string.Join(", ", sinTipo.ToArray()) +
                                   ". Selecciónalos y usa  Kibo el Origen > Marcar selección como residuo.");
            }
            if (BuscarTodos<Residuo>().Length == 0)
            {
                CrearResiduosEjemplo(basePos, adelante, jugador);
                informe.AppendLine("• No encontré residuos, así que creé 7 de ejemplo alrededor del jugador.");
            }

            // ---- 3. Sistema de juego ----
            var juego = Buscar<KiboJuego>();
            if (juego == null)
            {
                var go = new GameObject("KIBO_Sistema");
                Undo.RegisterCreatedObjectUndo(go, "Sistema");
                juego = go.AddComponent<KiboJuego>();
            }
            Undo.RecordObject(juego, "Jugador");
            if (jugador && jugador.CompareTag("Player")) juego.jugador = jugador;
            EditorUtility.SetDirty(juego);

            // ---- 4. Interfaz (se reconstruye siempre) ----
            foreach (var viejo in BuscarTodos<KiboUI>()) Undo.DestroyObjectImmediate(viejo.gameObject);
            VideoClip clip = BuscarVideo();
            CrearInterfaz(clip);
            informe.AppendLine(clip != null
                ? "• Interfaz creada con tu video: " + clip.name
                : "• Interfaz creada. NO encontré ningún video en el proyecto: el juego irá directo al menú.\n  Arrastra tu video a Assets y vuelve a usar Crear Todo.");

            // ---- 5. EventSystem ----
            PrepararEventSystem();

            if (jugador == null)
                informe.AppendLine("\n⚠ No encontré jugador (tag \"Player\") ni cámara principal. Ponle el tag Player a tu personaje.");

            EditorSceneManager.MarkSceneDirty(escena);
            Undo.CollapseUndoOperations(grupo);
            AssetDatabase.SaveAssets();

            informe.AppendLine("\nControles: E = recoger / echar en la caneca,  Q = soltar.");
            informe.AppendLine("Guarda la escena (Ctrl+S) y dale Play.");
            EditorUtility.DisplayDialog("Kibo el Origen  ✔", informe.ToString(), "¡Listo!");
        }

        // =====================================================================
        //  MARCAR RESIDUOS A MANO
        // =====================================================================
        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Blanca (aprovechables)", false, 20)]
        static void MarcarBlanca() { Marcar(TipoCaneca.Blanca); }

        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Negra (no aprovechables)", false, 21)]
        static void MarcarNegra() { Marcar(TipoCaneca.Negra); }

        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Verde (orgánicos)", false, 22)]
        static void MarcarVerde() { Marcar(TipoCaneca.Verde); }

        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Blanca (aprovechables)", true)]
        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Negra (no aprovechables)", true)]
        [MenuItem("Kibo el Origen/Marcar selección como residuo/Caneca Verde (orgánicos)", true)]
        static bool HaySeleccion() { return Selection.gameObjects.Length > 0; }

        static void Marcar(TipoCaneca tipo)
        {
            foreach (var go in Selection.gameObjects)
            {
                var r = go.GetComponent<Residuo>();
                if (r == null) { r = Undo.AddComponent<Residuo>(go); r.nombre = NombreBonito(go.name); }
                Undo.RecordObject(r, "Tipo residuo");
                r.tipo = tipo;
                if (go.GetComponentInChildren<Collider>() == null) Undo.AddComponent<BoxCollider>(go);
                EditorUtility.SetDirty(r);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Kibo el Origen/Ayuda", false, 40)]
        static void Ayuda()
        {
            EditorUtility.DisplayDialog("Kibo el Origen",
                "1. Pon tu video de inicio en cualquier carpeta de Assets.\n" +
                "2. Ponle el tag \"Player\" a tu personaje.\n" +
                "3. Menú Kibo el Origen > Crear Todo.\n" +
                "4. Mueve las canecas con la herramienta Mover (W).\n" +
                "5. Si un residuo no se detectó, selecciónalo y usa Marcar selección como residuo.\n\n" +
                "Caneca BLANCA: aprovechables (plástico, vidrio, metal, papel, cartón)\n" +
                "Caneca NEGRA: no aprovechables (servilletas, papel higiénico, sucios)\n" +
                "Caneca VERDE: orgánicos (restos de comida, cáscaras)", "OK");
        }

        // =====================================================================
        //  MODELO 3D DE LA CANECA
        // =====================================================================
        static GameObject ConstruirCaneca(TipoCaneca tipo, Mesh cuerpo, Mesh aro)
        {
            string col = InfoCaneca.Titulo(tipo);
            Color c = InfoCaneca.ColorModelo(tipo);
            Material mCuerpo = Mat("Caneca_" + col, c, 0.55f);
            Material mTapa = Mat("Caneca_Tapa_" + col, new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f, 1f), 0.5f);
            Material mOscuro = Mat("Caneca_Detalle", new Color(0.16f, 0.16f, 0.17f), 0.3f);

            var root = new GameObject("Caneca " + col);
            var can = root.AddComponent<Caneca>();
            can.tipo = tipo;
            can.alto = ALTO;

            // Zona donde caen los residuos
            var trig = root.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0, ALTO * 0.7f, 0);
            trig.size = new Vector3(0.55f, 0.5f, 0.55f);

            // Cuerpo (malla hueca y abierta arriba)
            var goC = new GameObject("Cuerpo");
            goC.transform.SetParent(root.transform, false);
            goC.AddComponent<MeshFilter>().sharedMesh = cuerpo;
            goC.AddComponent<MeshRenderer>().sharedMaterial = mCuerpo;
            goC.AddComponent<MeshCollider>().sharedMesh = cuerpo;

            // Borde redondeado
            var goA = new GameObject("Borde");
            goA.transform.SetParent(root.transform, false);
            goA.transform.localPosition = new Vector3(0, ALTO, 0);
            goA.AddComponent<MeshFilter>().sharedMesh = aro;
            goA.AddComponent<MeshRenderer>().sharedMaterial = mTapa;

            // Tapa abierta con bisagra atrás
            var bisagra = new GameObject("Bisagra");
            bisagra.transform.SetParent(root.transform, false);
            bisagra.transform.localPosition = new Vector3(0, ALTO + 0.01f, R_ARRIBA);
            bisagra.transform.localRotation = Quaternion.Euler(110f, 0, 0);
            var tapa = Primitiva(PrimitiveType.Cylinder, "Tapa", bisagra.transform, mTapa, false);
            tapa.localPosition = new Vector3(0, 0.012f, -R_ARRIBA - 0.01f);
            tapa.localScale = new Vector3(R_ARRIBA * 2f + 0.05f, 0.012f, R_ARRIBA * 2f + 0.05f);
            var agarre = Primitiva(PrimitiveType.Cube, "Agarre", bisagra.transform, mOscuro, false);
            agarre.localPosition = new Vector3(0, 0.03f, -R_ARRIBA * 2f - 0.02f);
            agarre.localScale = new Vector3(0.14f, 0.03f, 0.03f);

            // Asa trasera
            var asa = Primitiva(PrimitiveType.Cube, "Asa", root.transform, mOscuro, false);
            asa.localPosition = new Vector3(0, ALTO - 0.12f, R_ARRIBA + 0.03f);
            asa.localScale = new Vector3(0.32f, 0.035f, 0.05f);

            // Ruedas
            for (int i = -1; i <= 1; i += 2)
            {
                var rueda = Primitiva(PrimitiveType.Cylinder, "Rueda", root.transform, mOscuro, false);
                rueda.localPosition = new Vector3(i * 0.2f, 0.065f, R_ABAJO + 0.02f);
                rueda.localRotation = Quaternion.Euler(0, 0, 90f);
                rueda.localScale = new Vector3(0.13f, 0.025f, 0.13f);
            }
            var eje = Primitiva(PrimitiveType.Cube, "Eje", root.transform, mOscuro, false);
            eje.localPosition = new Vector3(0, 0.065f, R_ABAJO + 0.02f);
            eje.localScale = new Vector3(0.38f, 0.03f, 0.03f);

            // Etiqueta frontal con la categoría
            CrearEtiqueta(root.transform, tipo);
            return root;
        }

        static void CrearEtiqueta(Transform padre, TipoCaneca tipo)
        {
            float y = 0.55f;
            float r = Mathf.Lerp(R_ABAJO, R_ARRIBA, y / ALTO) + 0.008f;
            float inclinacion = Mathf.Atan((R_ARRIBA - R_ABAJO) / ALTO) * Mathf.Rad2Deg;

            var go = new GameObject("Etiqueta", typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var esc = go.AddComponent<CanvasScaler>();
            esc.dynamicPixelsPerUnit = 10f;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 240);
            rt.localScale = Vector3.one * 0.001f; // 0.42 m x 0.24 m
            rt.localPosition = new Vector3(0, y, -r);
            rt.localRotation = Quaternion.Euler(-inclinacion, 0, 0);

            var fondo = Lleno("Fondo", go.transform);
            var img = Img(fondo, InfoCaneca.ColorEtiqueta(tipo));
            img.raycastTarget = false;

            Color ct = InfoCaneca.ColorTextoEtiqueta(tipo);
            var t1 = NuevoRect("Categoria", go.transform, new Vector2(0, 0.42f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30, -16));
            var tx1 = Txt(t1, InfoCaneca.Categoria(tipo), 52, ct, TextAnchor.MiddleCenter, FontStyle.Bold);
            tx1.resizeTextForBestFit = true; tx1.resizeTextMinSize = 18; tx1.resizeTextMaxSize = 56;

            var t2 = NuevoRect("Ejemplos", go.transform, new Vector2(0, 0), new Vector2(1, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30, -16));
            var tx2 = Txt(t2, InfoCaneca.Ejemplos(tipo), 24, ct, TextAnchor.MiddleCenter, FontStyle.Normal);
            tx2.resizeTextForBestFit = true; tx2.resizeTextMinSize = 12; tx2.resizeTextMaxSize = 26;
        }

        static Transform Primitiva(PrimitiveType tipo, string nombre, Transform padre, Material m, bool conCollider)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            if (!conCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            go.transform.SetParent(padre, false);
            return go.transform;
        }

        // ---------- Mallas procedurales ----------
        class Constructor
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector3> n = new List<Vector3>();
            public List<Vector2> uv = new List<Vector2>();
            public List<int> t = new List<int>();

            // Asegura que el triángulo mire hacia la normal deseada
            public void Tri(int a, int b, int c, Vector3 normal)
            {
                Vector3 cr = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (Vector3.Dot(cr, normal) >= 0) { t.Add(a); t.Add(b); t.Add(c); }
                else { t.Add(a); t.Add(c); t.Add(b); }
            }

            public Mesh Crear(string nombre)
            {
                var m = new Mesh { name = nombre };
                m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        static Mesh MallaCuerpo(float rb, float rt, float h, float g, int seg)
        {
            var c = new Constructor();
            Pared(c, rb, 0f, rt, h, true, seg);              // pared exterior
            Pared(c, rb - g, g, rt - g, h, false, seg);      // pared interior
            Anillo(c, rt - g, rt, h, seg);                   // borde superior
            Disco(c, rb, 0f, false, seg);                    // base exterior
            Disco(c, rb - g, g, true, seg);                  // fondo interior
            return c.Crear("Cuerpo_Caneca");
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

        static Mesh MallaGuardada(string nombre, System.Func<Mesh> crear)
        {
            string ruta = MOD + "/" + nombre + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (m != null) return m;
            m = crear();
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        // =====================================================================
        //  RESIDUOS
        // =====================================================================
        static readonly string[] clavesNegra = { "servilleta", "higienico", "panal", "colilla", "chicle", "icopor", "tapabocas", "sucio", "sucia", "contaminad", "napkin", "tissue", "diaper" };
        static readonly string[] clavesVerde = { "cascara", "comida", "fruta", "organic", "manzana", "banano", "banana", "platano", "verdura", "huevo", "sandia", "alimento", "apple", "food", "peel" };
        static readonly string[] clavesBlanca = { "botella", "plastic", "lata", "vidrio", "papel", "carton", "caja", "periodico", "revista", "envase", "tetrapak", "metal", "aluminio", "frasco", "bolsa", "bottle", "can", "paper", "cardboard", "glass" };
        static readonly string[] clavesGenericas = { "residuo", "basura", "desecho", "trash", "garbage", "waste" };

        static int DetectarResiduos(List<string> sinTipo)
        {
            int n = 0;
            var hechos = new HashSet<Transform>();
            foreach (var rend in BuscarTodos<Renderer>())
            {
                if (rend.bounds.size.magnitude > 2.5f) continue;            // objetos grandes no
                if (rend.GetComponentInParent<Residuo>() != null) continue;
                if (rend.GetComponentInParent<Caneca>() != null) continue;
                if (rend.GetComponentInParent<Canvas>() != null) continue;
                if (rend.transform.root.name.StartsWith("KIBO_")) continue;

                // Busca el objeto más alto (padre) cuyo nombre indique el tipo
                Transform elegido = null; TipoCaneca tipo = TipoCaneca.Blanca; bool generico = false;
                for (Transform t = rend.transform; t != null; t = t.parent)
                {
                    TipoCaneca tp;
                    if (Clasificar(t.name, out tp)) { elegido = t; tipo = tp; generico = false; }
                    else if (elegido == null && TieneClave(t.name, clavesGenericas)) { elegido = t; generico = true; }
                }
                if (elegido == null || hechos.Contains(elegido)) continue;
                hechos.Add(elegido);

                if (generico) { sinTipo.Add(elegido.name); continue; }

                var r = Undo.AddComponent<Residuo>(elegido.gameObject);
                r.tipo = tipo;
                r.nombre = NombreBonito(elegido.name);
                if (elegido.GetComponentInChildren<Collider>() == null) Undo.AddComponent<BoxCollider>(elegido.gameObject);
                EditorUtility.SetDirty(r);
                n++;
            }
            return n;
        }

        static bool Clasificar(string nombre, out TipoCaneca tipo)
        {
            tipo = TipoCaneca.Blanca;
            if (TieneClave(nombre, new[] { "caneca" })) return false;
            if (TieneClave(nombre, clavesNegra)) { tipo = TipoCaneca.Negra; return true; }
            if (TieneClave(nombre, clavesVerde)) { tipo = TipoCaneca.Verde; return true; }
            if (TieneClave(nombre, clavesBlanca)) { tipo = TipoCaneca.Blanca; return true; }
            return false;
        }

        static bool TieneClave(string nombre, string[] claves)
        {
            foreach (string tok in Palabras(nombre))
                foreach (string k in claves)
                    if (tok == k || (k.Length >= 4 && tok.StartsWith(k))) return true;
            return false;
        }

        // "BotellaPlastica_01" -> ["botella", "plastica"]
        static List<string> Palabras(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (i > 0 && char.IsUpper(ch) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(ch);
            }
            string t = SinTildes(sb.ToString().ToLowerInvariant());
            var lista = new List<string>();
            var actual = new StringBuilder();
            foreach (char ch in t)
            {
                if (ch >= 'a' && ch <= 'z') actual.Append(ch);
                else if (actual.Length > 0) { lista.Add(actual.ToString()); actual.Length = 0; }
            }
            if (actual.Length > 0) lista.Add(actual.ToString());
            return lista;
        }

        static string SinTildes(string s)
        {
            return s.Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u')
                    .Replace('ñ', 'n').Replace('ü', 'u');
        }

        static string NombreBonito(string s)
        {
            int p = s.IndexOf('(');
            if (p > 0) s = s.Substring(0, p);
            s = s.Replace('_', ' ').Replace('-', ' ').Trim();
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (i > 0 && char.IsUpper(ch) && char.IsLower(s[i - 1])) sb.Append(' ');
                sb.Append(ch);
            }
            s = sb.ToString().Trim();
            while (s.Length > 0 && char.IsDigit(s[s.Length - 1])) s = s.Substring(0, s.Length - 1).Trim();
            if (s.Length == 0) return "Residuo";
            return char.ToUpper(s[0]) + s.Substring(1).ToLower();
        }

        static void CrearResiduosEjemplo(Vector3 basePos, Vector3 adelante, Transform jugador)
        {
            var padre = new GameObject("KIBO_Residuos");
            Undo.RegisterCreatedObjectUndo(padre, "Residuos");

            var datos = new[]
            {
                new { n = "Botella de plástico", t = TipoCaneca.Blanca, f = PrimitiveType.Cylinder, e = new Vector3(0.12f, 0.15f, 0.12f), c = new Color(0.55f, 0.80f, 0.95f) },
                new { n = "Cáscara de banano",   t = TipoCaneca.Verde,  f = PrimitiveType.Capsule,  e = new Vector3(0.09f, 0.12f, 0.09f), c = new Color(0.95f, 0.85f, 0.20f) },
                new { n = "Servilleta usada",    t = TipoCaneca.Negra,  f = PrimitiveType.Cube,     e = new Vector3(0.22f, 0.04f, 0.22f), c = new Color(0.85f, 0.82f, 0.75f) },
                new { n = "Lata de aluminio",    t = TipoCaneca.Blanca, f = PrimitiveType.Cylinder, e = new Vector3(0.10f, 0.08f, 0.10f), c = new Color(0.75f, 0.75f, 0.78f) },
                new { n = "Manzana mordida",     t = TipoCaneca.Verde,  f = PrimitiveType.Sphere,   e = new Vector3(0.15f, 0.15f, 0.15f), c = new Color(0.80f, 0.12f, 0.10f) },
                new { n = "Papel higiénico usado", t = TipoCaneca.Negra, f = PrimitiveType.Cylinder, e = new Vector3(0.11f, 0.06f, 0.11f), c = new Color(0.92f, 0.92f, 0.90f) },
                new { n = "Caja de cartón",      t = TipoCaneca.Blanca, f = PrimitiveType.Cube,     e = new Vector3(0.35f, 0.25f, 0.30f), c = new Color(0.65f, 0.47f, 0.28f) },
            };

            for (int i = 0; i < datos.Length; i++)
            {
                var d = datos[i];
                float ang = Mathf.Lerp(-75f, 75f, i / (float)(datos.Length - 1));
                float dist = (i % 2 == 0) ? 2.5f : 6f;
                Vector3 p = Suelo(basePos + Quaternion.Euler(0, ang, 0) * adelante * dist, jugador);
                float medio = (d.f == PrimitiveType.Cylinder || d.f == PrimitiveType.Capsule) ? d.e.y : d.e.y * 0.5f;

                var go = GameObject.CreatePrimitive(d.f);
                go.name = d.n;
                go.transform.SetParent(padre.transform, false);
                go.transform.position = p + Vector3.up * (medio + 0.03f);
                go.transform.localScale = d.e;
                go.GetComponent<MeshRenderer>().sharedMaterial = Mat("Residuo_" + Archivo(d.n), d.c, 0.4f);
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                var r = go.AddComponent<Residuo>();
                r.nombre = d.n;
                r.tipo = d.t;
            }
        }

        // =====================================================================
        //  INTERFAZ
        // =====================================================================
        static void CrearInterfaz(VideoClip clip)
        {
            var go = new GameObject("KIBO_Interfaz", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Interfaz");
            go.layer = 5;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var esc = go.GetComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920, 1080);
            esc.matchWidthOrHeight = 0.5f;
            var ui = go.AddComponent<KiboUI>();
            Transform raiz = go.transform;

            // ---------- HUD ----------
            var hud = Lleno("HUD", raiz);

            var pm = NuevoRect("PanelMensaje", hud, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1150, 170));
            Img(pm, new Color(0.05f, 0.15f, 0.35f, 0.92f), true).raycastTarget = false;
            var tm = NuevoRect("Texto", pm, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-60, -30));
            var textoMensaje = Txt(tm, "", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);

            var ta = NuevoRect("TextoAyuda", hud, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1500, 70));
            var textoAyuda = Txt(ta, "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            ta.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

            var tp = NuevoRect("TextoPuntaje", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(35, -30), new Vector2(600, 60));
            var textoPuntaje = Txt(tp, "", 34, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            tp.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.9f);

            // ---------- MENÚ ----------
            var menu = Lleno("PanelMenu", raiz);
            Img(menu, new Color(0.05f, 0.20f, 0.13f, 1f));

            // Franja decorativa con los colores de las canecas
            TipoCaneca[] tipos = { TipoCaneca.Blanca, TipoCaneca.Negra, TipoCaneca.Verde };
            for (int i = 0; i < 3; i++)
            {
                var f = NuevoRect("Franja" + i, menu, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 18));
                Img(f, InfoCaneca.ColorModelo(tipos[i])).raycastTarget = false;
            }

            var titulo = NuevoRect("Titulo", menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(1400, 200));
            var txTitulo = Txt(titulo, "KIBO", 180, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            titulo.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6, -6);
            var sub = NuevoRect("Subtitulo", menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1400, 90));
            Txt(sub, "EL ORIGEN", 70, new Color(0.55f, 0.95f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var frase = NuevoRect("Frase", menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1400, 60));
            Txt(frase, "Recoge los residuos y clasifícalos en la caneca correcta", 34, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter, FontStyle.Italic);

            var botonJugar = Boton("BotonJugar", menu, "JUGAR", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -90), new Vector2(460, 130), new Color(0.20f, 0.72f, 0.32f), 64);

            var leyenda = NuevoRect("Leyenda", menu, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(1700, 130));
            Txt(leyenda,
                "<b><color=#FFFFFF>BLANCA</color></b>: aprovechables     <b><color=#B5B5B5>NEGRA</color></b>: no aprovechables     <b><color=#5BE37A>VERDE</color></b>: orgánicos\n" +
                "<b>E</b>: recoger / echar en la caneca      <b>Q</b>: soltar",
                30, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);

            // ---------- VIDEO ----------
            var panelVideo = Lleno("PanelVideo", raiz);
            Img(panelVideo, Color.black);
            var pantalla = Lleno("Pantalla", panelVideo);
            var raw = pantalla.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            var ajuste = pantalla.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = 16f / 9f;

            var vp = panelVideo.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.skipOnDrop = true;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            vp.clip = clip;

            var botonOmitir = Boton("BotonOmitir", panelVideo, "OMITIR  >>", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-50, 50), new Vector2(320, 95), new Color(0, 0, 0, 0.65f), 40);
            botonOmitir.gameObject.SetActive(false);

            panelVideo.gameObject.SetActive(false); // KiboUI lo enciende al iniciar
            menu.gameObject.SetActive(true);        // visible en el editor

            // ---------- Referencias ----------
            ui.panelVideo = panelVideo.gameObject;
            ui.videoPlayer = vp;
            ui.pantallaVideo = raw;
            ui.botonOmitir = botonOmitir;
            ui.segundosParaOmitir = 8f;
            ui.panelMenu = menu.gameObject;
            ui.botonJugar = botonJugar;
            ui.panelMensaje = pm.gameObject;
            ui.textoMensaje = textoMensaje;
            ui.textoAyuda = textoAyuda;
            ui.textoPuntaje = textoPuntaje;
            EditorUtility.SetDirty(ui);
            txTitulo.raycastTarget = false;
        }

        static void PrepararEventSystem()
        {
            var es = Buscar<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                Undo.RegisterCreatedObjectUndo(go, "EventSystem");
#if ENABLE_INPUT_SYSTEM
                go.AddComponent<InputSystemUIInputModule>();
#else
                go.AddComponent<StandaloneInputModule>();
#endif
                return;
            }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Con el Input System nuevo el módulo antiguo da errores: se reemplaza
            var viejo = es.GetComponent<StandaloneInputModule>();
            if (viejo != null)
            {
                Undo.DestroyObjectImmediate(viejo);
                Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
            }
#endif
        }

        static VideoClip BuscarVideo()
        {
            string[] guids = AssetDatabase.FindAssets("t:VideoClip");
            VideoClip elegido = null;
            foreach (string g in guids)
            {
                var c = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(g));
                if (c == null) continue;
                string n = c.name.ToLowerInvariant();
                if (elegido == null || n.Contains("intro") || n.Contains("inicio")) elegido = c;
            }
            return elegido;
        }

        // ---------- Ayudantes de UI ----------
        static RectTransform NuevoRect(string n, Transform padre, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 pos, Vector2 tam)
        {
            var go = new GameObject(n, typeof(RectTransform));
            go.layer = 5;
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(padre, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return rt;
        }

        static RectTransform Lleno(string n, Transform padre)
        {
            return NuevoRect(n, padre, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        static Image Img(RectTransform rt, Color c, bool redondo = false)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.color = c;
            if (redondo && spriteRedondo != null) { i.sprite = spriteRedondo; i.type = Image.Type.Sliced; }
            return i;
        }

        static Text Txt(RectTransform rt, string s, int tam, Color c, TextAnchor a, FontStyle estilo)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = fuente;
            t.text = s;
            t.fontSize = tam;
            t.color = c;
            t.alignment = a;
            t.fontStyle = estilo;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static Button Boton(string n, Transform padre, string texto, Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 pos, Vector2 tam, Color fondo, int fs)
        {
            var rt = NuevoRect(n, padre, aMin, aMax, piv, pos, tam);
            var img = Img(rt, fondo, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colores = b.colors;
            colores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = colores;
            rt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);
            var lbl = Lleno("Texto", rt);
            Txt(lbl, texto, fs, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        // =====================================================================
        //  UTILIDADES
        // =====================================================================
        static void CrearCarpetas()
        {
            if (!AssetDatabase.IsValidFolder(RAIZ)) AssetDatabase.CreateFolder("Assets", "KiboElOrigen");
            if (!AssetDatabase.IsValidFolder(MAT)) AssetDatabase.CreateFolder(RAIZ, "Materiales");
            if (!AssetDatabase.IsValidFolder(MOD)) AssetDatabase.CreateFolder(RAIZ, "Modelos");
            if (!AssetDatabase.IsValidFolder(PRE)) AssetDatabase.CreateFolder(RAIZ, "Prefabs");
        }

        static void CargarRecursos()
        {
            fuente = null;
#if UNITY_2022_2_OR_NEWER
            try { fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
#else
            try { fuente = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
#endif
            spriteRedondo = null;
            try { spriteRedondo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); } catch { }
        }

        static Material Mat(string nombre, Color c, float suavidad)
        {
            string ruta = MAT + "/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(ShaderBase());
                AssetDatabase.CreateAsset(m, ruta);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Shader ShaderBase()
        {
            Shader s = null;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            {
                s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("HDRP/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        static string Archivo(string s)
        {
            s = SinTildes(s.ToLowerInvariant());
            var sb = new StringBuilder();
            foreach (char ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.ToString();
        }

        static Transform BuscarJugador()
        {
            GameObject p = null;
            try { p = GameObject.FindGameObjectWithTag("Player"); } catch { }
            if (p) return p.transform;
            var cam = Camera.main;
            return cam ? cam.transform : null;
        }

        static void Referencia(Transform jugador, out Vector3 pos, out Vector3 adelante)
        {
            if (jugador) { pos = jugador.position; adelante = jugador.forward; }
            else if (SceneView.lastActiveSceneView != null)
            {
                var sv = SceneView.lastActiveSceneView;
                pos = sv.pivot; adelante = sv.rotation * Vector3.forward;
            }
            else { pos = Vector3.zero; adelante = Vector3.forward; }
            adelante.y = 0;
            if (adelante.sqrMagnitude < 0.001f) adelante = Vector3.forward;
            adelante.Normalize();
            pos = Suelo(pos, jugador);
        }

        // Busca el piso debajo de un punto (ignorando al jugador)
        static Vector3 Suelo(Vector3 p, Transform ignorar)
        {
            var hits = Physics.RaycastAll(p + Vector3.up * 3f, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider is CharacterController) continue;
                if (ignorar != null && h.transform.IsChildOf(ignorar)) continue;
                if (h.collider.GetComponentInParent<Caneca>() != null) continue;
                return h.point;
            }
            return p;
        }

        static T Buscar<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<T>(true);
#endif
        }

        static T[] BuscarTodos<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>(true);
#endif
        }
    }
}
