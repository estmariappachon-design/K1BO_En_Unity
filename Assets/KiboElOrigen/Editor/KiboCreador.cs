using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace KiboElOrigen.Herramientas
{
    // Menú superior de Unity:  Kibo el Origen > Crear Todo
    // Hace todos los cambios en las escenas Main_inicio y MainScene automáticamente.
    public static class KiboCreador
    {
        const string RAIZ = "Assets/KiboElOrigen";
        const string MAT = RAIZ + "/Materiales";
        const string MOD = RAIZ + "/Modelos";
        const string PRE = RAIZ + "/Prefabs";
        const string UI = RAIZ + "/Resources/KiboUI";
        const string FUENTES = UI + "/Fuentes";
        const string TTF = "Assets/TextMesh Pro/Resources/Fonts & Materials/";

        static readonly Dictionary<string, Vector4> bordes = new Dictionary<string, Vector4>
        {
            // x = izquierda, y = abajo, z = derecha, w = arriba
            { "panel", new Vector4(180, 84, 176, 185) },
            { "boton", new Vector4(75, 18, 70, 18) },
            { "slot", new Vector4(22, 22, 22, 22) },
            { "redondo", new Vector4(20, 20, 20, 20) },
            { "barra_fondo", new Vector4(22, 22, 22, 22) },
        };

        // =====================================================================
        //  CREAR TODO
        // =====================================================================
        [MenuItem("Kibo el Origen/Crear Todo", false, 0)]
        public static void CrearTodo()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Kibo el Origen", "Primero detén el juego (botón Play) y vuelve a intentarlo.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string rutaInicio = BuscarEscena("Main_inicio");
            string rutaJuego = BuscarEscena("MainScene");
            var informe = new StringBuilder();
            GameObject canecasCreadas = null;

            try
            {
                EditorUtility.DisplayProgressBar("Kibo el Origen", "Preparando imágenes...", 0.1f);
                CrearCarpetas();
                PrepararImagenes();
                EditorUtility.DisplayProgressBar("Kibo el Origen", "Letras en alta definición...", 0.18f);
                CrearRecursos();

                EditorUtility.DisplayProgressBar("Kibo el Origen", "Clasificando residuos...", 0.25f);
                ClasificarResiduos(informe);

                EditorUtility.DisplayProgressBar("Kibo el Origen", "Escenas del juego...", 0.35f);
                PrepararBuildSettings(rutaInicio, rutaJuego);

                if (rutaInicio != null)
                {
                    EditorUtility.DisplayProgressBar("Kibo el Origen", "Menú de inicio (botón Omitir)...", 0.45f);
                    PrepararInicio(rutaInicio, informe);
                }
                else informe.AppendLine("⚠ No encontré la escena Main_inicio.");

                if (rutaJuego != null)
                {
                    EditorUtility.DisplayProgressBar("Kibo el Origen", "Nivel: canecas e interfaz...", 0.7f);
                    canecasCreadas = PrepararNivel(rutaJuego, informe);
                }
                else informe.AppendLine("⚠ No encontré la escena MainScene.");

                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                informe.AppendLine("\n⚠ Ocurrió un error: " + e.Message + "\n(mira la consola para más detalles)");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (canecasCreadas != null)
            {
                Selection.activeGameObject = canecasCreadas;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            }

            informe.AppendLine("\nListo. Las escenas se guardaron solas.");
            informe.AppendLine("Mueve las canecas con la herramienta Mover (W) y dale Play desde Main_inicio.");
            EditorUtility.DisplayDialog("Kibo el Origen  ✔", informe.ToString(), "¡Listo!");
        }

        // =====================================================================
        //  OTRAS OPCIONES DEL MENÚ
        // =====================================================================
        [MenuItem("Kibo el Origen/Agregar 3 canecas frente a la vista", false, 20)]
        static void AgregarCanecasAqui()
        {
            var escena = SceneManager.GetActiveScene();
            if (!escena.IsValid()) return;
            CrearCarpetas();
            PrepararImagenes();
            CrearRecursos();
            Vector3 pos = Vector3.zero, adelante = Vector3.forward;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                adelante = sv.rotation * Vector3.forward;
                adelante.y = 0;
                if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
                adelante.Normalize();
                pos = sv.pivot;
            }
            var padre = CrearCanecas(pos, adelante, null, null);
            Undo.RegisterCreatedObjectUndo(padre, "Canecas");
            EditorSceneManager.MarkSceneDirty(escena);
            Selection.activeGameObject = padre;
        }

        [MenuItem("Kibo el Origen/Borrar partida guardada y puntajes", false, 40)]
        static void BorrarPartida()
        {
            KiboGuardado.Borrar();
            PlayerPrefs.DeleteKey("Kibo_Mejor");
            PlayerPrefs.Save();
            EditorUtility.DisplayDialog("Kibo el Origen", "Se borró la partida guardada y la mejor puntuación.", "OK");
        }

        [MenuItem("Kibo el Origen/Ayuda", false, 60)]
        static void Ayuda()
        {
            EditorUtility.DisplayDialog("Kibo el Origen",
                "1. Menú Kibo el Origen > Crear Todo (hace todos los cambios).\n" +
                "2. En MainScene mueve las 3 canecas (objeto KIBO_Canecas) a donde quieras.\n" +
                "3. Abre Main_inicio y dale Play.\n\n" +
                "En el juego:\n" +
                "• Haz clic en un residuo para recogerlo: aparece en qué caneca va.\n" +
                "• Haz clic en una caneca (letrero CLICK) y arrastra los residuos a la caneca correcta.\n" +
                "• Ganas cuando clasificas bien el 50% de los residuos del nivel.\n" +
                "• ESC: pausa (continuar, ajustes, guardar, menú).\n\n" +
                "Para cambiar la caneca de un residuo: selecciona su ItemData (Interfaz/Inventario/Item),\n" +
                "marca 'Caneca Manual' y elige el color.\n" +
                "Para cambiar la meta (50%): objeto KIBO_Sistema > Kibo Juego > Porcentaje Meta.", "OK");
        }

        // =====================================================================
        //  IMÁGENES DE LA INTERFAZ
        // =====================================================================
        static void PrepararImagenes()
        {
            if (!AssetDatabase.IsValidFolder(UI)) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UI }))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (ti == null) continue;
                string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
                Vector4 borde;
                if (!bordes.TryGetValue(nombre, out borde)) borde = Vector4.zero;
                bool cambiar = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                               || ti.spriteBorder != borde || ti.mipmapEnabled || !ti.alphaIsTransparency
                               || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.filterMode != FilterMode.Bilinear;
                if (!cambiar) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.spriteBorder = borde;
                ti.textureCompression = TextureImporterCompression.Uncompressed; // sin pérdida de calidad
                ti.filterMode = FilterMode.Bilinear;
                var ajustes = new TextureImporterSettings();
                ti.ReadTextureSettings(ajustes);
                ajustes.spriteMeshType = SpriteMeshType.FullRect;
                ti.SetTextureSettings(ajustes);
                ti.SaveAndReimport();
            }
        }

        // =====================================================================
        //  RECURSOS: letras TextMeshPro (nítidas), materiales del cielo, chispas y bombillos
        // =====================================================================
        static void CrearRecursos()
        {
            if (!AssetDatabase.IsValidFolder(FUENTES)) AssetDatabase.CreateFolder(UI, "Fuentes");
            var titulo = CrearFuente("LondrinaSolid-Black", KiboEstilo.FUENTE_TITULO);
            var texto = CrearFuente("LondrinaSolid-Regular", KiboEstilo.FUENTE_TEXTO);
            foreach (var fa in new[] { titulo, texto })
            {
                if (fa == null) continue;
                CrearMaterialFuente(fa, EstiloTexto.Titulo);
                CrearMaterialFuente(fa, EstiloTexto.Normal);
            }

            // Chispas del teletransporte (en Resources para que funcionen en el juego compilado)
            string rutaP = UI + "/Particula.mat";
            var mp = AssetDatabase.LoadAssetAtPath<Material>(rutaP);
            if (mp == null)
            {
                var sh = Shader.Find("Legacy Shaders/Particles/Additive");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                mp = new Material(sh);
                AssetDatabase.CreateAsset(mp, rutaP);
            }
            mp.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(UI + "/brillo.png");
            EditorUtility.SetDirty(mp);

            AssetDatabase.SaveAssets();
        }

        static TMP_FontAsset CrearFuente(string ttf, string nombre)
        {
            string ruta = FUENTES + "/" + nombre + ".asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
            if (fa != null) return fa;

            var fuente = AssetDatabase.LoadAssetAtPath<Font>(TTF + ttf + ".ttf");
            if (fuente == null)
                foreach (var guid in AssetDatabase.FindAssets(ttf + " t:Font"))
                {
                    fuente = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
                    if (fuente != null) break;
                }
            if (fuente == null) return null;

            fa = TMP_FontAsset.CreateFontAsset(fuente, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fa == null) return null;
            fa.name = nombre;
            AssetDatabase.CreateAsset(fa, ruta);
            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
            {
                fa.atlasTextures[0].name = nombre + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }
            if (fa.material != null)
            {
                fa.material.name = nombre + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            fa.TryAddCharacters("ABCDEFGHIJKLMNÑOPQRSTUVWXYZabcdefghijklmnñopqrstuvwxyzÁÉÍÓÚÜáéíóúü0123456789¡!¿?.,:;()/%+-<>=*\"' ");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static void CrearMaterialFuente(TMP_FontAsset fa, EstiloTexto estilo)
        {
            if (fa.material == null) return;
            string ruta = FUENTES + "/" + fa.name + " " + estilo + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(fa.material);
                AssetDatabase.CreateAsset(m, ruta);
            }
            KiboEstilo.ConfigurarMaterial(m, estilo);
            EditorUtility.SetDirty(m);
        }

        static Material MaterialCielo()
        {
            string ruta = MAT + "/Cielo_Kibo.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            var sh = Shader.Find("Kibo/Cielo");
            if (sh == null) return null;
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, ruta);
            }
            return m;
        }

        static Material MaterialBombillo()
        {
            string ruta = MAT + "/Bombillo_Poste.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = KiboCanecaModelo.MaterialBasico(new Color(1f, 0.92f, 0.75f), 0.6f);
                AssetDatabase.CreateAsset(m, ruta);
            }
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.5f) * 0.3f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        // =====================================================================
        //  RESIDUOS (ItemData): en qué caneca va cada uno
        // =====================================================================
        static void ClasificarResiduos(StringBuilder informe)
        {
            var lineas = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ItemData"))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;
                string clave = (item.nombreItem ?? "") + " " + item.name;
                bool cambio = false;
                if (!item.canecaManual)
                {
                    item.caneca = KiboClasificador.TipoPorNombre(clave);
                    item.canecaManual = true;
                    cambio = true;
                }
                if (string.IsNullOrEmpty(item.nombreVisible)) { item.nombreVisible = KiboClasificador.NombrePorClave(clave); cambio = true; }
                if (string.IsNullOrEmpty(item.explicacion)) { item.explicacion = KiboClasificador.ExplicacionPorClave(clave); cambio = true; }
                if (cambio) EditorUtility.SetDirty(item);
                lineas.Add("   " + item.nombreVisible + " → " + InfoCaneca.NombreColor(item.caneca));
            }
            if (lineas.Count > 0)
            {
                informe.AppendLine("• Residuos y su caneca (puedes cambiarlo en cada ItemData):");
                foreach (var l in lineas.OrderBy(x => x)) informe.AppendLine(l);
            }
        }

        // =====================================================================
        //  BUILD SETTINGS
        // =====================================================================
        static void PrepararBuildSettings(string inicio, string juego)
        {
            var lista = EditorBuildSettings.scenes.ToList();
            System.Action<string, int> asegurar = (ruta, indice) =>
            {
                if (ruta == null) return;
                int i = lista.FindIndex(s => s.path == ruta);
                EditorBuildSettingsScene es;
                if (i >= 0) { es = lista[i]; lista.RemoveAt(i); es.enabled = true; }
                else es = new EditorBuildSettingsScene(ruta, true);
                lista.Insert(Mathf.Min(indice, lista.Count), es);
            };
            asegurar(inicio, 0);
            asegurar(juego, 1);
            EditorBuildSettings.scenes = lista.ToArray();
        }

        // =====================================================================
        //  ESCENA Main_inicio: botón OMITIR a los 3 segundos
        // =====================================================================
        static void PrepararInicio(string ruta, StringBuilder informe)
        {
            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            var mm = BuscarEnEscena<MainMenuManager>(escena).FirstOrDefault();
            if (mm == null)
            {
                informe.AppendLine("⚠ Main_inicio no tiene MainMenuManager.");
                return;
            }

            // El botón OMITIR se crea al jugar (con letras nítidas). Se quita el de la versión anterior.
            foreach (var go in TodosEnEscena(escena).Where(g => g != null && g.name == "Btn_Omitir").ToList())
                Object.DestroyImmediate(go);
            mm.botonOmitir = null;
            mm.segundosParaOmitir = 3f;
            mm.crearBotonesExtra = true;
            EditorUtility.SetDirty(mm);

            AsegurarEventSystem(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            informe.AppendLine("• Main_inicio: botón OMITIR abajo a la derecha (aparece a los 3 s del video),");
            informe.AppendLine("  botón de ajustes, CONTINUAR (si hay partida guardada) y SALIR.");
        }

        // =====================================================================
        //  ESCENA MainScene: canecas, sistema de juego e interfaz
        // =====================================================================
        static GameObject PrepararNivel(string ruta, StringBuilder informe)
        {
            var escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            Physics.SyncTransforms();

            // ---- 1. Quitar lo de la versión anterior ----
            int quitados = 0;
            foreach (var go in TodosEnEscena(escena).Where(g => g != null && g.name == "KIBO_Interfaz").ToList())
            {
                Object.DestroyImmediate(go);
                quitados++;
            }
            foreach (var r in BuscarEnEscena<Residuo>(escena))
            {
                if (r != null) { Object.DestroyImmediate(r); quitados++; }
            }

            var posiciones = new Dictionary<TipoCaneca, Pose>();
            Transform padreViejo = null;
            foreach (var c in BuscarEnEscena<Caneca>(escena))
            {
                if (c == null || c.version >= Caneca.VERSION_ACTUAL) continue;
                if (!posiciones.ContainsKey(c.tipo)) posiciones[c.tipo] = new Pose(c.transform.position, c.transform.rotation);
                padreViejo = c.transform.parent;
                Object.DestroyImmediate(c.gameObject);
                quitados++;
            }
            if (quitados > 0) informe.AppendLine("• Se actualizaron " + quitados + " elementos de la versión anterior.");

            // ---- 2. Sistema de juego ----
            var juego = BuscarEnEscena<KiboJuego>(escena).FirstOrDefault();
            if (juego == null)
            {
                var go = new GameObject("KIBO_Sistema");
                SceneManager.MoveGameObjectToScene(go, escena);
                juego = go.AddComponent<KiboJuego>();
            }
            juego.gameObject.name = "KIBO_Sistema";
            var jugador = BuscarEnEscena<PlayerMovement>(escena).FirstOrDefault();
            if (jugador != null) juego.jugador = jugador.transform;
            juego.porcentajeMeta = 0.5f;
            EditorUtility.SetDirty(juego);

            var ui = BuscarEnEscena<KiboUI>(escena).FirstOrDefault();
            if (ui == null) ui = juego.gameObject.AddComponent<KiboUI>();
            ui.historieta = BuscarVideo();
            EditorUtility.SetDirty(ui);

            // Cielo con nubes y estrellas
            var cielo = juego.GetComponent<KiboCielo>();
            if (cielo == null) cielo = juego.gameObject.AddComponent<KiboCielo>();
            var matCielo = MaterialCielo();
            cielo.materialCielo = matCielo;
            if (matCielo != null) RenderSettings.skybox = matCielo;
            EditorUtility.SetDirty(cielo);

            // Cámara que se puede girar (y opción de primera persona)
            Camera camara = null;
            foreach (var c in BuscarEnEscena<Camera>(escena))
                if (c != null && c.CompareTag("MainCamera")) { camara = c; break; }
            if (camara != null)
            {
                var kc = camara.GetComponent<KiboCamara>();
                if (kc == null) kc = camara.gameObject.AddComponent<KiboCamara>();
                if (jugador != null) kc.objetivo = jugador.transform;
                var vieja = camara.GetComponent<CrashCamera>();
                if (vieja != null) { vieja.enabled = false; EditorUtility.SetDirty(vieja); }
                EditorUtility.SetDirty(kc);
                informe.AppendLine("• Cámara: clic derecho + mouse (o Q/E, Z/X) para girarla, rueda para acercar, V = primera persona.");
                informe.AppendLine("  En el Inspector de Main Camera > Kibo Camara puedes elegir el Modo.");
            }

            int luces = CrearLucesPostes(escena, MaterialBombillo());
            if (luces > 0) informe.AppendLine("• " + luces + " luces de noche en los postes (se prenden solas al oscurecer).");

            int residuos = BuscarEnEscena<ItemRecolectable>(escena).Count();
            informe.AppendLine("• Nivel: " + residuos + " residuos en el mapa. Para ganar hay que clasificar bien " +
                               Mathf.Max(1, Mathf.CeilToInt(residuos * 0.5f)) + " (50%).");

            // ---- 3. Canecas ----
            GameObject padre = null;
            var actuales = BuscarEnEscena<Caneca>(escena).Where(c => c != null).ToList();
            if (actuales.Count >= 3)
            {
                informe.AppendLine("• Las canecas ya existían: no se tocaron (puedes moverlas).");
                padre = actuales[0].transform.parent != null ? actuales[0].transform.parent.gameObject : actuales[0].gameObject;
            }
            else
            {
                Vector3 basePos = Vector3.zero, adelante = Vector3.forward;
                if (jugador != null)
                {
                    basePos = jugador.transform.position;
                    adelante = jugador.transform.forward;
                }
                adelante.y = 0;
                if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
                adelante.Normalize();
                padre = CrearCanecas(basePos, adelante, posiciones, jugador != null ? jugador.transform : null);
                if (padreViejo != null && padreViejo.childCount == 0 && padreViejo.gameObject != padre) Object.DestroyImmediate(padreViejo.gameObject);
                SceneManager.MoveGameObjectToScene(padre, escena);
                informe.AppendLine("• 3 canecas 3D (Blanca, Negra y Verde) con letrero flotante CLICK.");
                informe.AppendLine("  Están en KIBO_Canecas: muévelas a donde quieras. Prefabs en " + PRE);
            }

            AsegurarEventSystem(escena);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            informe.AppendLine("• Interfaz nueva: contador, avisos al recoger, ventana para arrastrar residuos a las canecas,");
            informe.AppendLine("  ¡Ganaste! (puntuación, recogidos, bien y fallos, Reintentar / Menú), pausa con ESC y ajustes.");
            informe.AppendLine("• Botones arreglados: Guardar partida, Ver historieta, Libro y Cerrar libro.");
            return padre;
        }

        // =====================================================================
        //  CANECAS 3D
        // =====================================================================
        static GameObject CrearCanecas(Vector3 basePos, Vector3 adelante, Dictionary<TipoCaneca, Pose> posiciones, Transform ignorar)
        {
            Mesh cuerpo = MallaGuardada("Cuerpo_Caneca", KiboCanecaModelo.MallaCuerpo());
            Mesh aro = MallaGuardada("Borde_Caneca", KiboCanecaModelo.MallaBorde());
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante).normalized;
            Vector3 centro = basePos + adelante * 4f;

            var padre = new GameObject("KIBO_Canecas");
            padre.transform.position = Suelo(centro, ignorar);

            for (int i = 0; i < 3; i++)
            {
                var tipo = InfoCaneca.Todas[i];
                var mats = Materiales(tipo);
                var go = KiboCanecaModelo.Construir(tipo, cuerpo, aro, mats);
                go.transform.SetParent(padre.transform, true);

                Pose pose;
                if (posiciones != null && posiciones.TryGetValue(tipo, out pose))
                {
                    go.transform.position = pose.position;
                    go.transform.rotation = pose.rotation;
                }
                else
                {
                    go.transform.position = Suelo(centro + derecha * ((i - 1) * 1.4f), ignorar);
                    go.transform.rotation = Quaternion.LookRotation(adelante); // el frente mira al jugador
                }

                string rutaPrefab = PRE + "/Caneca_" + InfoCaneca.Titulo(tipo) + ".prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, rutaPrefab, InteractionMode.AutomatedAction);
            }
            return padre;
        }

        static KiboCanecaModelo.Materiales Materiales(TipoCaneca tipo)
        {
            string col = InfoCaneca.Titulo(tipo);
            Color c = InfoCaneca.ColorModelo(tipo);
            return new KiboCanecaModelo.Materiales
            {
                cuerpo = Mat("Caneca_" + col, c, 0.45f, 0f),
                tapa = Mat("Caneca_Tapa_" + col, new Color(c.r * 0.82f, c.g * 0.82f, c.b * 0.82f), 0.4f, 0f),
                detalle = Mat("Caneca_Detalle", new Color(0.15f, 0.15f, 0.16f), 0.25f, 0f),
                metal = Mat("Caneca_Metal", new Color(0.62f, 0.64f, 0.66f), 0.7f, 0.6f)
            };
        }

        static Material Mat(string nombre, Color c, float suavidad, float metalico)
        {
            string ruta = MAT + "/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = KiboCanecaModelo.MaterialBasico(c, suavidad, metalico);
                AssetDatabase.CreateAsset(m, ruta);
            }
            c.a = 1f;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
            EditorUtility.SetDirty(m);
            return m;
        }

        // Guarda la malla como archivo (si ya existía la actualiza sin cambiar su referencia)
        static Mesh MallaGuardada(string nombre, Mesh nueva)
        {
            string ruta = MOD + "/" + nombre + ".asset";
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (existente != null)
            {
                EditorUtility.CopySerialized(nueva, existente);
                existente.name = nombre;
                EditorUtility.SetDirty(existente);
                Object.DestroyImmediate(nueva);
                return existente;
            }
            nueva.name = nombre;
            AssetDatabase.CreateAsset(nueva, ruta);
            return nueva;
        }

        // =====================================================================
        //  LUCES DE LOS POSTES
        // =====================================================================
        static int CrearLucesPostes(Scene escena, Material bombillo)
        {
            int n = 0;
            foreach (var mr in BuscarEnEscena<MeshRenderer>(escena))
            {
                if (mr == null) continue;
                if (!mr.gameObject.name.ToLowerInvariant().Contains("poste")) continue;
                if (mr.GetComponentInChildren<KiboLuzNoche>(true) != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                if (mr.bounds.size.y < 1.5f) continue;
                foreach (var p in CabezasDePoste(mf, mr))
                {
                    CrearLuz(mr.transform, p, bombillo);
                    n++;
                }
            }
            return n;
        }

        static void CrearLuz(Transform padre, Vector3 pos, Material bombillo)
        {
            var go = new GameObject("KIBO_LuzPoste");
            go.transform.SetParent(padre, true);
            go.transform.position = pos;
            var luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = 11f;
            luz.intensity = 0f;
            luz.color = new Color(1f, 0.82f, 0.52f);
            luz.shadows = LightShadows.None;
            luz.renderMode = LightRenderMode.Auto;

            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            b.name = "Bombillo";
            var col = b.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            b.transform.SetParent(go.transform, false);
            b.transform.localPosition = Vector3.up * 0.1f;
            Vector3 ls = go.transform.lossyScale;
            b.transform.localScale = new Vector3(0.2f / Mathf.Max(0.0001f, Mathf.Abs(ls.x)), 0.2f / Mathf.Max(0.0001f, Mathf.Abs(ls.y)), 0.2f / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
            var r = b.GetComponent<MeshRenderer>();
            r.sharedMaterial = bombillo;
            r.shadowCastingMode = ShadowCastingMode.Off;

            go.AddComponent<KiboLuzNoche>();
        }

        // Busca la(s) lámpara(s) en la parte de arriba del poste
        static List<Vector3> CabezasDePoste(MeshFilter mf, Renderer r)
        {
            var res = new List<Vector3>();
            Vector3[] v = null;
            try { v = mf.sharedMesh.vertices; } catch { v = null; }
            Bounds bb = r.bounds;
            if (v == null || v.Length < 8)
            {
                res.Add(new Vector3(bb.center.x, bb.max.y - 0.35f, bb.center.z));
                return res;
            }
            var w = new Vector3[v.Length];
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < v.Length; i++)
            {
                w[i] = mf.transform.TransformPoint(v[i]);
                minY = Mathf.Min(minY, w[i].y);
                maxY = Mathf.Max(maxY, w[i].y);
            }
            float alto = maxY - minY;
            float corteArriba = maxY - alto * 0.14f, corteAbajo = minY + alto * 0.1f;
            Vector3 baseP = Vector3.zero; int nb = 0;
            var arriba = new List<Vector3>();
            foreach (var p in w)
            {
                if (p.y <= corteAbajo) { baseP += p; nb++; }
                if (p.y >= corteArriba) arriba.Add(p);
            }
            if (nb == 0 || arriba.Count == 0)
            {
                res.Add(new Vector3(bb.center.x, bb.max.y - 0.35f, bb.center.z));
                return res;
            }
            baseP /= nb;
            Vector2 eje = new Vector2(baseP.x, baseP.z);
            float maxD = 0f; Vector2 lejos = eje;
            foreach (var p in arriba)
            {
                float d = Vector2.Distance(new Vector2(p.x, p.z), eje);
                if (d > maxD) { maxD = d; lejos = new Vector2(p.x, p.z); }
            }
            if (maxD < 0.35f)
            {
                res.Add(Cabeza(arriba));
                return res;
            }
            Vector2 dir = (lejos - eje).normalized;
            var a = new List<Vector3>(); var b = new List<Vector3>();
            float maxB = 0f;
            foreach (var p in arriba)
            {
                float d = Vector2.Dot(new Vector2(p.x, p.z) - eje, dir);
                if (d > maxD * 0.45f) a.Add(p);
                else if (d < -maxD * 0.2f) { b.Add(p); maxB = Mathf.Max(maxB, -d); }
            }
            if (a.Count > 0) res.Add(Cabeza(a));
            if (b.Count > arriba.Count * 0.15f && maxB > 0.35f) res.Add(Cabeza(b));
            if (res.Count == 0) res.Add(Cabeza(arriba));
            return res;
        }

        // Centro de la lámpara, un poco debajo de su parte más baja
        static Vector3 Cabeza(List<Vector3> pts)
        {
            Vector3 c = Vector3.zero;
            float minY = float.MaxValue;
            foreach (var p in pts) { c += p; minY = Mathf.Min(minY, p.y); }
            c /= pts.Count;
            return new Vector3(c.x, minY - 0.15f, c.z);
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

        static string BuscarEscena(string nombre)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene " + nombre))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant() == nombre.ToLowerInvariant() && !ruta.Contains("_Recovery"))
                    return ruta;
            }
            return null;
        }

        static VideoClip BuscarVideo()
        {
            VideoClip elegido = null;
            foreach (var guid in AssetDatabase.FindAssets("t:VideoClip"))
            {
                var c = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null) continue;
                string n = c.name.ToLowerInvariant();
                if (elegido == null || n.Contains("educativo") || n.Contains("k1b0") || n.Contains("intro")) elegido = c;
            }
            return elegido;
        }

        static IEnumerable<GameObject> TodosEnEscena(Scene escena)
        {
            foreach (var raiz in escena.GetRootGameObjects())
                foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                    yield return t.gameObject;
        }

        static List<T> BuscarEnEscena<T>(Scene escena) where T : Component
        {
            var lista = new List<T>();
            foreach (var raiz in escena.GetRootGameObjects())
                lista.AddRange(raiz.GetComponentsInChildren<T>(true));
            return lista;
        }

        static void AsegurarEventSystem(Scene escena)
        {
            if (BuscarEnEscena<EventSystem>(escena).Count > 0) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(go, escena);
        }

        // Busca el piso debajo de un punto
        static Vector3 Suelo(Vector3 p, Transform ignorar)
        {
            var hits = Physics.RaycastAll(p + Vector3.up * 3f, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (ignorar != null && h.transform.IsChildOf(ignorar)) continue;
                if (h.collider.GetComponentInParent<Caneca>() != null) continue;
                if (h.collider.CompareTag("ObjetoAgarrable")) continue;
                return h.point;
            }
            return p;
        }
    }
}
