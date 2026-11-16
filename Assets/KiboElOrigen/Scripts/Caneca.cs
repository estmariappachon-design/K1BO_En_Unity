using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KiboElOrigen
{
    // Caneca de reciclaje. Muévela libremente en la escena.
    public class Caneca : MonoBehaviour
    {
        public TipoCaneca tipo = TipoCaneca.Blanca;

        [Tooltip("Altura de la boca de la caneca (metros)")]
        public float alto = 1f;

        public static readonly List<Caneca> Todas = new List<Caneca>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { Todas.Clear(); }

        void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
        void OnDisable() { Todas.Remove(this); }

        // Si lanzas o dejas caer un residuo dentro de la caneca también cuenta
        void OnTriggerEnter(Collider otro)
        {
            var r = otro.GetComponentInParent<Residuo>();
            if (r == null || !r.enabled || r.recogido) return;
            var juego = KiboJuego.Instancia;
            if (juego != null && juego.jugando) juego.Depositar(r, this);
        }

        public IEnumerator Tragar(Residuo r)
        {
            r.recogido = true;
            r.Fisica(false);
            r.enabled = false; // deja de contar como residuo pendiente

            Transform t = r.transform;
            Vector3 inicio = t.position;
            Vector3 arriba = transform.position + transform.up * (alto + 0.35f);
            Vector3 dentro = transform.position + transform.up * (alto * 0.3f);
            Vector3 escala = t.localScale;

            float k = 0f;
            while (k < 1f && t != null)
            {
                k += Time.deltaTime * 2.2f;
                t.position = k < 0.5f
                    ? Vector3.Lerp(inicio, arriba, k * 2f)
                    : Vector3.Lerp(arriba, dentro, (k - 0.5f) * 2f);
                t.localScale = Vector3.Lerp(escala, escala * 0.3f, k);
                yield return null;
            }
            if (r != null) Destroy(r.gameObject);
        }

        public void Expulsar(Residuo r)
        {
            r.transform.position = transform.position + transform.up * (alto + 0.4f) - transform.forward * 0.8f;
            r.recogido = false;
            r.Fisica(true);
            r.Lanzar(-transform.forward * 2f + Vector3.up * 1.5f);
        }
    }
}
