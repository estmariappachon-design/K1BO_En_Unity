using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Al abrir el minijuego del rompecabezas aparecen primero unas instrucciones
    // con la imagen de cómo debe quedar. El tiempo no corre hasta que se presiona JUGAR.
    public class KiboInstruccionesRompecabezas : MonoBehaviour
    {
        const string ESCENA = "Minijuego_Rompecabezas";
        float escalaPrevia = 1f;
        bool pausado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Registrar()
        {
            SceneManager.sceneLoaded -= AlCargar;
            SceneManager.sceneLoaded += AlCargar;
            SceneManager.sceneUnloaded -= AlDescargar;
            SceneManager.sceneUnloaded += AlDescargar;
        }

        static void AlCargar(Scene escena, LoadSceneMode modo)
        {
            if (escena.name != ESCENA) return;
            KiboMusica.Poner("musica_rompecabezas");
            var go = new GameObject("KIBO_InstruccionesRompecabezas");
            SceneManager.MoveGameObjectToScene(go, escena); // se va cuando se cierra el minijuego
            go.AddComponent<KiboInstruccionesRompecabezas>();
        }

        static void AlDescargar(Scene escena)
        {
            if (escena.name == ESCENA) KiboMusica.Volver();
        }

        void Start()
        {
            escalaPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f; // pausa el cronómetro mientras lee
            pausado = true;

            var canvas = KiboEstilo.CrearCanvas("Canvas", 500, transform);
            var raiz = (RectTransform)canvas.transform;
            KiboEstilo.Oscurecer(raiz, 0.7f);

            var panel = KiboEstilo.Panel(raiz, "Panel", new Vector2(0, -30), new Vector2(1500, 860), "ROMPECABEZAS");
            var c = KiboEstilo.Contenido(panel);

            // Imagen de cómo debe quedar
            var marco = KiboEstilo.Centro("Marco", c, new Vector2(-340, 30), new Vector2(470, 470));
            KiboEstilo.Imagen(marco, "redondo", KiboEstilo.Cafe, true, 0.6f);
            var img = KiboEstilo.Centro("Solucion", marco, Vector2.zero, new Vector2(440, 440));
            KiboEstilo.Imagen(img, "rompecabezas_solucion", Color.white);
            var et = KiboEstilo.Centro("AsiDebeQuedar", c, new Vector2(-340, -240), new Vector2(470, 56));
            KiboEstilo.Texto(et, "ASÍ DEBE QUEDAR", 40, KiboEstilo.Dorado, TextAnchor.MiddleCenter);

            // Pasos
            var tx = KiboEstilo.Centro("Pasos", c, new Vector2(270, 40), new Vector2(640, 520));
            var t = KiboEstilo.Texto(tx,
                "<color=#FFE066>1.</color> Las fichas están desordenadas y hay un <color=#FFE066>espacio vacío</color>.\n\n" +
                "<color=#FFE066>2.</color> Haz clic en una ficha que esté <color=#FFE066>junto al espacio vacío</color> para moverla allí.\n\n" +
                "<color=#FFE066>3.</color> Ve moviendo las fichas hasta que la imagen quede <color=#FFE066>igual a la de la izquierda</color>.\n\n" +
                "<color=#FFE066>4.</color> ¡Termina antes de que se acabe el tiempo! Si no lo logras, pierdes una vida.",
                34, Color.white, TextAnchor.MiddleLeft, false);
            KiboEstilo.AutoTam(t, 18, 34);

            KiboEstilo.Boton(c, "Btn_Jugar", "¡JUGAR!", new Vector2(270, -255), new Vector2(360, 100), Jugar, 54);
        }

        void Jugar()
        {
            pausado = false;
            Time.timeScale = escalaPrevia;
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (pausado) Time.timeScale = escalaPrevia;
        }
    }
}
