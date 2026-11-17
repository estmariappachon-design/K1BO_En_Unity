using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Cambio de escena: se oscurece, gira la ruedita azul de carga
    // y luego se aclara suavemente sobre la escena nueva.
    public class KiboTransicion : MonoBehaviour
    {
        static KiboTransicion actual;

        CanvasGroup grupo, grupoCarga;
        RectTransform rueda;

        public static void Cargar(string escena, float duracion = 0.4f)
        {
            Time.timeScale = 1f;
            if (actual != null) return;
            var canvas = KiboEstilo.CrearCanvas("KIBO_Transicion", 32000);
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            DontDestroyOnLoad(canvas.gameObject);
            actual = canvas.gameObject.AddComponent<KiboTransicion>();
            actual.Construir((RectTransform)canvas.transform);
            actual.StartCoroutine(actual.Rutina(escena, duracion));
        }

        void Construir(RectTransform raiz)
        {
            grupo = gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.blocksRaycasts = true;

            var fondo = KiboEstilo.Lleno("Fondo", raiz);
            var img = fondo.gameObject.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.07f, 1f);
            img.raycastTarget = true;

            var carga = KiboEstilo.Lleno("Carga", raiz);
            grupoCarga = carga.gameObject.AddComponent<CanvasGroup>();
            grupoCarga.alpha = 0f;

            // Solo la ruedita azul (del mismo azul del marco de los minijuegos)
            rueda = KiboEstilo.Centro("Rueda", carga, Vector2.zero, new Vector2(150, 150));
            KiboEstilo.Imagen(rueda, "cargando", Color.white);
        }

        void Update()
        {
            if (rueda != null) rueda.localRotation = Quaternion.Euler(0, 0, -Time.unscaledTime * 300f);
        }

        IEnumerator Rutina(string escena, float d)
        {
            // 1) se oscurece
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                grupo.alpha = t / d;
                yield return null;
            }
            grupo.alpha = 1f;

            // 2) aparece la ruedita y se carga la escena
            float inicio = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(escena);
            if (op == null)
            {
                SceneManager.LoadScene(escena);
            }
            else
            {
                op.allowSceneActivation = false;
                while (op.progress < 0.9f || Time.unscaledTime - inicio < 1.2f)
                {
                    float p = Mathf.Min(op.progress / 0.9f, (Time.unscaledTime - inicio) / 1.2f);
                    MostrarProgreso(p);
                    yield return null;
                }
                MostrarProgreso(1f);
                yield return new WaitForSecondsRealtime(0.15f);
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
            }

            // espera un par de cuadros a que todo arranque
            yield return null;
            yield return null;
            Time.timeScale = 1f;

            // 3) se aclara sobre la escena nueva
            for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                grupoCarga.alpha = 1f - t / 0.25f;
                grupo.alpha = 1f - t / 0.5f;
                yield return null;
            }
            actual = null;
            Destroy(gameObject);
        }

        void MostrarProgreso(float p)
        {
            grupoCarga.alpha = Mathf.MoveTowards(grupoCarga.alpha, 1f, Time.unscaledDeltaTime * 4f);
        }
    }
}
