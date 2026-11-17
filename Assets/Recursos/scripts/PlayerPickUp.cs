using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using KiboElOrigen;

// Recoger residuos con clic: el residuo se teletransporta (gira, se encoge entre chispas)
// y vuela hasta la barra de inventario. Ya no se mueve el brazo del personaje.
public class PlayerPickUp : MonoBehaviour
{
    [Header("Referencias (opcionales)")]
    public Animator anim;            // ya no se usa para recoger
    public Transform handPoint;      // ya no se usa para recoger
    public Transform hombro;         // ya no se usa para recoger
    public InventarioUI inventarioUI;

    [Header("Recoger")]
    [Tooltip("Distancia máxima entre el personaje y el residuo para recogerlo")]
    public float alcance = 8f;
    [Tooltip("(Antes) distancia del rayo desde la cámara")]
    public float distanciaRayo = 10f;

    [Header("Teletransporte")]
    public float duracionTeletransporte = 0.5f;
    public bool efectoEnElPersonaje = true;

    private bool recogiendo = false;

    void Start()
    {
        if (inventarioUI == null) inventarioUI = Object.FindAnyObjectByType<InventarioUI>();
    }

    void Update()
    {
        bool puedeInteractuar =
            !(GameManager.Instance != null && GameManager.Instance.estaEnMinijuego) &&
            !KiboJuego.EntradaBloqueada &&   // ventana abierta o juego en pausa
            !recogiendo;

        ItemRecolectable encima = null;
        if (puedeInteractuar && !KiboUtil.PunteroSobreUI())
        {
            Camera cam = Camera.main;
            if (cam != null) encima = BuscarResiduo(cam.ScreenPointToRay(Input.mousePosition));
        }

        bool alAlcance = encima != null && EstaAlAlcance(encima.gameObject);

        // Cambia de color el residuo que se puede tomar
        Resaltar(alAlcance ? encima : null);

        if (encima == null || !Input.GetMouseButtonDown(0))
            return;

        if (!alAlcance)
        {
            if (KiboUI.Instancia != null)
                KiboUI.Instancia.Aviso("¡Está muy lejos! Acércate más para recogerlo.", TipoCaneca.Blanca);
            KiboSonidos.Aviso();
            return;
        }

        // verificar espacio antes de recoger
        if (inventarioUI != null && !inventarioUI.TieneEspacio())
        {
            if (KiboJuego.Instancia != null)
                KiboJuego.Instancia.AlInventarioLleno();
            return;
        }

        Resaltar(null);
        StartCoroutine(Teletransportar(encima.gameObject));
    }

    void OnDisable()
    {
        Resaltar(null);
    }

