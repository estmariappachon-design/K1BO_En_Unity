using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KiboElOrigen
{
    // Minijuego 2D: dentro de un TransMilenio van cayendo residuos por una torre.
    // Con las flechas ← → se mueve el residuo para que caiga en la caneca correcta. ↓ lo baja más rápido.
    // Empieza lento y cada vez va más rápido.
    public class KiboMinijuegoTransmilenio : MonoBehaviour
    {
        public static bool Activo { get; private set; }

        [Header("Reglas")]
        public int metaAciertos = 10;
        public int vidas = 3;

        [Header("Velocidad (píxeles por segundo)")]
        public float velocidadInicial = 140f;
        public float velocidadMaxima = 620f;
        public float aumentoPorAcierto = 26f;
        public float aumentoPorSegundo = 3f;

        const float ANCHO_CARRIL = 330f;
        const float Y_INICIO = 300f;
        const float Y_BOCA = -150f;   // boca de las canecas

        Canvas canvas;
        RectTransform raiz, torre, residuo, capaJuego;
        Image residuoImg;
        ItemData itemActual;
        readonly RectTransform[] canecas = new RectTransform[3];
        readonly List<Image> corazones = new List<Image>();
        TMP_Text txPuntos, txVelocidad, txMensaje, txNombre;
        CanvasGroup grupoMensaje;
        Coroutine rutinaMensaje;

        List<ItemData> items = new List<ItemData>();
        int carril = 1;
        float y, velocidadBase;
        int aciertos, fallos;
        bool jugando, resolviendo, terminado, gano;
        float xVisual;

        // ------------------------------------------------------------------
        public static void Abrir()
        {
            if (Activo) return;
            new GameObject("KIBO_MinijuegoTransmilenio").AddComponent<KiboMinijuegoTransmilenio>();
        }

        void Awake()
        {
            Activo = true;
            if (GameManager.Instance != null) GameManager.Instance.EntrarMinijuego();
            KiboMusica.Poner("musica_transmilenio");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            BuscarResiduos();
            Construir();
            MostrarInicio();
        }

        void OnDestroy()
        {
            Activo = false;
        }

        void BuscarResiduos()
        {
            var vistos = new HashSet<ItemData>();
            foreach (var r in KiboUtil.Buscar<ItemRecolectable>())
                if (r.itemData != null && r.itemData.icono != null && vistos.Add(r.itemData)) items.Add(r.itemData);
        }

        // ------------------------------------------------------------------
        //  Construcción de la pantalla
        // ------------------------------------------------------------------
        void Construir()
        {
            canvas = KiboEstilo.CrearCanvas("Canvas", 400, transform);
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            raiz = (RectTransform)canvas.transform;

            // Fondo: interior del bus
            var fondo = KiboEstilo.Lleno("Fondo", raiz);
            var imgFondo = fondo.gameObject.AddComponent<Image>();
            imgFondo.sprite = KiboEstilo.Dibujo("tm_fondo");
            imgFondo.color = imgFondo.sprite != null ? Color.white : new Color(0.75f, 0.1f, 0.12f);
            imgFondo.raycastTarget = true; // bloquea clics al mundo

            // Torre por donde caen los residuos
            torre = KiboEstilo.Centro("Torre", raiz, new Vector2(0, -40), new Vector2(1080, 900));
            var imgTorre = KiboEstilo.Imagen(torre, "redondo", new Color(0.08f, 0.04f, 0.03f, 0.55f), true, 0.6f);
            var borde = torre.gameObject.AddComponent<Outline>();
            borde.effectColor = KiboEstilo.Cafe;
            borde.effectDistance = new Vector2(6, -6);
            imgTorre.raycastTarget = false;
            for (int i = -1; i <= 1; i += 2)
            {
                var linea = KiboEstilo.Centro("Separador", torre, new Vector2(i * ANCHO_CARRIL / 2f, 40), new Vector2(6, 780));
                KiboEstilo.Imagen(linea, null, new Color(1f, 1f, 1f, 0.18f));
            }

            capaJuego = KiboEstilo.Lleno("Juego", torre);

            // Canecas abajo
            for (int i = 0; i < 3; i++)
            {
                var tipo = InfoCaneca.Todas[i];
                var c = KiboEstilo.Centro("Caneca_" + InfoCaneca.Titulo(tipo), torre, new Vector2((i - 1) * ANCHO_CARRIL, -250), new Vector2(180, 228));
                KiboEstilo.Imagen(c, InfoCaneca.Sprite(tipo), Color.white);
                canecas[i] = c;
                var et = KiboEstilo.Centro("Etiqueta", torre, new Vector2((i - 1) * ANCHO_CARRIL, -395), new Vector2(310, 54));
                KiboEstilo.Imagen(et, "boton", Color.white, true, 1.6f);
                var tx = KiboEstilo.TextoLleno(et, InfoCaneca.Categoria(tipo), 26, Color.white, TextAnchor.MiddleCenter);
                KiboEstilo.AutoTam(tx, 12, 26);
            }

            // Residuo que cae
            residuo = KiboEstilo.Centro("Residuo", capaJuego, new Vector2(0, Y_INICIO), new Vector2(130, 130));
            var brillo = KiboEstilo.Centro("Brillo", residuo, Vector2.zero, new Vector2(230, 230));
            KiboEstilo.Imagen(brillo, "brillo", new Color(1f, 0.95f, 0.65f, 0.9f));
            var ico = KiboEstilo.Lleno("Icono", residuo);
            residuoImg = ico.gameObject.AddComponent<Image>();
            residuoImg.preserveAspect = true;
            residuoImg.raycastTarget = false;
            var nom = KiboEstilo.Rect("Nombre", residuo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(360, 44));
            txNombre = KiboEstilo.Texto(nom, "", 28, Color.white, TextAnchor.MiddleCenter, false);
            residuo.gameObject.SetActive(false);

            // Título
            var titulo = KiboEstilo.Rect("Titulo", raiz, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(720, 100));
            KiboEstilo.Imagen(titulo, "ovalo", Color.white).preserveAspect = false;
            var tt = KiboEstilo.TextoLleno(titulo, "RECICLA EN EL TRANSMILENIO", 44, Color.white, TextAnchor.MiddleCenter);
            KiboEstilo.AutoTam(tt, 18, 44);
            ((RectTransform)tt.transform).offsetMin = new Vector2(50, 10);
            ((RectTransform)tt.transform).offsetMax = new Vector2(-50, -8);

            // Vidas (izquierda)
            var vidasRt = KiboEstilo.Rect("Vidas", raiz, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -20), new Vector2(330, 110));
            KiboEstilo.Imagen(vidasRt, "boton", Color.white, true, 1.3f);
            for (int i = 0; i < vidas; i++)
            {
                var h = KiboEstilo.Centro("Corazon", vidasRt, new Vector2(-95 + i * 95, 0), new Vector2(80, 80));
                corazones.Add(KiboEstilo.Imagen(h, "corazon", Color.white));
            }

            // Puntos y velocidad (derecha)
            var info = KiboEstilo.Rect("Info", raiz, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-130, -20), new Vector2(360, 110));
            KiboEstilo.Imagen(info, "boton", Color.white, true, 1.3f);
            var p1 = KiboEstilo.Centro("Puntos", info, new Vector2(0, 18), new Vector2(320, 50));
            txPuntos = KiboEstilo.Texto(p1, "", 38, Color.white, TextAnchor.MiddleCenter);
            var p2 = KiboEstilo.Centro("Velocidad", info, new Vector2(0, -24), new Vector2(320, 40));
            txVelocidad = KiboEstilo.Texto(p2, "", 26, Color.white, TextAnchor.MiddleCenter, false);

            // Salir
            KiboEstilo.BotonX(raiz, new Vector2(-55, -60), 85, Salir);

            // Flechas en pantalla (también se puede con el teclado)
            BotonFlecha("Izquierda", new Vector2(-760, -330), 90f, () => Mover(-1));
            BotonFlecha("Derecha", new Vector2(760, -330), -90f, () => Mover(1));

            // Mensaje en el centro
            var m = KiboEstilo.Centro("Mensaje", raiz, new Vector2(0, 80), new Vector2(1000, 200));
            grupoMensaje = m.gameObject.AddComponent<CanvasGroup>();
            grupoMensaje.blocksRaycasts = false;
            grupoMensaje.alpha = 0f;
            txMensaje = KiboEstilo.Texto(m, "", 80, Color.white, TextAnchor.MiddleCenter);

            ActualizarHUD();
        }

        void BotonFlecha(string nombre, Vector2 pos, float giro, UnityEngine.Events.UnityAction accion)
        {
            var rt = KiboEstilo.Centro("Btn_" + nombre, raiz, pos, new Vector2(170, 170));
            var fondo = KiboEstilo.Imagen(rt, "circulo", new Color(1f, 0.85f, 0.6f, 0.9f));
            fondo.raycastTarget = true;
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = KiboEstilo.Cafe;
            o.effectDistance = new Vector2(5, -5);
            var fl = KiboEstilo.Centro("Flecha", rt, Vector2.zero, new Vector2(100, 115));
            KiboEstilo.Imagen(fl, "flecha", Color.white);
            fl.localRotation = Quaternion.Euler(0, 0, giro);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = fondo;
            b.onClick.AddListener(accion);
            rt.gameObject.AddComponent<KiboBotonEfecto>();
        }

        // ------------------------------------------------------------------
        //  Pantallas de inicio / final
        // ------------------------------------------------------------------
        void MostrarInicio()
        {
            var capa = KiboEstilo.Lleno("Inicio", raiz);
            KiboEstilo.Oscurecer(capa, 0.55f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -30), new Vector2(980, 700), "MINIJUEGO");
            var c = KiboEstilo.Contenido(panel);
            var t1 = KiboEstilo.Centro("Texto", c, new Vector2(0, 110), new Vector2(760, 300));
            KiboEstilo.Texto(t1,
                "¡Los residuos van cayendo dentro del TransMilenio!\n\n" +
                "Usa las flechas <color=#FFE066>← →</color> (izquierda y derecha) para moverlos a la caneca correcta.\n" +
                "Con la flecha <color=#FFE066>↓</color> (abajo) caen más rápido.\n\n" +
                "Clasifica bien <color=#FFE066>" + metaAciertos + "</color> para ganar. Si fallas " + vidas + " veces, pierdes.",
                34, Color.white, TextAnchor.MiddleCenter, false);
            if (items.Count == 0)
            {
                KiboEstilo.Etiqueta(c, "No encontré residuos con imagen en el nivel.", new Vector2(0, -120), new Vector2(760, 60), 30, TextAnchor.MiddleCenter, false);
                KiboEstilo.Boton(c, "Btn_Salir", "VOLVER", new Vector2(0, -230), new Vector2(320, 92), Salir, 46);
                return;
            }
            KiboEstilo.Boton(c, "Btn_Jugar", "¡JUGAR!", new Vector2(0, -210), new Vector2(340, 100), () =>
            {
                Destroy(capa.gameObject);
                StartCoroutine(CuentaRegresiva());
            }, 54);
        }

        IEnumerator CuentaRegresiva()
        {
            for (int i = 3; i >= 1; i--)
            {
                Mensaje(i.ToString(), Color.white, 0.8f);
                KiboSonidos.Clic();
                yield return new WaitForSecondsRealtime(0.8f);
            }
            Mensaje("¡YA!", KiboEstilo.Dorado, 0.7f);
            velocidadBase = velocidadInicial;
            jugando = true;
            NuevoResiduo();
        }

        void Terminar(bool ganaste)
        {
            jugando = false;
            terminado = true;
            gano = ganaste;
            residuo.gameObject.SetActive(false);
            KiboMusica.Detener();
            KiboMusica.Stinger(ganaste ? "musica_ganaste" : "musica_perdiste");

            var capa = KiboEstilo.Lleno("Final", raiz);
            KiboEstilo.Oscurecer(capa, 0.6f);
            var panel = KiboEstilo.Panel(capa, "Panel", new Vector2(0, -30), new Vector2(900, 640), ganaste ? "¡GANASTE!" : "¡PERDISTE!");
            var c = KiboEstilo.Contenido(panel);
            var t = KiboEstilo.Centro("Texto", c, new Vector2(0, 80), new Vector2(700, 260));
            KiboEstilo.Texto(t, ganaste
                ? "¡Muy bien! Clasificaste <color=#9CFF9C>" + aciertos + "</color> residuos dentro del TransMilenio.\nGanaste la insignia del minijuego."
                : "Fallaste " + fallos + " veces.\nPierdes una vida, ¡inténtalo en otro cofre!",
                36, Color.white, TextAnchor.MiddleCenter, false);
            KiboEstilo.Boton(c, "Btn_Volver", "VOLVER AL JUEGO", new Vector2(0, -190), new Vector2(420, 100), Cerrar, 44);
        }

        void Salir()
        {
            // salir antes de terminar no quita vidas
            if (!terminado) gano = false;
            Cerrar();
        }

        void Cerrar()
        {
            bool perdio = terminado && !gano;
            KiboMusica.Volver();
            if (GameManager.Instance != null)
            {
                if (gano) GameManager.Instance.GanarRompecabezas();
                GameManager.Instance.SalirMinijuego();
                if (perdio) GameManager.Instance.PerderVida();
            }
            if (gano)
            {
                var insignia = Object.FindAnyObjectByType<InsigniaUI>();
                if (insignia != null && insignia.insignia != null) insignia.MostrarInsignia();
            }
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------
        //  Juego
        // ------------------------------------------------------------------
        void NuevoResiduo()
        {
            if (items.Count == 0) { Terminar(true); return; }
            itemActual = items[Random.Range(0, items.Count)];
            residuoImg.sprite = itemActual.icono;
            txNombre.text = KiboClasificador.Nombre(itemActual);
            carril = 1;
            xVisual = 0f;
            y = Y_INICIO;
            residuo.anchoredPosition = new Vector2(0, y);
            residuo.localScale = Vector3.one;
            residuo.localRotation = Quaternion.identity;
            residuo.gameObject.SetActive(true);
            resolviendo = false;
        }

        void Mover(int dir)
        {
            if (!jugando || resolviendo) return;
            int nuevo = Mathf.Clamp(carril + dir, 0, 2);
            if (nuevo != carril) { carril = nuevo; KiboSonidos.Clic(); }
        }

        void Update()
        {
            if (!jugando || resolviendo) return;
            float dt = Time.unscaledDeltaTime;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Mover(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Mover(1);
            if (Input.GetKeyDown(KeyCode.Escape)) { Salir(); return; }

            velocidadBase = Mathf.Min(velocidadMaxima, velocidadBase + aumentoPorSegundo * dt);
            bool rapido = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            y -= velocidadBase * (rapido ? 3.2f : 1f) * dt;

            float xObjetivo = (carril - 1) * ANCHO_CARRIL;
            xVisual = Mathf.Lerp(xVisual, xObjetivo, 1f - Mathf.Exp(-18f * dt));
            residuo.anchoredPosition = new Vector2(xVisual, y);
            residuo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 3f) * 8f);

            if (y <= Y_BOCA) StartCoroutine(Resolver());
            ActualizarHUD();
        }

        IEnumerator Resolver()
        {
            resolviendo = true;
            var tipoCaneca = InfoCaneca.Todas[carril];
            var correcto = KiboClasificador.Tipo(itemActual);
            bool bien = correcto == tipoCaneca;

            // entra a la caneca
            Vector2 desde = residuo.anchoredPosition;
            Vector2 hasta = new Vector2((carril - 1) * ANCHO_CARRIL, -230);
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.22f;
                residuo.anchoredPosition = Vector2.Lerp(desde, hasta, k);
                residuo.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
                yield return null;
            }
            residuo.gameObject.SetActive(false);

            if (bien)
            {
                aciertos++;
                velocidadBase = Mathf.Min(velocidadMaxima, velocidadBase + aumentoPorAcierto);
                KiboSonidos.Bien();
                Mensaje("¡BIEN!", KiboEstilo.VerdeBien, 0.7f);
                StartCoroutine(Rebote(canecas[carril]));
            }
            else
            {
                fallos++;
                KiboSonidos.Mal();
                Mensaje("¡FALLASTE!\n<size=45%>" + KiboClasificador.Nombre(itemActual) + " va en la caneca " + InfoCaneca.Pintado(correcto) + "</size>", KiboEstilo.RojoMal, 1.4f);
                StartCoroutine(Sacudir(canecas[carril]));
            }
            ActualizarHUD();

            if (aciertos >= metaAciertos) { yield return new WaitForSecondsRealtime(0.6f); Terminar(true); yield break; }
            if (fallos >= vidas) { yield return new WaitForSecondsRealtime(0.8f); Terminar(false); yield break; }

            yield return new WaitForSecondsRealtime(bien ? 0.35f : 1.0f);
            NuevoResiduo();
        }

        void ActualizarHUD()
        {
            if (txPuntos != null) txPuntos.text = "BIEN: <color=#FFE066>" + aciertos + "</color> / " + metaAciertos;
            if (txVelocidad != null)
            {
                float v = velocidadBase > 0 ? velocidadBase / velocidadInicial : 1f;
                if (jugando) KiboMusica.Tono(1f + (v - 1f) * 0.12f); // la música se acelera con el juego
                txVelocidad.text = "VELOCIDAD x" + v.ToString("0.0");
            }
            for (int i = 0; i < corazones.Count; i++)
                corazones[i].sprite = KiboEstilo.Dibujo(i < vidas - fallos ? "corazon" : "corazon_vacio");
        }

        void Mensaje(string texto, Color color, float duracion)
        {
            txMensaje.text = texto;
            txMensaje.color = color;
            if (rutinaMensaje != null) StopCoroutine(rutinaMensaje);
            rutinaMensaje = StartCoroutine(RutinaMensaje(duracion));
        }

        IEnumerator RutinaMensaje(float duracion)
        {
            var t = grupoMensaje.transform;
            for (float k = 0; k < 0.18f; k += Time.unscaledDeltaTime)
            {
                float p = k / 0.18f;
                grupoMensaje.alpha = p;
                t.localScale = Vector3.one * (0.6f + 0.4f * p + Mathf.Sin(p * Mathf.PI) * 0.2f);
                yield return null;
            }
            grupoMensaje.alpha = 1f;
            t.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(duracion);
            for (float k = 0; k < 0.25f; k += Time.unscaledDeltaTime)
            {
                grupoMensaje.alpha = 1f - k / 0.25f;
                yield return null;
            }
            grupoMensaje.alpha = 0f;
        }

        IEnumerator Rebote(RectTransform r)
        {
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.4f;
                float s = 1f + Mathf.Sin(k * Mathf.PI * 2f) * 0.15f * (1f - k);
                r.localScale = new Vector3(2f - s, s, 1f);
                yield return null;
            }
            r.localScale = Vector3.one;
        }

        IEnumerator Sacudir(RectTransform r)
        {
            Vector2 p0 = r.anchoredPosition;
            for (float t = 0; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.45f;
                r.anchoredPosition = p0 + new Vector2(Mathf.Sin(k * Mathf.PI * 8f) * 16f * (1f - k), 0);
                yield return null;
            }
            r.anchoredPosition = p0;
        }
    }
}
