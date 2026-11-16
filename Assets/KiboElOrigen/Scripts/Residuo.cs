using System.Collections.Generic;
using UnityEngine;

namespace KiboElOrigen
{
    // Pon este componente en cualquier objeto que sea un residuo recogible.
    [DisallowMultipleComponent]
    public class Residuo : MonoBehaviour
    {
        [Tooltip("Nombre que aparece en los mensajes")]
        public string nombre = "Residuo";

        [Tooltip("Caneca en la que va este residuo")]
        public TipoCaneca tipo = TipoCaneca.Blanca;

        [HideInInspector] public bool recogido;

        public static readonly List<Residuo> Todos = new List<Residuo>();

        Rigidbody rb;
        bool kinematicoOriginal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Todos.Clear(); }

        public string NombreMostrar
        {
            get { return string.IsNullOrEmpty(nombre) ? gameObject.name : nombre; }
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb) kinematicoOriginal = rb.isKinematic;
        }

        void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
        void OnDisable() { Todos.Remove(this); }

        public void Fisica(bool activa)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = activa;
            if (!rb) return;
            if (!activa)
            {
                if (!rb.isKinematic)
                {
                    PonerVelocidad(Vector3.zero);
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            else
            {
                rb.isKinematic = kinematicoOriginal;
            }
        }

        public void Lanzar(Vector3 velocidad)
        {
            if (rb && !rb.isKinematic) PonerVelocidad(velocidad);
        }

        void PonerVelocidad(Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
