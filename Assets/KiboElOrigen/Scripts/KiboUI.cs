using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace KiboElOrigen
{
    // Toda la interfaz del nivel: contador, avisos, ventana de clasificación, ¡Ganaste!, pausa y ajustes.
    // Se construye sola al iniciar el juego con la estética de madera.
    public class KiboUI : MonoBehaviour
    {
        public static KiboUI Instancia;

        [Tooltip("Video que se muestra con el botón 'Ver historieta' de la configuración")]
        public VideoClip historieta;

        [Tooltip("Mostrar el contador de residuos arriba en la pantalla")]
        public bool mostrarContador = true;

        [Tooltip("Segundos que dura un aviso en pantalla")]
        public float duracionAviso = 4.5f;

        Canvas canvas;
        RectTransform raiz;
        CanvasGroup grupoRaiz;
        GameObject panelInstrucciones;
        bool interfazMostrada;
        public RectTransform CapaArrastre { get; private set; }

        // HUD
        RectTransform hud;
        TMP_Text txContador, txDetalle;
        RectTransform hudRelleno;

        // Aviso
        RectTransform aviso;
        CanvasGroup grupoAviso;
        Image avisoIcono;
        TMP_Text avisoTexto;
        Coroutine rutinaAviso;

        // Ventanas
        GameObject capaClasificar, capaPausa, capaGanaste, capaAjustes, capaVideo, capaTarjeta;
        float escalaPrevia = 1f;
        CursorLockMode cursorPrevio;
        bool cursorVisiblePrevio = true;

        // Clasificación
        class Objetivo { public TipoCaneca tipo; public RectTransform zona; public RectTransform dibujo; }
        readonly List<Objetivo> objetivos = new List<Objetivo>();
        RectTransform bandeja;
        TMP_Text txStats, txVacio, txFeedback, txFeedbackSub;
        CanvasGroup grupoFeedback;
        Coroutine rutinaFeedback;
        Button btnTabBarra, btnTabMaleta;
        bool verMaleta;
        Caneca canecaActual;
        readonly List<TipoCaneca> celebrarAlCerrar = new List<TipoCaneca>();

        void Awake()
        {
            Instancia = this;

            // Restos de la versión anterior (video y menú dentro de MainScene): ya no se usan
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var h = transform.GetChild(i);
                if (h.name == "HUD" || h.name == "PanelMenu" || h.name == "PanelVideo") Destroy(h.gameObject);
            }

            canvas = KiboEstilo.CrearCanvas("KIBO_Canvas", 60, transform);
            raiz = (RectTransform)canvas.transform;
            grupoRaiz = canvas.gameObject.AddComponent<CanvasGroup>();
            grupoRaiz.alpha = 0f; // aparece cuando se cierran las instrucciones
            ConstruirHUD();
            ConstruirAviso();
            CapaArrastre = KiboEstilo.Lleno("CapaArrastre", raiz);
            CapaArrastre.SetAsLastSibling();
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Start()
        {
            ConectarBotonesDelJuego();
            panelInstrucciones = KiboUtil.BuscarPorNombre("Panel_Instrucciones");
            PrepararFondoInstrucciones();
            ActualizarHUD();
            StartCoroutine(MensajeInicial());
        }

        bool InstruccionesAbiertas
        {
            get { return panelInstrucciones != null && panelInstrucciones.activeInHierarchy; }
        }

        // Fondo blanco semitransparente detrás del tablero de instrucciones
        void PrepararFondoInstrucciones()
        {
            if (panelInstrucciones == null) return;
            if (panelInstrucciones.transform.Find("KIBO_FondoBlanco") != null) return;
            var rt = KiboEstilo.Lleno("KIBO_FondoBlanco", panelInstrucciones.transform);
            rt.SetAsFirstSibling();
            // un poco más grande que la pantalla por si el panel tiene márgenes
            rt.offsetMin = new Vector2(-4000, -4000);
            rt.offsetMax = new Vector2(4000, 4000);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.55f);
            img.raycastTarget = true;
        }

        IEnumerator MensajeInicial()
        {
            // Primero las instrucciones; el contador y los avisos salen cuando se cierran (X)
            yield return null;
            while (InstruccionesAbiertas) yield return null;
            yield return new WaitForSecondsRealtime(0.35f);
            interfazMostrada = true;
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                grupoRaiz.alpha = t / 0.5f;
                yield return null;
            }
            grupoRaiz.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.4f);
            var j = KiboJuego.Instancia;
            if (j != null && !j.Terminado && j.aciertos == 0)
                Aviso("Recoge residuos con clic y llévalos a las canecas.\nClasifica bien <color=#FFE066>" + j.meta + "</color> de " + j.total + " para ganar.", TipoCaneca.Verde);
        }

        void Update()
        {
            bool minijuego = GameManager.Instance != null && GameManager.Instance.estaEnMinijuego;
            bool ocultar = minijuego || InstruccionesAbiertas;
            if (canvas != null && canvas.enabled == ocultar) canvas.enabled = !ocultar;
            if (!interfazMostrada && grupoRaiz != null && HayVentana()) grupoRaiz.alpha = 1f;
        }

        // =====================================================================
        //  HUD (contador arriba)
        // =====================================================================
        void ConstruirHUD()
        {
            hud = KiboEstilo.Rect("Contador", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(500, 112));
            KiboEstilo.Imagen(hud, "boton", Color.white, true, 1.3f);

            var t = KiboEstilo.Rect("Texto", hud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(-40, 58));
            txContador = KiboEstilo.Texto(t, "", 36, Color.white, TextAnchor.MiddleCenter);

            var barra = KiboEstilo.Rect("Barra", hud, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(420, 30));
            KiboEstilo.Imagen(barra, "barra_fondo", Color.white, true, 2.5f);
            var area = KiboEstilo.Lleno("Area", barra);
            area.offsetMin = new Vector2(5, 5);
            area.offsetMax = new Vector2(-5, -5);
            hudRelleno = KiboEstilo.Lleno("Relleno", area);
            KiboEstilo.Imagen(hudRelleno, "redondo", KiboEstilo.VerdeBien, true, 3f);

            var d = KiboEstilo.Rect("Detalle", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(700, 40));
            txDetalle = KiboEstilo.Texto(d, "", 28, Color.white, TextAnchor.MiddleCenter, false);
        }

        public void ActualizarHUD()
        {
            var j = KiboJuego.Instancia;
            if (hud == null) return;
            hud.gameObject.SetActive(mostrarContador && j != null);
            txDetalle.gameObject.SetActive(mostrarContador && j != null);
            if (j == null) return;
            txContador.text = "CLASIFICADOS  <color=#FFE066>" + j.aciertos + "</color> / " + j.meta;
            float p = j.meta > 0 ? Mathf.Clamp01(j.aciertos / (float)j.meta) : 0f;
            hudRelleno.anchorMax = new Vector2(Mathf.Max(0.001f, p), 1f);
            hudRelleno.gameObject.SetActive(p > 0.001f);
            txDetalle.text = "RECOGIDOS: " + j.recolectados + "      FALLOS: " + j.fallos;
            if (txStats != null) txStats.text = TextoStats();
        }

        string TextoStats()
        {
            var j = KiboJuego.Instancia;
            if (j == null) return "";
            return "BIEN: <color=#9CFF9C>" + j.aciertos + "</color>      FALLOS: <color=#FFA08C>" + j.fallos + "</color>      META: " + j.aciertos + " / " + j.meta;
        }

        // =====================================================================
        //  AVISOS
        // =====================================================================
        void ConstruirAviso()
        {
            aviso = KiboEstilo.Rect("Aviso", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(1000, 170));
            KiboEstilo.Imagen(aviso, "boton", Color.white, true, 0.9f);
            grupoAviso = aviso.gameObject.AddComponent<CanvasGroup>();
            grupoAviso.alpha = 0f;
            grupoAviso.blocksRaycasts = false;
            grupoAviso.interactable = false;

            var ic = KiboEstilo.Rect("Icono", aviso, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(105, 0), new Vector2(120, 140));
            avisoIcono = KiboEstilo.Imagen(ic, "caneca_blanca", Color.white);

            var tx = KiboEstilo.Lleno("Texto", aviso);
            tx.offsetMin = new Vector2(185, 14);
            tx.offsetMax = new Vector2(-40, -12);
            avisoTexto = KiboEstilo.Texto(tx, "", 36, Color.white, TextAnchor.MiddleLeft, false);
            KiboEstilo.AutoTam(avisoTexto, 18, 36);
            aviso.gameObject.SetActive(false);
        }

        public void Aviso(string texto, TipoCaneca icono)
        {
            MostrarAviso(texto, InfoCaneca.Sprite(icono));
        }

        public void MostrarRecogido(ItemData item)
        {
            var tipo = KiboClasificador.Tipo(item);
            string nombre = KiboClasificador.Nombre(item);
            string expl = KiboClasificador.Explicacion(item);
            string s = "Recogiste: <color=#FFE066>" + nombre + "</color>\nEste residuo va en la caneca de color " + InfoCaneca.Pintado(tipo);
            if (!string.IsNullOrEmpty(expl)) s += "\n<size=75%>" + expl + "</size>";
            MostrarAviso(s, InfoCaneca.Sprite(tipo));
        }

        void MostrarAviso(string texto, string sprite)
        {
            if (aviso == null) return;
            avisoTexto.text = texto;
            avisoIcono.sprite = KiboEstilo.Dibujo(sprite);
            aviso.gameObject.SetActive(true);
            if (rutinaAviso != null) StopCoroutine(rutinaAviso);
            rutinaAviso = StartCoroutine(RutinaAviso());
        }

        IEnumerator RutinaAviso()
        {
            Vector2 fin = new Vector2(0, -175);
            Vector2 ini = fin + new Vector2(0, 60);
            float a0 = grupoAviso.alpha;
            for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / 0.25f, 3f);
                grupoAviso.alpha = Mathf.Lerp(a0, 1f, k);
                aviso.anchoredPosition = Vector2.Lerp(a0 > 0.5f ? fin : ini, fin, k);
                aviso.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.04f);
                yield return null;
            }
            grupoAviso.alpha = 1f;
            aviso.anchoredPosition = fin;
            aviso.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(duracionAviso);
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                grupoAviso.alpha = 1f - t / 0.4f;
                yield return null;
            }
            grupoAviso.alpha = 0f;
            aviso.gameObject.SetActive(false);
            rutinaAviso = null;
        }

        // =====================================================================
        //  ANUNCIO "CLICK PARA JUGAR EL MINIJUEGO"
        // =====================================================================
        GameObject anuncio;

        public void MostrarAnuncioMinijuego(string texto, UnityAction alClic)
        {
            OcultarAnuncioMinijuego();
            var root = KiboEstilo.Rect("AnuncioMinijuego", raiz, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(820, 150));
            var brillo = KiboEstilo.Centro("Brillo", root, Vector2.zero, new Vector2(1100, 360));
            KiboEstilo.Imagen(brillo, "brillo", new Color(1f, 0.92f, 0.5f, 0.6f)).preserveAspect = false;
            var pulso = brillo.gameObject.AddComponent<KiboPulso>();
            pulso.cantidad = 0.08f;
            pulso.velocidad = 5f;
            KiboEstilo.Boton(root, "Boton", texto, Vector2.zero, new Vector2(820, 150), () =>
            {
                OcultarAnuncioMinijuego();
                if (alClic != null) alClic();
            }, 54);
            anuncio = root.gameObject;
            if (CapaArrastre != null) CapaArrastre.SetAsLastSibling();
        }

        public void OcultarAnuncioMinijuego()
        {
            if (anuncio != null) Destroy(anuncio);
            anuncio = null;
        }

        // =====================================================================
        //  TARJETA 2D DEL MINIJUEGO (sale al abrir un cofre de minijuego)
        // =====================================================================
        UnityAction accionAhoraNo;

        public void MostrarTarjetaMinijuego(TipoMinijuego tipo, UnityAction alJugar, UnityAction alAhoraNo)
        {
            if (capaTarjeta != null) return;
            accionAhoraNo = alAhoraNo;
            AbrirVentana();
            grupoRaiz.alpha = 1f;

            var capa = KiboEstilo.Lleno("KIBO_TarjetaMinijuego", raiz);
            capaTarjeta = capa.gameObject;
            KiboEstilo.Oscurecer(capa, 0.65f);

            var arriba = KiboEstilo.Centro("Titulo", capa, new Vector2(0, 400), new Vector2(1100, 90));
            KiboEstilo.Texto(arriba, "¡ENCONTRASTE UN MINIJUEGO!", 66, KiboEstilo.Dorado, TextAnchor.MiddleCenter);

            var brillo = KiboEstilo.Centro("Brillo", capa, new Vector2(0, 50), new Vector2(760, 760));
            KiboEstilo.Imagen(brillo, "brillo", new Color(1f, 0.93f, 0.55f, 0.7f));
            brillo.gameObject.AddComponent<KiboPulso>().cantidad = 0.07f;

            var carta = KiboEstilo.Centro("Carta", capa, new Vector2(0, 50), new Vector2(420, 500));
            KiboEstilo.Imagen(carta, tipo == TipoMinijuego.Transmilenio ? "carta_transmilenio" : "carta_rompecabezas", Color.white);

            KiboEstilo.Boton(capa, "Btn_Jugar", "¡JUGAR!", new Vector2(0, -280), new Vector2(380, 105), () =>
            {
                CerrarTarjeta();
                if (alJugar != null) alJugar();
            }, 56);
            KiboEstilo.Boton(capa, "Btn_AhoraNo", "AHORA NO", new Vector2(0, -385), new Vector2(260, 72), () =>
            {
                CerrarTarjeta();
                if (alAhoraNo != null) alAhoraNo();
                Aviso("Puedes volver a abrir el cofre cuando quieras.", TipoCaneca.Verde);
            }, 34);

            KiboSonidos.Ganar();
            StartCoroutine(AnimarCarta(carta));
        }

        IEnumerator AnimarCarta(RectTransform carta)
        {
            // gira como una carta que se voltea y crece
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                if (carta == null) yield break;
                float k = t / 0.6f;
                float s = k + Mathf.Sin(k * Mathf.PI) * 0.25f;
                carta.localScale = new Vector3(Mathf.Abs(Mathf.Cos((1f - k) * Mathf.PI * 2f)) * s, s, 1f);
                yield return null;
            }
            if (carta == null) yield break;
            carta.localScale = Vector3.one;
            // flota suavemente
            while (carta != null)
            {
                carta.anchoredPosition = new Vector2(0, 50 + Mathf.Sin(Time.unscaledTime * 2f) * 8f);
                carta.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.5f) * 2f);
                yield return null;
            }
        }

        void CerrarTarjeta()
        {
            if (capaTarjeta == null) return;
            Destroy(capaTarjeta);
            capaTarjeta = null;
            CerrarVentanaSiNoHayOtra();
        }

        // =====================================================================
        //  ÍCONO QUE VUELA A LA BARRA DE INVENTARIO
        // =====================================================================
        public void VolarAlInventario(Sprite icono, Vector3 posMundo, System.Action alLlegar)
        {
            var cam = Camera.main;
            if (cam == null || icono == null || raiz == null || !canvas.enabled)
            {
                if (alLlegar != null) alLlegar();
                return;
            }
            Vector3 sp = cam.WorldToScreenPoint(posMundo);
            if (sp.z < 0f) sp = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);
            StartCoroutine(RutinaVolar(icono, sp, DestinoInventario(), alLlegar));
        }

        Vector2 DestinoInventario()
        {
            var inv = Object.FindAnyObjectByType<InventarioUI>();
            if (inv != null && inv.slotsBarra != null)
            {
                foreach (var s in inv.slotsBarra)
                    if (s != null && s.EstaVacio() && s.gameObject.activeInHierarchy)
                        return PosPantalla((RectTransform)s.transform);
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.08f);
        }

        static Vector2 PosPantalla(RectTransform rt)
        {
            Camera cam = null;
            var c = rt.GetComponentInParent<Canvas>();
            if (c != null)
            {
                c = c.rootCanvas;
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;
            }
            return RectTransformUtility.WorldToScreenPoint(cam, rt.position);
        }

        Vector2 ALocal(Vector2 pantalla)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, pantalla, null, out local);
            return local;
        }

        IEnumerator RutinaVolar(Sprite icono, Vector2 desde, Vector2 hasta, System.Action alLlegar)
        {
            var rt = KiboEstilo.Centro("ResiduoVolando", raiz, ALocal(desde), new Vector2(120, 120));
            var brillo = KiboEstilo.Centro("Brillo", rt, Vector2.zero, new Vector2(240, 240));
            KiboEstilo.Imagen(brillo, "brillo", new Color(1f, 0.95f, 0.6f, 0.85f));
            var ico = KiboEstilo.Lleno("Icono", rt);
            var img = ico.gameObject.AddComponent<Image>();
            img.sprite = icono;
            img.preserveAspect = true;
            img.raycastTarget = false;

            Vector2 a = ALocal(desde), b = ALocal(hasta);
            Vector2 control = (a + b) * 0.5f + Vector2.up * 260f;
            const float d = 0.6f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                float k = t / d;
                float e = k * k * (3f - 2f * k);
                Vector2 p = (1 - e) * (1 - e) * a + 2 * (1 - e) * e * control + e * e * b;
                rt.anchoredPosition = p;
                float s = k < 0.2f ? Mathf.Lerp(0.2f, 1.3f, k / 0.2f) : Mathf.Lerp(1.3f, 0.65f, (k - 0.2f) / 0.8f);
                rt.localScale = Vector3.one * s;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI) * 25f);
                yield return null;
            }
            Destroy(rt.gameObject);
            if (alLlegar != null) alLlegar();
        }

        // =====================================================================
        //  VENTANAS (pausan el juego)
        // =====================================================================
        bool HayVentana()
        {
            return capaClasificar != null || capaPausa != null || capaGanaste != null || capaAjustes != null || capaVideo != null || capaTarjeta != null;
        }

        void AbrirVentana()
        {
            if (!KiboJuego.UIAbierta)
            {
                escalaPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
                cursorPrevio = Cursor.lockState;
                cursorVisiblePrevio = Cursor.visible;
            }
            KiboJuego.UIAbierta = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (aviso != null && capaGanaste == null) aviso.SetAsLastSibling();
            if (CapaArrastre != null) CapaArrastre.SetAsLastSibling();
        }

        void CerrarVentanaSiNoHayOtra()
        {
            if (HayVentana()) return;
            KiboJuego.UIAbierta = false;
            Time.timeScale = escalaPrevia > 0f ? escalaPrevia : 1f;
            Cursor.lockState = cursorPrevio;
            Cursor.visible = cursorVisiblePrevio;
        }

        public void TeclaEscape()
        {
            if (capaAjustes != null) { CerrarAjustes(); return; }
            if (capaVideo != null) { TerminarVideo(); return; }
            if (capaTarjeta != null) { CerrarTarjeta(); if (accionAhoraNo != null) accionAhoraNo(); return; }
            if (capaClasificar != null) { CerrarClasificacion(); return; }
            if (capaPausa != null) { CerrarPausa(); return; }
            if (capaGanaste != null) return;
            if (Time.timeScale <= 0.0001f) return; // Game Over u otra ventana del juego
            if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego) return;
            AbrirPausa();
        }

        // =====================================================================
        //  VENTANA DE CLASIFICACIÓN
        // =====================================================================
        public void AbrirClasificacion(Caneca caneca)
        {
            if (capaClasificar != null || capaGanaste != null) return;
            canecaActual = caneca;
            verMaleta = false;
            AbrirVentana();
            KiboMusica.Poner("musica_clasificar");

            var capa = KiboEstilo.Lleno("KIBO_Clasificar", raiz);
            capaClasificar = capa.gameObject;
            KiboEstilo.Oscurecer(capa, 0.6f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -30), new Vector2(1500, 900), "CLASIFICA LOS RESIDUOS");
            KiboEstilo.BotonX(panel, new Vector2(-78, -78), 90, CerrarClasificacion);
            var c = KiboEstilo.Contenido(panel);

            KiboEstilo.Etiqueta(c, "Arrastra cada residuo a la caneca correcta", new Vector2(0, 330), new Vector2(1250, 52), 40, TextAnchor.MiddleCenter, false);
            var st = KiboEstilo.Centro("Stats", c, new Vector2(0, 288), new Vector2(1250, 40));
            txStats = KiboEstilo.Texto(st, TextoStats(), 30, Color.white, TextAnchor.MiddleCenter, false);

            objetivos.Clear();
            for (int i = 0; i < 3; i++)
            {
                var tipo = InfoCaneca.Todas[i];
                float x = (i - 1) * 400f;
                var zona = KiboEstilo.Centro("Zona_" + InfoCaneca.Titulo(tipo), c, new Vector2(x, 110), new Vector2(360, 400));

                if (caneca != null && caneca.tipo == tipo)
                {
                    var brillo = KiboEstilo.Centro("Brillo", zona, new Vector2(0, 25), new Vector2(380, 380));
                    KiboEstilo.Imagen(brillo, "brillo", new Color(1f, 0.93f, 0.6f, 0.75f));
                    brillo.gameObject.AddComponent<KiboPulso>();
                }

                var dib = KiboEstilo.Centro("Caneca", zona, new Vector2(0, 25), new Vector2(210, 266));
                KiboEstilo.Imagen(dib, InfoCaneca.Sprite(tipo), Color.white);

                var tabla = KiboEstilo.Centro("Categoria", c, new Vector2(x, -25), new Vector2(330, 66));
                KiboEstilo.Imagen(tabla, "boton", Color.white, true, 1.4f);
                var tt = KiboEstilo.TextoLleno(tabla, InfoCaneca.Categoria(tipo), 32, Color.white, TextAnchor.MiddleCenter);
                KiboEstilo.AutoTam(tt, 14, 32);
                ((RectTransform)tt.transform).offsetMin = new Vector2(14, 4);
                ((RectTransform)tt.transform).offsetMax = new Vector2(-14, -2);

                var ej = KiboEstilo.EtiquetaCafe(c, InfoCaneca.Ejemplos(tipo), new Vector2(x, -78), new Vector2(380, 50), 22);
                KiboEstilo.AutoTam(ej, 12, 22);

                objetivos.Add(new Objetivo { tipo = tipo, zona = zona, dibujo = dib });
            }

            // Pestañas
            btnTabBarra = KiboEstilo.Boton(c, "Tab_Barra", "BARRA", new Vector2(-470, -142), new Vector2(240, 60), () => { verMaleta = false; LlenarBandeja(); }, 32);
            btnTabMaleta = KiboEstilo.Boton(c, "Tab_Maleta", "MALETA", new Vector2(-215, -142), new Vector2(240, 60), () => { verMaleta = true; LlenarBandeja(); }, 32);

            // Bandeja con los residuos recolectados
            var fondoBandeja = KiboEstilo.Centro("Bandeja", c, new Vector2(0, -265), new Vector2(1250, 150));
            KiboEstilo.Imagen(fondoBandeja, "barra_fondo", new Color(1, 1, 1, 0.95f), true, 0.8f);
            bandeja = KiboEstilo.Lleno("Casillas", fondoBandeja);
            txVacio = KiboEstilo.Texto(KiboEstilo.Lleno("Vacio", fondoBandeja), "", 34, Color.white, TextAnchor.MiddleCenter, false);

            // Mensaje ¡BIEN! / ¡FALLASTE!
            var fb = KiboEstilo.Centro("Feedback", c, new Vector2(0, 140), new Vector2(980, 250));
            KiboEstilo.Imagen(fb, "panel", Color.white, true, 2.6f);
            grupoFeedback = fb.gameObject.AddComponent<CanvasGroup>();
            grupoFeedback.alpha = 0f;
            grupoFeedback.blocksRaycasts = false;
            var f1 = KiboEstilo.Centro("Titulo", fb, new Vector2(0, 45), new Vector2(900, 120));
            txFeedback = KiboEstilo.Texto(f1, "", 100, Color.white, TextAnchor.MiddleCenter);
            var f2 = KiboEstilo.Centro("Detalle", fb, new Vector2(0, -55), new Vector2(880, 100));
            txFeedbackSub = KiboEstilo.Texto(f2, "", 30, Color.white, TextAnchor.MiddleCenter, false);
            fb.gameObject.SetActive(false);

            LlenarBandeja();
            CapaArrastre.SetAsLastSibling();
            StartCoroutine(Aparecer(panel));
        }

        IEnumerator Aparecer(RectTransform panel)
        {
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                if (panel == null) yield break;
                float k = t / 0.22f;
                panel.localScale = Vector3.one * (0.85f + 0.15f * (1f - Mathf.Pow(1f - k, 3f)) + Mathf.Sin(k * Mathf.PI) * 0.03f);
                yield return null;
            }
            if (panel != null) panel.localScale = Vector3.one;
        }

        public void CerrarClasificacion()
        {
            if (capaClasificar == null) return;
            Destroy(capaClasificar);
            capaClasificar = null;
            KiboMusica.Volver();
            objetivos.Clear();
            txStats = null;
            foreach (Transform h in CapaArrastre) Destroy(h.gameObject);
            CerrarVentanaSiNoHayOtra();

            // Animación en las canecas 3D
            foreach (var tipo in celebrarAlCerrar)
            {
                var cn = CanecaDeTipo(tipo);
                if (cn != null) cn.Celebrar();
            }
            celebrarAlCerrar.Clear();
            canecaActual = null;
        }

        void LlenarBandeja()
        {
            if (bandeja == null) return;
            for (int i = bandeja.childCount - 1; i >= 0; i--) Destroy(bandeja.GetChild(i).gameObject);

            var inv = Object.FindAnyObjectByType<InventarioUI>();
            var full = KiboUtil.BuscarUno<InventarioFullUI>();

            int enBarra = 0, enMaleta = 0;
            if (inv != null && inv.slotsBarra != null) foreach (var s in inv.slotsBarra) if (s != null && !s.EstaVacio()) enBarra++;
            if (full != null && full.slotsFull != null) foreach (var s in full.slotsFull) if (s != null && !s.EstaVacio()) enMaleta += s.Cantidad;

            btnTabBarra.GetComponentInChildren<TMP_Text>().text = "BARRA (" + enBarra + ")";
            btnTabMaleta.GetComponentInChildren<TMP_Text>().text = "MALETA (" + enMaleta + ")";
            btnTabBarra.transform.localScale = Vector3.one * (verMaleta ? 0.92f : 1.05f);
            btnTabMaleta.transform.localScale = Vector3.one * (verMaleta ? 1.05f : 0.92f);
            btnTabBarra.targetGraphic.color = verMaleta ? new Color(0.8f, 0.75f, 0.7f) : Color.white;
            btnTabMaleta.targetGraphic.color = verMaleta ? Color.white : new Color(0.8f, 0.75f, 0.7f);

            var lista = new List<KeyValuePair<ItemData, object>>();
            if (!verMaleta && inv != null && inv.slotsBarra != null)
                foreach (var s in inv.slotsBarra) if (s != null && !s.EstaVacio()) lista.Add(new KeyValuePair<ItemData, object>(s.itemActual, s));
            if (verMaleta && full != null && full.slotsFull != null)
                foreach (var s in full.slotsFull) if (s != null && !s.EstaVacio()) lista.Add(new KeyValuePair<ItemData, object>(s.Item, s));

            txVacio.text = lista.Count == 0
                ? (verMaleta ? "Tu maleta está vacía." : "No tienes residuos en la barra. ¡Ve a recogerlos!")
                : "";

            const float tam = 112f, sep = 18f;
            float ancho = lista.Count * tam + Mathf.Max(0, lista.Count - 1) * sep;
            for (int i = 0; i < lista.Count; i++)
            {
                float x = -ancho / 2f + tam / 2f + i * (tam + sep);
                var casilla = KiboEstilo.Centro("Casilla", bandeja, new Vector2(x, 0), new Vector2(tam, tam));
                KiboEstilo.Imagen(casilla, "slot", Color.white, true, 1.5f);

                var ico = KiboEstilo.Centro("Residuo", casilla, Vector2.zero, new Vector2(tam - 18, tam - 18));
                var img = KiboEstilo.Imagen(ico, null, Color.white);
                img.sprite = lista[i].Key.icono;
                img.preserveAspect = true;
                img.raycastTarget = true;
                if (img.sprite == null) KiboEstilo.Texto(KiboEstilo.Lleno("Nombre", ico), KiboClasificador.Nombre(lista[i].Key), 18, Color.white, TextAnchor.MiddleCenter);

                var arr = ico.gameObject.AddComponent<KiboArrastre>();
                arr.item = lista[i].Key;
                arr.slotBarra = lista[i].Value as SlotBarraInventario;
                arr.slotMochila = lista[i].Value as SlotInventarioFull;

                if (arr.slotMochila != null && arr.slotMochila.Cantidad > 1)
                {
                    var n = KiboEstilo.Rect("Cantidad", casilla, Vector2.one, Vector2.one, Vector2.one, new Vector2(4, 4), new Vector2(60, 40));
                    KiboEstilo.Texto(n, "x" + arr.slotMochila.Cantidad, 30, Color.white, TextAnchor.MiddleRight);
                }
            }
        }

        public void ResaltarObjetivo(Vector2 pantalla)
        {
            foreach (var o in objetivos)
            {
                if (o.dibujo == null) continue;
                bool encima = RectTransformUtility.RectangleContainsScreenPoint(o.zona, pantalla, null);
                o.dibujo.localScale = Vector3.Lerp(o.dibujo.localScale, Vector3.one * (encima ? 1.12f : 1f), 0.35f);
            }
        }

        public void Soltar(KiboArrastre a, Vector2 pantalla)
        {
            Objetivo destino = null;
            foreach (var o in objetivos)
            {
                if (o.dibujo != null) o.dibujo.localScale = Vector3.one;
                if (destino == null && RectTransformUtility.RectangleContainsScreenPoint(o.zona, pantalla, null)) destino = o;
            }
            if (destino == null || KiboJuego.Instancia == null || a.item == null) { a.Volver(); return; }

            var item = a.item;
            var tipoCorrecto = KiboClasificador.Tipo(item);
            string nombre = KiboClasificador.Nombre(item);
            bool bien = KiboJuego.Instancia.Clasificar(item, destino.tipo);

            if (bien)
            {
                QuitarDelInventario(a);
                a.Entrar(destino.dibujo.position);
                StartCoroutine(Rebote(destino.dibujo));
                if (!celebrarAlCerrar.Contains(destino.tipo)) celebrarAlCerrar.Add(destino.tipo);
                Feedback(true, "¡BIEN!", nombre + " va en la caneca " + InfoCaneca.Pintado(destino.tipo) + ".");
                LlenarBandeja();
            }
            else
            {
                a.Volver();
                StartCoroutine(Sacudir(destino.dibujo));
                string expl = KiboClasificador.Explicacion(item);
                Feedback(false, "¡FALLASTE!", nombre + " no va en la caneca " + InfoCaneca.Pintado(destino.tipo) +
                    ".\nVa en la caneca " + InfoCaneca.Pintado(tipoCorrecto) + "." + (string.IsNullOrEmpty(expl) ? "" : " <size=80%>" + expl + "</size>"));
                var cn = CanecaDeTipo(destino.tipo);
                if (cn != null) cn.Sacudir();
            }
            if (txStats != null) txStats.text = TextoStats();

            if (KiboJuego.Instancia.Terminado) StartCoroutine(GanarDespues());
        }

        Caneca CanecaDeTipo(TipoCaneca t)
        {
            if (canecaActual != null && canecaActual.tipo == t) return canecaActual;
            foreach (var c in Caneca.Todas) if (c.tipo == t) return c;
            return null;
        }

        void QuitarDelInventario(KiboArrastre a)
        {
            if (a.slotMochila != null)
            {
                a.slotMochila.QuitarUno();
                return;
            }
            if (a.slotBarra != null)
            {
                a.slotBarra.VaciarSlot();
                // Junta los residuos que quedan al inicio de la barra
                var inv = Object.FindAnyObjectByType<InventarioUI>();
                if (inv != null && inv.slotsBarra != null)
                {
                    var quedan = new List<ItemData>();
                    foreach (var s in inv.slotsBarra) if (s != null && !s.EstaVacio()) quedan.Add(s.itemActual);
                    inv.VaciarBarraInventario();
                    foreach (var it in quedan) inv.AgregarItem(it);
                }
            }
        }

        void Feedback(bool bien, string titulo, string detalle)
        {
            if (grupoFeedback == null) return;
            txFeedback.text = titulo;
            txFeedback.color = bien ? KiboEstilo.VerdeBien : KiboEstilo.RojoMal;
            txFeedbackSub.text = detalle;
            grupoFeedback.gameObject.SetActive(true);
            grupoFeedback.transform.SetAsLastSibling();
            if (rutinaFeedback != null) StopCoroutine(rutinaFeedback);
            rutinaFeedback = StartCoroutine(RutinaFeedback(bien));
        }

        IEnumerator RutinaFeedback(bool bien)
        {
            var t = grupoFeedback.transform;
            for (float k = 0; k < 0.25f; k += Time.unscaledDeltaTime)
            {
                if (grupoFeedback == null) yield break;
                float p = k / 0.25f;
                grupoFeedback.alpha = p;
                t.localScale = Vector3.one * (0.6f + 0.4f * p + Mathf.Sin(p * Mathf.PI) * 0.15f);
                yield return null;
            }
            if (grupoFeedback == null) yield break;
            grupoFeedback.alpha = 1f;
            t.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(bien ? 1.3f : 2.6f);
            for (float k = 0; k < 0.3f; k += Time.unscaledDeltaTime)
            {
                if (grupoFeedback == null) yield break;
                grupoFeedback.alpha = 1f - k / 0.3f;
                yield return null;
            }
            if (grupoFeedback != null) grupoFeedback.gameObject.SetActive(false);
            rutinaFeedback = null;
        }

        IEnumerator Rebote(RectTransform r)
        {
            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                if (r == null) yield break;
                float k = t / 0.45f;
                float s = 1f + Mathf.Sin(k * Mathf.PI * 2f) * 0.15f * (1f - k);
                r.localScale = new Vector3(2f - s, s, 1f);
                yield return null;
            }
            if (r != null) r.localScale = Vector3.one;
        }

        IEnumerator Sacudir(RectTransform r)
        {
            if (r == null) yield break;
            Vector2 p0 = r.anchoredPosition;
            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                if (r == null) yield break;
                float k = t / 0.45f;
                r.anchoredPosition = p0 + new Vector2(Mathf.Sin(k * Mathf.PI * 8f) * 18f * (1f - k), 0);
                r.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 8f) * 6f * (1f - k));
                yield return null;
            }
            if (r == null) yield break;
            r.anchoredPosition = p0;
            r.localRotation = Quaternion.identity;
        }

        IEnumerator GanarDespues()
        {
            yield return new WaitForSecondsRealtime(1.4f);
            CerrarClasificacion();
            yield return new WaitForSecondsRealtime(0.3f);
            MostrarGanaste();
        }

        // =====================================================================
        //  ¡GANASTE!
        // =====================================================================
        public void MostrarGanaste()
        {
            if (capaGanaste != null) return;
            var j = KiboJuego.Instancia;
            if (j == null) return;
            AbrirVentana();
            KiboMusica.Detener();
            KiboMusica.Stinger("musica_ganaste", "musica_menu");

            var capa = KiboEstilo.Lleno("KIBO_Ganaste", raiz);
            capaGanaste = capa.gameObject;
            KiboEstilo.Oscurecer(capa, 0.7f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -25), new Vector2(1000, 880), "NIVEL 1");
            var c = KiboEstilo.Contenido(panel);

            var titulo = KiboEstilo.Centro("Ganaste", c, new Vector2(0, 292), new Vector2(820, 130));
            var tg = KiboEstilo.Texto(titulo, "¡GANASTE!", 116, KiboEstilo.Dorado, TextAnchor.MiddleCenter);

            int estrellas = j.Estrellas;
            var estrellasRt = new List<RectTransform>();
            for (int i = 0; i < 3; i++)
            {
                bool central = i == 1;
                var e = KiboEstilo.Centro("Estrella" + i, c, new Vector2((i - 1) * 170f, central ? 168 : 152), central ? new Vector2(160, 160) : new Vector2(130, 130));
                KiboEstilo.Imagen(e, i < estrellas ? "estrella" : "estrella_vacia", Color.white);
                e.localScale = Vector3.zero;
                estrellasRt.Add(e);
            }

            int mejor = KiboGuardado.MejorPuntaje;
            KiboEstilo.Etiqueta(c, "PUNTUACIÓN: <color=#FFE066><size=140%>" + j.Puntuacion + "</size></color>", new Vector2(0, 32), new Vector2(820, 80), 46, TextAnchor.MiddleCenter);
            KiboEstilo.EtiquetaCafe(c, "MEJOR PUNTUACIÓN: " + Mathf.Max(mejor, j.Puntuacion), new Vector2(0, -24), new Vector2(820, 40), 30);
            KiboEstilo.Etiqueta(c, "RESIDUOS RECOLECTADOS: " + j.recolectados, new Vector2(0, -76), new Vector2(820, 50), 38, TextAnchor.MiddleCenter, false);
            KiboEstilo.Etiqueta(c, "BIEN CLASIFICADOS: <color=#9CFF9C>" + j.aciertos + "</color>", new Vector2(0, -124), new Vector2(820, 50), 38, TextAnchor.MiddleCenter, false);
            KiboEstilo.Etiqueta(c, "FALLOS: <color=#FFA08C>" + j.fallos + "</color>", new Vector2(0, -172), new Vector2(820, 50), 38, TextAnchor.MiddleCenter, false);

            KiboEstilo.Boton(c, "Btn_Reintentar", "REINTENTAR", new Vector2(-205, -282), new Vector2(380, 100), Reintentar, 46);
            KiboEstilo.Boton(c, "Btn_Menu", "VOLVER AL MENÚ", new Vector2(205, -282), new Vector2(380, 100), VolverAlMenu, 42);

            StartCoroutine(AnimarGanaste(panel, tg.rectTransform, estrellasRt, estrellas));
        }

        IEnumerator AnimarGanaste(RectTransform panel, RectTransform titulo, List<RectTransform> estrellas, int llenas)
        {
            yield return Aparecer(panel);
            for (int i = 0; i < estrellas.Count; i++)
            {
                var e = estrellas[i];
                for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    float k = t / 0.3f;
                    e.localScale = Vector3.one * (k + Mathf.Sin(k * Mathf.PI) * 0.35f);
                    e.localRotation = Quaternion.Euler(0, 0, (1f - k) * 40f);
                    yield return null;
                }
                e.localScale = Vector3.one;
                e.localRotation = Quaternion.identity;
                if (i < llenas) KiboSonidos.Recoger();
            }
            // El título late suavemente
            while (titulo != null)
            {
                titulo.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.04f);
                yield return null;
            }
        }

        public void Reintentar()
        {
            PrepararCambioDeEscena();
            KiboTransicion.Cargar(gameObject.scene.name);
        }

        public void VolverAlMenu()
        {
            PrepararCambioDeEscena();
            KiboTransicion.Cargar("Main_inicio");
        }

        void PrepararCambioDeEscena()
        {
            KiboJuego.UIAbierta = false;
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.PrepararNuevaPartida();
        }

        // =====================================================================
        //  PAUSA
        // =====================================================================
        public void AbrirPausa()
        {
            if (capaPausa != null || capaGanaste != null) return;
            AbrirVentana();
            var capa = KiboEstilo.Lleno("KIBO_Pausa", raiz);
            capaPausa = capa.gameObject;
            KiboEstilo.Oscurecer(capa, 0.6f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -30), new Vector2(760, 900), "PAUSA");
            var c = KiboEstilo.Contenido(panel);
            KiboEstilo.BotonX(panel, new Vector2(-70, -70), 80, CerrarPausa);

            var acciones = new List<KeyValuePair<string, UnityAction>>
            {
                new KeyValuePair<string, UnityAction>("CONTINUAR", CerrarPausa),
                new KeyValuePair<string, UnityAction>("AJUSTES", AbrirAjustes),
                new KeyValuePair<string, UnityAction>("INSTRUCCIONES", AbrirInstrucciones),
                new KeyValuePair<string, UnityAction>("GUARDAR PARTIDA", () => { if (KiboJuego.Instancia != null) KiboJuego.Instancia.Guardar(); }),
                new KeyValuePair<string, UnityAction>("MENÚ PRINCIPAL", VolverAlMenu),
                new KeyValuePair<string, UnityAction>("SALIR DEL JUEGO", SalirDelJuego),
            };
            for (int i = 0; i < acciones.Count; i++)
                KiboEstilo.Boton(c, "Btn_" + i, acciones[i].Key, new Vector2(0, 272 - i * 108), new Vector2(470, 92), acciones[i].Value, 44);
            StartCoroutine(Aparecer(panel));
        }

        public void CerrarPausa()
        {
            if (capaPausa == null) return;
            Destroy(capaPausa);
            capaPausa = null;
            CerrarVentanaSiNoHayOtra();
        }

        public void AbrirAjustes()
        {
            if (capaAjustes != null) return;
            AbrirVentana();
            if (capaPausa != null) capaPausa.SetActive(false);
            capaAjustes = KiboAjustes.CrearPanel(raiz, () =>
            {
                capaAjustes = null;
                if (capaPausa != null) capaPausa.SetActive(true);
                CerrarVentanaSiNoHayOtra();
            });
        }

        void CerrarAjustes()
        {
            if (capaAjustes == null) return;
            Destroy(capaAjustes);
            capaAjustes = null;
            PlayerPrefs.Save();
            if (capaPausa != null) capaPausa.SetActive(true);
            CerrarVentanaSiNoHayOtra();
        }

        void AbrirInstrucciones()
        {
            CerrarPausa();
            GameObject p = KiboUtil.BuscarPorNombre("Panel_Instrucciones");
            if (p != null)
            {
                p.SetActive(true);
                var rt = p.transform as RectTransform;
                if (rt != null) rt.SetAsLastSibling();
            }
            else Aviso("Recoge residuos con clic, llévalos a una caneca y arrástralos a la correcta.", TipoCaneca.Blanca);
        }

        void SalirDelJuego()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =====================================================================
        //  HISTORIETA (video)
        // =====================================================================
        public void VerHistorieta()
        {
            if (capaVideo != null) return;
            if (historieta == null)
            {
                Aviso("No hay video de historieta asignado. Usa Kibo el Origen > Crear Todo.", TipoCaneca.Blanca);
                return;
            }
            AbrirVentana();
            var capa = KiboEstilo.Lleno("KIBO_Video", raiz);
            capaVideo = capa.gameObject;
            var fondo = capa.gameObject.AddComponent<Image>();
            fondo.color = Color.black;
            fondo.raycastTarget = true;

            int w = historieta.width > 0 ? (int)historieta.width : 1920;
            int h = historieta.height > 0 ? (int)historieta.height : 1080;
            var tex = new RenderTexture(w, h, 0);
            var pantalla = KiboEstilo.Lleno("Pantalla", capa);
            var raw = pantalla.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            var ajuste = pantalla.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ajuste.aspectRatio = w / (float)h;

            var vp = capa.gameObject.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = tex;
            vp.clip = historieta;
            vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;
            vp.loopPointReached += v => TerminarVideo();
            vp.errorReceived += (v, m) => TerminarVideo();
            vp.Play();

            var omitir = KiboEstilo.Boton(capa, "Btn_Omitir", "OMITIR  >>", Vector2.zero, new Vector2(300, 95), TerminarVideo, 44);
            var ort = (RectTransform)omitir.transform;
            ort.anchorMin = ort.anchorMax = ort.pivot = new Vector2(1, 0);
            ort.anchoredPosition = new Vector2(-50, 45);
            omitir.gameObject.SetActive(false);
            StartCoroutine(MostrarLuego(omitir.gameObject, 3f));
        }

        IEnumerator MostrarLuego(GameObject go, float s)
        {
            yield return new WaitForSecondsRealtime(s);
            if (go != null) go.SetActive(true);
        }

        void TerminarVideo()
        {
            if (capaVideo == null) return;
            var vp = capaVideo.GetComponent<VideoPlayer>();
            RenderTexture rt = null;
            if (vp != null) { rt = vp.targetTexture; vp.Stop(); }
            Destroy(capaVideo);
            capaVideo = null;
            if (rt != null) rt.Release();
            CerrarVentanaSiNoHayOtra();
        }

        // =====================================================================
        //  Botones del juego que no hacían nada
        // =====================================================================
        void ConectarBotonesDelJuego()
        {
            Conectar("Btn_GuardarPartida", () => { if (KiboJuego.Instancia != null) KiboJuego.Instancia.Guardar(); }, false);
            Conectar("Btn_Historieta", VerHistorieta, false);
            Conectar("Btn_Libro", () => { var inv = Object.FindAnyObjectByType<InventarioUI>(); if (inv != null) inv.AbrirLibro(); }, false);
            // Este botón cerraba la configuración en vez del libro
            Conectar("Btn_CerrarLibro", () => { var inv = Object.FindAnyObjectByType<InventarioUI>(); if (inv != null) inv.CerrarLibro(); }, true);
        }

        static void Conectar(string nombre, UnityAction accion, bool reemplazar)
        {
            var go = KiboUtil.BuscarPorNombre(nombre);
            if (go == null) return;
            var b = go.GetComponent<Button>();
            if (b == null) return;
            int n = b.onClick.GetPersistentEventCount();
            if (reemplazar)
            {
                for (int i = 0; i < n; i++) b.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                b.onClick.AddListener(accion);
            }
            else
            {
                bool tieneAlgo = false;
                for (int i = 0; i < n; i++) if (b.onClick.GetPersistentTarget(i) != null) tieneAlgo = true;
                if (!tieneAlgo) b.onClick.AddListener(accion);
            }
        }
    }
}
