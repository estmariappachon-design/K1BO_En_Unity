using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KiboElOrigen
{
    // Lógica del nivel: cuenta los residuos, revisa la clasificación en las canecas y decide cuándo se gana.
    public class KiboJuego : MonoBehaviour
    {
        public static KiboJuego Instancia;

        [Header("Meta del nivel")]
        [Tooltip("Porcentaje de residuos que hay que clasificar bien para ganar (0.5 = 50%)")]
        [Range(0.05f, 1f)] public float porcentajeMeta = 0.5f;

        [Header("Puntuación")]
        public int puntosPorAcierto = 100;
        public int puntosMenosPorFallo = 25;

        [Header("Canecas")]
        [Tooltip("Distancia máxima (metros) para poder usar una caneca con clic")]
        public float distanciaCaneca = 9f;
        [Tooltip("Si en la escena no hay canecas, se crean 3 cerca del jugador al iniciar")]
        public bool crearCanecasSiNoHay = true;

        [Tooltip("Jugador (si lo dejas vacío se busca solo)")]
        public Transform jugador;

        // ---- Estado ----
        [HideInInspector] public int aciertos, fallos, recolectados, total, meta;
        public bool Terminado { get; private set; }

        // Se pone en true mientras hay una ventana abierta (clasificar, pausa, ganaste...)
        public static bool UIAbierta;

        public static bool EntradaBloqueada
        {
            get
            {
                if (UIAbierta) return true;
                if (Instancia != null && Instancia.Terminado) return true;
                if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego) return true;
                return Time.timeScale <= 0.0001f;
            }
        }

        public int Puntuacion { get { return Mathf.Max(0, aciertos * puntosPorAcierto - fallos * puntosMenosPorFallo); } }

        public int Estrellas
        {
            get
            {
                int intentos = aciertos + fallos;
                float p = intentos > 0 ? aciertos / (float)intentos : 0f;
                if (p >= 0.9f) return 3;
                if (p >= 0.7f) return 2;
                return 1;
            }
        }

        readonly Dictionary<GameObject, string> idResiduo = new Dictionary<GameObject, string>();
        readonly Dictionary<string, ItemData> itemsPorNombre = new Dictionary<string, ItemData>();
        readonly List<string> recogidos = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstaticos() { UIAbierta = false; Instancia = null; }

        void Awake()
        {
            Instancia = this;
            UIAbierta = false;
        }

        void OnDestroy()
        {
            if (Instancia == this) { Instancia = null; UIAbierta = false; }
        }

        void Start()
        {
            if (jugador == null) jugador = KiboUtil.Jugador();
            ContarResiduos();
            if (crearCanecasSiNoHay && Caneca.Todas.Count == 0) CrearCanecasRuntime();
            if (KiboUI.Instancia == null) gameObject.AddComponent<KiboUI>();
            KiboMusica.Volver(); // en el nivel suena la música ambiente

            // Cielo con nubes/estrellas y cámara nueva (por si no se usó "Crear Todo")
            if (Object.FindAnyObjectByType<KiboCielo>() == null) gameObject.AddComponent<KiboCielo>();
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<KiboCamara>() == null)
            {
                var vieja = cam.GetComponent<CrashCamera>();
                var nueva = cam.gameObject.AddComponent<KiboCamara>();
                nueva.objetivo = jugador;
                if (vieja != null) vieja.enabled = false;
            }

            if (KiboGuardado.CargarAlIniciar)
            {
                KiboGuardado.CargarAlIniciar = false;
                StartCoroutine(CargarDespues());
            }
            else
            {
                KiboGuardado.Borrar(); // partida nueva
                ActualizarUI();
            }
        }

        void ContarResiduos()
        {
            idResiduo.Clear();
            itemsPorNombre.Clear();
            total = 0;
            // Algunos residuos estaban DENTRO de otro residuo (p. ej. varias cáscaras de banano
            // colgando de la primera). Al recoger la primera se iban todas y el clic fallaba.
            // Se separan para que cada uno se recoja por su cuenta.
            foreach (var r in KiboUtil.Buscar<ItemRecolectable>())
            {
                if (r.gameObject.scene != gameObject.scene || r.transform.parent == null) continue;
                var padreResiduo = r.transform.parent.GetComponentInParent<ItemRecolectable>(true);
                if (padreResiduo == null) continue;
                Transform nuevoPadre = padreResiduo.transform.parent;
                while (nuevoPadre != null && nuevoPadre.GetComponentInParent<ItemRecolectable>(true) != null)
                    nuevoPadre = nuevoPadre.parent;
                r.transform.SetParent(nuevoPadre, true);
                var resp = r.GetComponent<ObjetoRecolectableRespawn>();
                if (resp != null) resp.FijarPadreInicial(nuevoPadre);
            }

            foreach (var r in KiboUtil.Buscar<ItemRecolectable>())
            {
                if (r.gameObject.scene != gameObject.scene) continue;
                idResiduo[r.gameObject] = KiboUtil.Ruta(r.transform);
                AsegurarZonaClic(r);
                if (r.itemData != null) itemsPorNombre[r.itemData.name] = r.itemData;
                total++;
            }
            meta = Mathf.Max(1, Mathf.CeilToInt(total * porcentajeMeta));
        }

        // Agrega una zona de clic invisible un poco más grande que el residuo,
        // así se recoge al primer clic aunque el objeto sea pequeño o no tenga colisionador.
        static void AsegurarZonaClic(ItemRecolectable r)
        {
            if (r.transform.Find("KIBO_ZonaClic") != null) return;

            // Tamaño del residuo en su propio espacio (funciona aunque esté desactivado, p. ej. dentro de un cofre)
            bool hay = false;
            Bounds local = new Bounds();
            foreach (var mf in r.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Bounds mb = mf.sharedMesh.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 esquina = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    Vector3 p = r.transform.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                    if (!hay) { local = new Bounds(p, Vector3.zero); hay = true; }
                    else local.Encapsulate(p);
                }
            }
            if (!hay) return;

            var go = new GameObject("KIBO_ZonaClic");
            go.layer = r.gameObject.layer;
            try { go.tag = r.gameObject.tag; } catch { }
            go.transform.SetParent(r.transform, false);
            go.transform.localPosition = local.center;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 ls = r.transform.lossyScale;
            float sx = Mathf.Max(0.0001f, Mathf.Abs(ls.x)), sy = Mathf.Max(0.0001f, Mathf.Abs(ls.y)), sz = Mathf.Max(0.0001f, Mathf.Abs(ls.z));
            // un poco más grande que el objeto y mínimo 60 cm, para atinarle fácil
            box.size = new Vector3(
                Mathf.Max(local.size.x + 0.2f / sx, 0.6f / sx),
                Mathf.Max(local.size.y + 0.2f / sy, 0.6f / sy),
                Mathf.Max(local.size.z + 0.2f / sz, 0.6f / sz));
        }

        void CrearCanecasRuntime()
        {
            Transform j = jugador;
            Vector3 basePos = j != null ? j.position : Vector3.zero;
            Vector3 adelante = j != null ? j.forward : Vector3.forward;
            adelante.y = 0;
            if (adelante.sqrMagnitude < 0.01f) adelante = Vector3.forward;
            adelante.Normalize();
            Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
            var cuerpo = KiboCanecaModelo.MallaCuerpo();
            var aro = KiboCanecaModelo.MallaBorde();
            var padre = new GameObject("KIBO_Canecas");
            for (int i = 0; i < 3; i++)
            {
                var tipo = InfoCaneca.Todas[i];
                var go = KiboCanecaModelo.Construir(tipo, cuerpo, aro, KiboCanecaModelo.MaterialesRuntime(tipo));
                go.transform.SetParent(padre.transform, false);
                Vector3 p = basePos + adelante * 4f + derecha * ((i - 1) * 1.3f);
                RaycastHit hit;
                if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out hit, 50f, ~0, QueryTriggerInteraction.Ignore)) p = hit.point;
                go.transform.position = p;
                go.transform.rotation = Quaternion.LookRotation(adelante);
            }
        }

        void Update()
        {
            if (KiboUI.Instancia == null) return;

            if (Input.GetKeyDown(KeyCode.Escape)) KiboUI.Instancia.TeclaEscape();

            if (EntradaBloqueada) return;

            if (Input.GetMouseButtonDown(0) && !KiboUtil.PunteroSobreUI())
            {
                Caneca c = CanecaBajoMouse();
                if (c != null) UsarCaneca(c);
            }
        }

        static bool SobreUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        Caneca CanecaBajoMouse()
        {
            var cam = Camera.main;
            if (cam == null) return null;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                var c = h.collider.GetComponentInParent<Caneca>();
                if (c != null) return c;
                // si el primer objeto es otro objeto sólido grande (pared, árbol) no se atraviesa
                if (!h.collider.isTrigger && h.collider.GetComponentInParent<PlayerMovement>() == null && h.distance > 0.5f
                    && h.collider.bounds.size.magnitude > 6f) return null;
            }
            return null;
        }

        public void UsarCaneca(Caneca c)
        {
            if (jugador == null) jugador = KiboUtil.Jugador();
            if (jugador != null)
            {
                Vector3 a = jugador.position, b = c.transform.position;
                a.y = 0; b.y = 0;
                if (Vector3.Distance(a, b) > distanciaCaneca)
                {
                    KiboUI.Instancia.Aviso("¡Acércate más a la caneca para usarla!", c.tipo);
                    KiboSonidos.Aviso();
                    return;
                }
            }
            KiboUI.Instancia.AbrirClasificacion(c);
        }

        // ------------------------------------------------------------------
        //  Llamado por PlayerPickUp cuando un residuo entra al inventario
        // ------------------------------------------------------------------
        public void AlRecoger(ItemData item, GameObject objeto)
        {
            recolectados++;
            string id;
            if (objeto != null && idResiduo.TryGetValue(objeto, out id) && !recogidos.Contains(id)) recogidos.Add(id);
            if (item != null && !itemsPorNombre.ContainsKey(item.name)) itemsPorNombre[item.name] = item;
            KiboSonidos.Recoger();
            if (KiboUI.Instancia != null)
            {
                KiboUI.Instancia.MostrarRecogido(item);
                KiboUI.Instancia.ActualizarHUD();
            }
        }

        public void AlInventarioLleno()
        {
            KiboSonidos.Aviso();
            if (KiboUI.Instancia != null)
                KiboUI.Instancia.Aviso("¡Tu barra está llena! Lleva los residuos a las canecas o guárdalos en la maleta.", TipoCaneca.Blanca);
        }

        // Devuelve true si el residuo va en esa caneca
        public bool Clasificar(ItemData item, TipoCaneca caneca)
        {
            bool bien = KiboClasificador.Tipo(item) == caneca;
            if (bien) { aciertos++; KiboSonidos.Bien(); }
            else { fallos++; KiboSonidos.Mal(); }
            if (!Terminado && aciertos >= meta)
            {
                Terminado = true;
                KiboGuardado.Borrar();
                int p = Puntuacion;
                if (p > KiboGuardado.MejorPuntaje) KiboGuardado.MejorPuntaje = p;
            }
            if (KiboUI.Instancia != null) KiboUI.Instancia.ActualizarHUD();
            return bien;
        }

        // Llamado por GameManager.ReiniciarJugador (Game Over > Reiniciar)
        public void ReiniciarNivel()
        {
            aciertos = fallos = recolectados = 0;
            Terminado = false;
            recogidos.Clear();
            ActualizarUI();
        }

        void ActualizarUI()
        {
            if (KiboUI.Instancia != null) KiboUI.Instancia.ActualizarHUD();
        }

        // ------------------------------------------------------------------
        //  Guardar / Cargar partida
        // ------------------------------------------------------------------
        public void Guardar()
        {
            var d = new DatosPartida
            {
                aciertos = aciertos,
                fallos = fallos,
                recolectados = recolectados,
                ocultos = new List<string>(recogidos)
            };
            var inv = Object.FindAnyObjectByType<InventarioUI>();
            if (inv != null && inv.slotsBarra != null)
                foreach (var s in inv.slotsBarra)
                    if (s != null && !s.EstaVacio()) d.barra.Add(s.itemActual.name);
            var full = KiboUtil.BuscarUno<InventarioFullUI>();
            if (full != null && full.slotsFull != null)
                foreach (var s in full.slotsFull)
                    if (s != null && !s.EstaVacio()) { d.mochila.Add(s.Item.name); d.mochilaCantidad.Add(s.Cantidad); }
            if (jugador == null) jugador = KiboUtil.Jugador();
            if (jugador != null)
            {
                d.tienePosicion = true;
                d.jugadorX = jugador.position.x; d.jugadorY = jugador.position.y; d.jugadorZ = jugador.position.z;
            }
            KiboGuardado.Guardar(d);
            if (KiboUI.Instancia != null) KiboUI.Instancia.Aviso("¡Partida guardada!", TipoCaneca.Verde);
            KiboSonidos.Bien();
        }

        IEnumerator CargarDespues()
        {
            yield return null; // deja que los demás scripts terminen su Start
            var d = KiboGuardado.Leer();
            if (d == null) { ActualizarUI(); yield break; }

            aciertos = d.aciertos;
            fallos = d.fallos;
            recolectados = d.recolectados;
            recogidos.Clear();

            var porId = new Dictionary<string, GameObject>();
            foreach (var kv in idResiduo) if (kv.Key != null) porId[kv.Value] = kv.Key;
            foreach (var id in d.ocultos)
            {
                GameObject go;
                if (porId.TryGetValue(id, out go) && go != null)
                {
                    go.SetActive(false);
                    recogidos.Add(id);
                }
            }

            var inv = Object.FindAnyObjectByType<InventarioUI>();
            if (inv != null) foreach (var n in d.barra) { ItemData it; if (itemsPorNombre.TryGetValue(n, out it)) inv.AgregarItem(it); }
            var full = KiboUtil.BuscarUno<InventarioFullUI>();
            if (full != null)
                for (int i = 0; i < d.mochila.Count; i++)
                {
                    ItemData it;
                    if (!itemsPorNombre.TryGetValue(d.mochila[i], out it)) continue;
                    int cant = i < d.mochilaCantidad.Count ? d.mochilaCantidad[i] : 1;
                    for (int k = 0; k < cant; k++) full.AgregarItem(it);
                }

            if (d.tienePosicion)
            {
                if (jugador == null) jugador = KiboUtil.Jugador();
                if (jugador != null)
                {
                    var p = new Vector3(d.jugadorX, d.jugadorY, d.jugadorZ);
                    var rb = jugador.GetComponent<Rigidbody>();
                    if (rb != null) rb.position = p;
                    jugador.position = p;
                }
            }
            ActualizarUI();
            if (KiboUI.Instancia != null) KiboUI.Instancia.Aviso("¡Partida cargada! Sigue clasificando residuos.", TipoCaneca.Verde);
        }
    }
}
