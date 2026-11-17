using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KiboElOrigen
{
    public enum ModoCamara { TerceraPersona, PrimeraPersona }

    // Cámara que sigue al personaje.
    //  • Clic derecho + mover el mouse: girar la cámara (izquierda, derecha, arriba, abajo)
    //  • Q / E: girar a la izquierda / derecha     • Z / X: subir / bajar
    //  • Rueda del mouse: acercar / alejar         • V: cambiar entre tercera y primera persona
    [DisallowMultipleComponent]
    public class KiboCamara : MonoBehaviour
    {
        [Header("Modo de cámara")]
        [Tooltip("Tercera persona: se ve al personaje. Primera persona: ves por sus ojos (el personaje existe pero no se ve)")]
        public ModoCamara modo = ModoCamara.TerceraPersona;
        [Tooltip("Tecla para cambiar de modo mientras juegas (None para desactivar)")]
        public KeyCode teclaCambiarModo = KeyCode.V;

        [Header("Personaje")]
        public Transform objetivo;
        [Tooltip("Altura del punto que mira la cámara (pecho del personaje)")]
        public float alturaObjetivo = 1.2f;

        [Header("Tercera persona")]
        public float distancia = 6f;
        public float distanciaMinima = 2.5f;
        public float distanciaMaxima = 14f;
        [Tooltip("Ángulo vertical (más alto = cámara más arriba)")]
        public float anguloVertical = 22f;
        public float anguloMinimo = -5f;
        public float anguloMaximo = 75f;
        [Tooltip("La cámara se acerca si hay una pared entre ella y el personaje")]
        public bool evitarParedes = true;

        [Header("Primera persona")]
        [Tooltip("Altura de los ojos sobre los pies del personaje")]
        public float alturaOjos = 1.55f;
        [Tooltip("Oculta el modelo del personaje (sigue proyectando sombra)")]
        public bool ocultarPersonaje = true;

        [Header("Controles")]
        public bool girarConClicDerecho = true;
        public float sensibilidadMouse = 4f;
        public float velocidadTeclas = 100f;
        public float velocidadZoom = 1.5f;
        [Tooltip("Suavidad del seguimiento (más alto = más rápido)")]
        public float suavizado = 10f;

        float yaw, pitch, pitchPrimera = 5f;
        float distanciaActual;
        bool iniciado;
        ModoCamara modoAplicado;
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<ShadowCastingMode> sombrasOriginales = new List<ShadowCastingMode>();

        void Start()
        {
            if (objetivo == null) objetivo = KiboUtil.Jugador();
            var vieja = GetComponent<CrashCamera>();
            if (vieja != null) vieja.enabled = false; // esta cámara reemplaza a la anterior
            Iniciar();
        }

        void Iniciar()
        {
            if (objetivo == null) return;
            Vector3 pivote = objetivo.position + Vector3.up * alturaObjetivo;
            Vector3 offset = transform.position - pivote;
            var vieja = GetComponent<CrashCamera>();
            if (vieja != null) offset = objetivo.position + vieja.offset - pivote;
            if (offset.sqrMagnitude < 0.5f) offset = new Vector3(0, 2.5f, -6f);
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            float horizontal = new Vector2(offset.x, offset.z).magnitude;
            pitch = Mathf.Clamp(Mathf.Atan2(offset.y, horizontal) * Mathf.Rad2Deg, anguloMinimo, anguloMaximo);
            distanciaActual = distancia = Mathf.Clamp(offset.magnitude, distanciaMinima, distanciaMaxima);
            anguloVertical = pitch;
            renderers.Clear();
            sombrasOriginales.Clear();
            foreach (var r in objetivo.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                renderers.Add(r);
                sombrasOriginales.Add(r.shadowCastingMode);
            }
            modoAplicado = modo == ModoCamara.TerceraPersona ? ModoCamara.PrimeraPersona : ModoCamara.TerceraPersona;
            iniciado = true;
        }

        void LateUpdate()
        {
            if (objetivo == null)
            {
                objetivo = KiboUtil.Jugador();
                if (objetivo == null) return;
            }
            if (!iniciado) Iniciar();

            bool bloqueado = KiboJuego.EntradaBloqueada;
            if (!bloqueado) LeerControles();

            if (modo != modoAplicado) AplicarModo();

            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-suavizado * Mathf.Max(dt, 0.0001f));

            if (modo == ModoCamara.PrimeraPersona)
            {
                Vector3 ojos = objetivo.position + Vector3.up * alturaOjos;
                transform.position = ojos;
                transform.rotation = Quaternion.Euler(pitchPrimera, yaw, 0f);
                return;
            }

            Vector3 pivote = objetivo.position + Vector3.up * alturaObjetivo;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            float dist = distancia;
            if (evitarParedes) dist = DistanciaLibre(pivote, rot * Vector3.back, distancia);
            distanciaActual = dist < distanciaActual ? dist : Mathf.Lerp(distanciaActual, dist, k);
            Vector3 deseada = pivote + rot * Vector3.back * distanciaActual;

            transform.position = Vector3.Lerp(transform.position, deseada, k);
            Quaternion mirar = Quaternion.LookRotation(pivote - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, mirar, k * 1.5f);
        }

        void LeerControles()
        {
            if (teclaCambiarModo != KeyCode.None && Input.GetKeyDown(teclaCambiarModo))
                modo = modo == ModoCamara.TerceraPersona ? ModoCamara.PrimeraPersona : ModoCamara.TerceraPersona;

            float dx = 0f, dy = 0f;
            if (!girarConClicDerecho || Input.GetMouseButton(1))
            {
                dx += Input.GetAxis("Mouse X") * sensibilidadMouse;
                dy -= Input.GetAxis("Mouse Y") * sensibilidadMouse;
            }
            if (Input.GetKey(KeyCode.Q)) dx -= velocidadTeclas * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) dx += velocidadTeclas * Time.deltaTime;
            if (Input.GetKey(KeyCode.Z)) dy += velocidadTeclas * 0.6f * Time.deltaTime;
            if (Input.GetKey(KeyCode.X)) dy -= velocidadTeclas * 0.6f * Time.deltaTime;

            yaw += dx;
            if (modo == ModoCamara.PrimeraPersona)
            {
                pitchPrimera = Mathf.Clamp(pitchPrimera + dy, -70f, 70f);
            }
            else
            {
                pitch = Mathf.Clamp(pitch + dy, anguloMinimo, anguloMaximo);
                float rueda = Input.mouseScrollDelta.y;
                if (Mathf.Abs(rueda) > 0.01f)
                    distancia = Mathf.Clamp(distancia - rueda * velocidadZoom, distanciaMinima, distanciaMaxima);
            }
            anguloVertical = pitch;
        }

        float DistanciaLibre(Vector3 pivote, Vector3 dir, float maxDist)
        {
            var hits = Physics.SphereCastAll(pivote, 0.25f, dir, maxDist, ~0, QueryTriggerInteraction.Ignore);
            float mejor = maxDist;
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(objetivo)) continue;
                if (h.collider.CompareTag("ObjetoAgarrable")) continue;
                if (h.collider.bounds.size.magnitude < 1.5f) continue; // objetos pequeños no empujan la cámara
                if (h.distance <= 0f) continue;
                mejor = Mathf.Min(mejor, h.distance - 0.1f);
            }
            return Mathf.Max(0.8f, mejor);
        }

        void AplicarModo()
        {
            modoAplicado = modo;
            bool ocultar = modo == ModoCamara.PrimeraPersona && ocultarPersonaje;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].shadowCastingMode = ocultar ? ShadowCastingMode.ShadowsOnly : sombrasOriginales[i];
            }
            if (modo == ModoCamara.PrimeraPersona) pitchPrimera = 5f;
        }

        // Para cambiar de modo desde un botón
        public void CambiarModo()
        {
            modo = modo == ModoCamara.TerceraPersona ? ModoCamara.PrimeraPersona : ModoCamara.TerceraPersona;
        }
    }
}
