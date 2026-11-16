using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KiboElOrigen
{
    // Lógica del juego: recoger residuos (E), echarlos en la caneca (E), soltarlos (Q).
    public class KiboJuego : MonoBehaviour
    {
        public static KiboJuego Instancia;

        [Tooltip("Jugador. Si lo dejas vacío se busca el objeto con tag Player o la cámara principal.")]
        public Transform jugador;

        [Tooltip("Distancia para recoger residuos y usar las canecas (metros)")]
        public float distancia = 2.5f;

        [HideInInspector] public bool jugando;
        [HideInInspector] public int aciertos, fallos;

        Residuo cargado;

        static readonly Color colorInfo = new Color(0.05f, 0.15f, 0.35f, 0.92f);
        static readonly Color colorBien = new Color(0.10f, 0.55f, 0.20f, 0.95f);
        static readonly Color colorMal = new Color(0.75f, 0.10f, 0.10f, 0.95f);
        static readonly Color colorFin = new Color(0.80f, 0.55f, 0.05f, 0.95f);

        void Awake() { Instancia = this; }

        void Start()
        {
            if (KiboUI.Instancia == null) Comenzar(); // por si no hay interfaz
        }

        public void Comenzar()
        {
            jugando = true;
            aciertos = 0;
            fallos = 0;
            Puntaje();
            Mensaje("Recoge los residuos con la tecla <b>E</b> y llévalos a la caneca correcta.", colorInfo, 5f);
        }

        Transform Jugador()
        {
            if (jugador) return jugador;
            GameObject p = null;
            try { p = GameObject.FindGameObjectWithTag("Player"); } catch { }
            if (p) jugador = p.transform;
            else if (Camera.main) jugador = Camera.main.transform;
            return jugador;
        }

        void Update()
        {
            if (!jugando) return;
            Transform j = Jugador();
            if (!j) return;

            if (cargado) MoverCargado(j);

            Residuo residuoCerca = cargado ? null : Cercano(Residuo.Todos, j.position, true);
            Caneca canecaCerca = Cercano(Caneca.Todas, j.position, false);

            if (cargado)
            {
                if (canecaCerca)
                    Ayuda("<b>E</b>: echar " + cargado.NombreMostrar + " en la caneca " + InfoCaneca.Pintado(canecaCerca.tipo) + "     <b>Q</b>: soltar");
                else
                    Ayuda("Llevas: <b>" + cargado.NombreMostrar + "</b>  ·  acércate a una caneca     <b>Q</b>: soltar");
            }
            else if (residuoCerca) Ayuda("<b>E</b>: recoger " + residuoCerca.NombreMostrar);
            else Ayuda("");

            if (KiboEntrada.Interactuar())
            {
                if (cargado)
                {
                    if (canecaCerca) Depositar(cargado, canecaCerca);
                    else Mensaje("Acércate a una caneca para echar el residuo.", colorInfo, 2f);
                }
                else if (residuoCerca) Recoger(residuoCerca);
            }
            else if (KiboEntrada.Soltar() && cargado) Soltar(j);
        }

        T Cercano<T>(List<T> lista, Vector3 p, bool soloLibres) where T : Component
        {
            T mejor = null;
            float md = distancia;
            for (int i = 0; i < lista.Count; i++)
            {
                T x = lista[i];
                if (!x) continue;
                if (soloLibres)
                {
                    var r = x as Residuo;
                    if (r != null && r.recogido) continue;
                }
                Vector3 a = p, b = x.transform.position;
                a.y = 0; b.y = 0;
                float d = Vector3.Distance(a, b);
                if (d < md) { md = d; mejor = x; }
            }
            return mejor;
        }

        void Recoger(Residuo r)
        {
            cargado = r;
            r.recogido = true;
            r.Fisica(false);
            Mensaje("Recogiste: <b>" + r.NombreMostrar + "</b>\nEste residuo va en la caneca de color " + InfoCaneca.Pintado(r.tipo),
                colorInfo, 4f);
        }

        void MoverCargado(Transform j)
        {
            Vector3 destino;
            Camera cam = Camera.main;
            bool primeraPersona = cam && (cam.transform == j || Vector3.Distance(cam.transform.position, j.position) < 1.5f);
            if (primeraPersona)
            {
                Transform m = cam.transform;
                destino = m.position + m.forward * 1.0f + m.right * 0.35f - m.up * 0.35f;
            }
            else
            {
                destino = j.position + j.forward * 0.7f + Vector3.up * 1.2f;
            }
            Transform t = cargado.transform;
            t.position = Vector3.Lerp(t.position, destino, 1f - Mathf.Exp(-20f * Time.deltaTime));
        }

        void Soltar(Transform j)
        {
            Residuo r = cargado;
            cargado = null;
            r.recogido = false;
            r.Fisica(true);
            r.Lanzar(j.forward * 1.5f);
        }

        public void Depositar(Residuo r, Caneca c)
        {
            if (r.tipo == c.tipo)
            {
                aciertos++;
                if (r == cargado) cargado = null;
                Mensaje("<size=48><b>¡BIEN!</b></size>\n" + r.NombreMostrar + " va en la caneca " + InfoCaneca.Pintado(c.tipo), colorBien, 3f);
                StartCoroutine(c.Tragar(r));
                if (Residuo.Todos.Count == 0) StartCoroutine(Felicitar());
            }
            else
            {
                fallos++;
                Mensaje("<size=48><b>¡FALLASTE!</b></size>\n" + r.NombreMostrar + " no va en la caneca " + InfoCaneca.Pintado(c.tipo) +
                        ". Va en la " + InfoCaneca.Pintado(r.tipo), colorMal, 3.5f);
                if (r != cargado) c.Expulsar(r);
            }
            Puntaje();
        }

        IEnumerator Felicitar()
        {
            yield return new WaitForSecondsRealtime(3.2f);
            Mensaje("<size=48><b>¡FELICIDADES!</b></size>\nClasificaste todos los residuos.  Bien: " + aciertos + "   Fallos: " + fallos,
                colorFin, 7f);
        }

        void Mensaje(string t, Color c, float s) { if (KiboUI.Instancia) KiboUI.Instancia.Mensaje(t, c, s); }
        void Ayuda(string t) { if (KiboUI.Instancia) KiboUI.Instancia.Ayuda(t); }
        void Puntaje() { if (KiboUI.Instancia) KiboUI.Instancia.Puntaje(aciertos, fallos); }
    }
}