    bool EstaAlAlcance(GameObject obj)
    {
        Vector3 a = transform.position, b = obj.transform.position;
        Collider colObj = obj.GetComponentInChildren<Collider>();
        if (colObj != null && colObj.enabled) b = colObj.bounds.center;
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b) <= alcance;
    }

    // ------------------------------------------------------------------
    //  Resaltado: el residuo brilla cuando el mouse está encima y se puede tomar
    // ------------------------------------------------------------------
    [Header("Resaltado al pasar el mouse")]
    [Tooltip("Color con el que brilla el residuo que se puede tomar")]
    public Color colorResaltado = new Color(1f, 0.92f, 0.25f);
    [Range(0f, 1f)] public float fuerzaResaltado = 0.65f;

    ItemRecolectable resaltado;
    Renderer[] rendResaltados;
    MaterialPropertyBlock bloque;

    void Resaltar(ItemRecolectable nuevo)
    {
        if (nuevo != resaltado)
        {
            // quita el color al anterior
            if (rendResaltados != null)
                foreach (var r in rendResaltados)
                    if (r != null) r.SetPropertyBlock(null);
            resaltado = nuevo;
            rendResaltados = nuevo != null ? nuevo.GetComponentsInChildren<Renderer>() : null;
        }
        if (resaltado == null || rendResaltados == null) return;
        if (bloque == null) bloque = new MaterialPropertyBlock();

        float pulso = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 7f);
        foreach (var r in rendResaltados)
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            Material m = r.sharedMaterial;
            if (m == null) continue;
            r.GetPropertyBlock(bloque);
            if (m.HasProperty("_Color"))
            {
                Color original = m.GetColor("_Color");
                Color c = Color.Lerp(original, colorResaltado, fuerzaResaltado * pulso);
                c.a = original.a;
                bloque.SetColor("_Color", c);
            }
            if (m.HasProperty("_BaseColor"))
            {
                Color original = m.GetColor("_BaseColor");
                Color c = Color.Lerp(original, colorResaltado, fuerzaResaltado * pulso);
                c.a = original.a;
                bloque.SetColor("_BaseColor", c);
            }
            if (m.HasProperty("_EmissionColor") && m.IsKeywordEnabled("_EMISSION"))
                bloque.SetColor("_EmissionColor", colorResaltado * (0.35f * pulso));
            r.SetPropertyBlock(bloque);
        }
    }

    // Encuentra el residuo bajo el mouse. Es tolerante: si el clic cae un poco al lado
    // del objeto, o hay algo pequeño delante, igual lo encuentra.
    ItemRecolectable BuscarResiduo(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue; // el personaje no tapa
            var it = hit.collider.GetComponentInParent<ItemRecolectable>();
            if (it != null && it.itemData != null) return it;
        }

        // Rayo "gordo" por si el clic quedó cerca pero no justo encima
        foreach (float radio in new[] { 0.25f, 0.5f })
        {
            hits = Physics.SphereCastAll(ray, radio, 200f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform)) continue;
                var it = hit.collider.GetComponentInParent<ItemRecolectable>();
                if (it != null && it.itemData != null) return it;
            }
        }
        return null;
    }

    IEnumerator Teletransportar(GameObject obj)
    {
        recogiendo = true;

        ItemRecolectable item = obj.GetComponent<ItemRecolectable>();
        ItemData data = item != null ? item.itemData : null;
        Color color = KiboEfectos.ColorDe(KiboClasificador.Tipo(data));

        // Apaga la física mientras se teletransporta
        Collider[] cols = obj.GetComponentsInChildren<Collider>();
        bool[] estados = new bool[cols.Length];
        for (int i = 0; i < cols.Length; i++) { estados[i] = cols[i].enabled; cols[i].enabled = false; }
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        bool kinematico = false;
        if (rb != null) { kinematico = rb.isKinematic; rb.isKinematic = true; }

        Transform t = obj.transform;
        Vector3 escala0 = t.localScale;
        Vector3 pos0 = t.position;
        Quaternion rot0 = t.rotation;

        Vector3 centro = pos0;
        Renderer rend = obj.GetComponentInChildren<Renderer>();
        if (rend != null) centro = rend.bounds.center;

        KiboEfectos.Teletransporte(centro, color);

        // 1) se infla un poquito
        float d1 = duracionTeletransporte * 0.3f;
        for (float k = 0; k < d1; k += Time.deltaTime)
        {
            float p = k / d1;
            t.localScale = escala0 * Mathf.Lerp(1f, 1.25f, p);
            t.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        // 2) gira rápido, sube y desaparece
        float d2 = duracionTeletransporte * 0.7f;
        for (float k = 0; k < d2; k += Time.deltaTime)
        {
            float p = k / d2;
            t.localScale = escala0 * Mathf.Lerp(1.25f, 0f, p * p);
            t.position = pos0 + Vector3.up * (p * 0.9f);
            t.Rotate(0f, 1100f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        t.localScale = Vector3.zero;

        // 3) el ícono vuela a la barra de inventario
        bool llego = false;
        if (KiboUI.Instancia != null && data != null && data.icono != null)
        {
            KiboUI.Instancia.VolarAlInventario(data.icono, centro + Vector3.up * 0.9f, () => llego = true);
            float espera = 0f;
            while (!llego && espera < 1.5f)
            {
                espera += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (data != null && inventarioUI != null)
            inventarioUI.AgregarItem(data);

        if (efectoEnElPersonaje)
            KiboEfectos.Teletransporte(transform.position + Vector3.up * 0.8f, color, 0.5f);

        // Avisa al nivel: muestra "Este residuo va en la caneca de color ..."
        if (item != null && KiboJuego.Instancia != null)
            KiboJuego.Instancia.AlRecoger(data, obj);

        // Deja el objeto como estaba (por si vuelve a aparecer después de un Game Over)
        t.localScale = escala0;
        t.position = pos0;
        t.rotation = rot0;
        for (int i = 0; i < cols.Length; i++) if (cols[i] != null) cols[i].enabled = estados[i];
        if (rb != null) rb.isKinematic = kinematico;

        ObjetoRecolectableRespawn respawn = obj.GetComponent<ObjetoRecolectableRespawn>();
        if (respawn != null) respawn.OcultarObjeto();
        else Destroy(obj);

        recogiendo = false;
    }
}
