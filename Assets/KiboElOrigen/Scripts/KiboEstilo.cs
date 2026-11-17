using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace KiboElOrigen
{
    public enum EstiloTexto { Titulo, Normal, Plano }

    // Fábrica de interfaz con la estética de madera del juego (tablas, letras blancas con sombra café).
    // Funciona en el editor y en el juego. Los dibujos están en KiboElOrigen/Resources/KiboUI.
    public static class KiboEstilo
    {
        // Tamaño general de las ventanas de Kibo (1 = original, más pequeño = ventanas más pequeñas)
        public static float EscalaInterfaz = 0.8f;

        public static readonly Color Cafe = new Color32(105, 58, 35, 255);
        public static readonly Color CafeMedio = new Color32(150, 95, 45, 255);
        public static readonly Color Madera = new Color32(225, 167, 103, 255);
        public static readonly Color MaderaClara = new Color32(246, 199, 152, 255);
        public static readonly Color VerdeBien = new Color32(70, 190, 90, 255);
        public static readonly Color RojoMal = new Color32(225, 70, 55, 255);
        public static readonly Color Dorado = new Color32(255, 200, 50, 255);

        public const string RUTA_FUENTES = "KiboUI/Fuentes/";
        public const string FUENTE_TITULO = "Kibo Titulo SDF";
        public const string FUENTE_TEXTO = "Kibo Texto SDF";

        static TMP_FontAsset fuenteTitulo, fuenteTexto;
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();

        // Letra gruesa (títulos y botones)
        public static TMP_FontAsset FuenteTitulo
        {
            get
            {
                if (fuenteTitulo == null) fuenteTitulo = CargarFuente(FUENTE_TITULO, "LondrinaSolid-Black");
                return fuenteTitulo;
            }
        }

        // Letra normal (textos largos)
        public static TMP_FontAsset FuenteTexto
        {
            get
            {
                if (fuenteTexto == null) fuenteTexto = CargarFuente(FUENTE_TEXTO, "LondrinaSolid-Regular");
                if (fuenteTexto == null) fuenteTexto = FuenteTitulo;
                return fuenteTexto;
            }
        }

        static TMP_FontAsset CargarFuente(string asset, string ttf)
        {
            var fa = Resources.Load<TMP_FontAsset>(RUTA_FUENTES + asset);
            if (fa != null) return fa;
            var f = Resources.Load<Font>("Fonts & Materials/" + ttf);
            if (f != null)
            {
                try { fa = TMP_FontAsset.CreateFontAsset(f); }
                catch (System.Exception e) { Debug.LogWarning("Kibo: no se pudo crear la fuente " + ttf + ": " + e.Message); }
                if (fa != null) fa.name = asset;
            }
            if (fa == null) fa = TMP_Settings.defaultFontAsset;
            return fa;
        }

        // Material de la letra según el estilo (borde y sombra café)
        public static Material MaterialDe(TMP_FontAsset fa, EstiloTexto estilo)
        {
            if (fa == null) return null;
            if (estilo == EstiloTexto.Plano) return fa.material;
            string clave = fa.name + " " + estilo;
            Material m;
            if (materiales.TryGetValue(clave, out m) && m != null) return m;
            m = Resources.Load<Material>(RUTA_FUENTES + clave);
            if (m == null && fa.material != null)
            {
                m = new Material(fa.material);
                m.name = clave;
                ConfigurarMaterial(m, estilo);
            }
            materiales[clave] = m;
            return m;
        }

        public static void ConfigurarMaterial(Material m, EstiloTexto estilo)
        {
            bool titulo = estilo == EstiloTexto.Titulo;
            Color cafe = Cafe;
            m.EnableKeyword("OUTLINE_ON");
            m.SetColor("_OutlineColor", cafe);
            m.SetFloat("_OutlineWidth", titulo ? 0.16f : 0.07f);
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", titulo ? cafe : new Color(cafe.r, cafe.g, cafe.b, 0.8f));
            m.SetFloat("_UnderlayOffsetX", titulo ? 0.55f : 0.3f);
            m.SetFloat("_UnderlayOffsetY", titulo ? -0.75f : -0.45f);
            m.SetFloat("_UnderlayDilate", titulo ? 0.25f : 0.05f);
            m.SetFloat("_UnderlaySoftness", 0f);
            m.SetFloat("_FaceDilate", titulo ? 0.04f : 0f);
        }

        public static Sprite Dibujo(string nombre)
        {
            Sprite s;
            if (sprites.TryGetValue(nombre, out s) && s != null) return s;
            s = Resources.Load<Sprite>("KiboUI/" + nombre);
            if (s == null)
            {
                // Por si la imagen se importó como textura normal
                var tex = Resources.Load<Texture2D>("KiboUI/" + nombre);
                if (tex != null) s = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            sprites[nombre] = s;
            return s;
        }

        // ------------------------------------------------------------------
        //  Básicos
        // ------------------------------------------------------------------
        public static RectTransform Rect(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            return rt;
        }

        public static RectTransform Rect(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 pos, Vector2 tam)
        {
            var rt = Rect(nombre, padre);
            rt.anchorMin = anclaMin;
            rt.anchorMax = anclaMax;
            rt.pivot = pivote;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            return rt;
        }

        public static RectTransform Centro(string nombre, Transform padre, Vector2 pos, Vector2 tam)
        {
            var c = new Vector2(0.5f, 0.5f);
            return Rect(nombre, padre, c, c, c, pos, tam);
        }

        public static RectTransform Lleno(string nombre, Transform padre)
        {
            var rt = Rect(nombre, padre);
            Estirar(rt);
            return rt;
        }

        public static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image Imagen(RectTransform rt, string dibujo, Color color, bool cortado = false, float multiplicador = 1f)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            img.sprite = string.IsNullOrEmpty(dibujo) ? null : Dibujo(dibujo);
            img.color = color;
            img.raycastTarget = false;
            if (cortado && img.sprite != null)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = multiplicador;
            }
            else
            {
                img.type = Image.Type.Simple;
                img.preserveAspect = img.sprite != null;
            }
            return img;
        }

        // titulo=true: letra gruesa. titulo=false: letra normal. sombra=false: sin borde ni sombra.
        public static TextMeshProUGUI Texto(RectTransform rt, string s, float tam, Color color, TextAnchor alineacion, bool titulo = true, bool sombra = true)
        {
            var estilo = !sombra ? EstiloTexto.Plano : (titulo ? EstiloTexto.Titulo : EstiloTexto.Normal);
            return Texto(rt, s, tam, color, alineacion, estilo, titulo);
        }

        public static TextMeshProUGUI Texto(RectTransform rt, string s, float tam, Color color, TextAnchor alineacion, EstiloTexto estilo, bool gruesa)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var fa = gruesa ? FuenteTitulo : FuenteTexto;
            if (fa != null)
            {
                t.font = fa;
                var m = MaterialDe(fa, estilo);
                if (m != null) t.fontSharedMaterial = m;
            }
            t.text = s;
            t.fontSize = tam;
            t.color = color;
            t.alignment = Alinear(alineacion);
            t.richText = true;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.lineSpacing = -6f;
            return t;
        }

        public static void AutoTam(TMP_Text t, float min, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
        }

        static TextAlignmentOptions Alinear(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }

        public static TextMeshProUGUI TextoLleno(Transform padre, string s, float tam, Color color, TextAnchor alineacion, bool titulo = true, bool sombra = true)
        {
            return Texto(Lleno("Texto", padre), s, tam, color, alineacion, titulo, sombra);
        }

        // ------------------------------------------------------------------
        //  Canvas y EventSystem
        // ------------------------------------------------------------------
        public static Canvas CrearCanvas(string nombre, int orden, Transform padre = null)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            if (padre != null) go.transform.SetParent(padre, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = orden;
            var esc = go.GetComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920, 1080) / Mathf.Clamp(EscalaInterfaz, 0.4f, 1.5f);
            esc.matchWidthOrHeight = 0.5f;
            AsegurarEventSystem();
            return c;
        }

        public static void AsegurarEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(null);
        }

        // ------------------------------------------------------------------
        //  Componentes con estilo
        // ------------------------------------------------------------------
        public static Button Boton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam, UnityAction alClic, int tamLetra = 46)
        {
            var rt = Centro(nombre, padre, pos, tam);
            var img = Imagen(rt, "boton", Color.white, true, 1.25f);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var col = b.colors;
            col.normalColor = Color.white;
            col.highlightedColor = new Color(1f, 0.96f, 0.88f);
            col.pressedColor = new Color(0.85f, 0.78f, 0.7f);
            col.selectedColor = Color.white;
            col.fadeDuration = 0.08f;
            b.colors = col;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            var lbl = Rect("Texto", rt);
            Estirar(lbl);
            lbl.offsetMin = new Vector2(18, 6);
            lbl.offsetMax = new Vector2(-18, -4);
            var t = Texto(lbl, etiqueta, tamLetra, Color.white, TextAnchor.MiddleCenter);
            AutoTam(t, 14, tamLetra);
            if (alClic != null) b.onClick.AddListener(alClic);
            rt.gameObject.AddComponent<KiboBotonEfecto>();
            return b;
        }

        public static Button BotonX(Transform padre, Vector2 pos, float tam, UnityAction alClic)
        {
            var rt = Rect("Btn_Cerrar", padre, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), pos, new Vector2(tam, tam * 0.8f));
            var img = Imagen(rt, "boton_x", Color.white);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            if (alClic != null) b.onClick.AddListener(alClic);
            rt.gameObject.AddComponent<KiboBotonEfecto>();
            return b;
        }

        // Tablero de madera con su óvalo de título. Devuelve la raíz; el área de contenido es raiz.Find("Contenido").
        public static RectTransform Panel(Transform padre, string nombre, Vector2 pos, Vector2 tam, string titulo, bool engranajes = false)
        {
            var raiz = Centro(nombre, padre, pos, tam);
            var fondo = Imagen(raiz, "panel", Color.white, true, 1.35f);
            fondo.raycastTarget = true;

            var contenido = Rect("Contenido", raiz);
            Estirar(contenido);
            contenido.offsetMin = new Vector2(100, 70);
            contenido.offsetMax = new Vector2(-95, -105);

            if (!string.IsNullOrEmpty(titulo))
            {
                float ancho = Mathf.Clamp(170 + titulo.Length * 26, 380, Mathf.Min(720, tam.x * 0.75f));
                if (engranajes)
                {
                    var eng = Rect("Engranajes", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f),
                        new Vector2(-ancho * 0.22f, 92), new Vector2(170, 75));
                    Imagen(eng, "engranajes", Color.white);
                }
                var ov = Rect("Titulo", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(ancho, 140));
                Imagen(ov, "ovalo", Color.white).preserveAspect = false;
                var tx = Rect("Texto", ov);
                Estirar(tx);
                tx.offsetMin = new Vector2(40, 18);
                tx.offsetMax = new Vector2(-40, -14);
                var t = Texto(tx, titulo, 58, Color.white, TextAnchor.MiddleCenter);
                AutoTam(t, 20, 58);
            }
            return raiz;
        }

        public static RectTransform Contenido(RectTransform panel)
        {
            var c = panel.Find("Contenido");
            return c != null ? (RectTransform)c : panel;
        }

        public static Image Oscurecer(Transform padre, float alfa = 0.65f)
        {
            var rt = Lleno("Oscuro", padre);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.08f, 0.05f, 0.02f, alfa);
            img.raycastTarget = true; // bloquea clics al mundo
            return img;
        }

        public static Slider Deslizador(Transform padre, string nombre, Vector2 pos, float ancho, float valor, UnityAction<float> alCambiar)
        {
            var rt = Centro(nombre, padre, pos, new Vector2(ancho, 44));
            var fondo = Lleno("Fondo", rt);
            fondo.offsetMin = new Vector2(0, 8);
            fondo.offsetMax = new Vector2(0, -8);
            Imagen(fondo, "barra_fondo", Color.white, true, 2f);

            var area = Lleno("Relleno", rt);
            area.offsetMin = new Vector2(7, 14);
            area.offsetMax = new Vector2(-7, -14);
            var relleno = Lleno("Barra", area);
            Imagen(relleno, "redondo", KiboEstilo.VerdeBien, true, 2.5f);

            var zona = Lleno("Zona", rt);
            zona.offsetMin = new Vector2(16, 0);
            zona.offsetMax = new Vector2(-16, 0);
            var mango = Rect("Mango", zona, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 8));
            var imgMango = Imagen(mango, "circulo", MaderaClara);
            imgMango.raycastTarget = true;
            var borde = mango.gameObject.AddComponent<Outline>();
            borde.effectColor = Cafe;
            borde.effectDistance = new Vector2(3, -3);

            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = relleno;
            s.handleRect = mango;
            s.targetGraphic = imgMango;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = valor;
            var nav = s.navigation;
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
            // el fondo también recibe clics
            fondo.GetComponent<Image>().raycastTarget = true;
            if (alCambiar != null) s.onValueChanged.AddListener(alCambiar);
            return s;
        }

        public static TextMeshProUGUI Etiqueta(Transform padre, string s, Vector2 pos, Vector2 tam, float letra, TextAnchor al = TextAnchor.MiddleLeft, bool titulo = true)
        {
            var rt = Centro("Etiqueta", padre, pos, tam);
            return Texto(rt, s, letra, Color.white, al, titulo);
        }

        // Texto café sin sombra, como "GUARDAR PARTIDA" de las tablas
        public static TextMeshProUGUI EtiquetaCafe(Transform padre, string s, Vector2 pos, Vector2 tam, float letra, TextAnchor al = TextAnchor.MiddleCenter)
        {
            var rt = Centro("Etiqueta", padre, pos, tam);
            return Texto(rt, s, letra, Cafe, al, false, false);
        }
    }
}
