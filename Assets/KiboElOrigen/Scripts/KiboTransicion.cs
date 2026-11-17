using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KiboElOrigen
{
    // Cambio de escena con fundido a negro
    public class KiboTransicion : MonoBehaviour
    {
        static KiboTransicion actual;
        Image negro;

        public static void Cargar(string escena, float duracion = 0.45f)
        {
            Time.timeScale = 1f;
            if (actual != null) return;
            var canvas = KiboEstilo.CrearCanvas("KIBO_Transicion", 32000);
            DontDestroyOnLoad(canvas.gameObject);
            actual = canvas.gameObject.AddComponent<KiboTransicion>();
            var rt = KiboEstilo.Lleno("Negro", canvas.transform);
            actual.negro = rt.gameObject.AddComponent<Image>();
            actual.negro.color = new Color(0, 0, 0, 0);
            actual.negro.raycastTarget = true;
            actual.StartCoroutine(actual.Rutina(escena, duracion));
        }

        IEnumerator Rutina(string escena, float d)
        {
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                negro.color = new Color(0, 0, 0, t / d);
                yield return null;
            }
            negro.color = Color.black;
            var op = SceneManager.LoadSceneAsync(escena);
            if (op != null)
            {
                while (!op.isDone) yield return null;
            }
            else
            {
                SceneManager.LoadScene(escena);
            }
            yield return null;
            Time.timeScale = 1f;
            for (float t = 0; t < d; t += Time.unscaledDeltaTime)
            {
                negro.color = new Color(0, 0, 0, 1f - t / d);
                yield return null;
            }
            actual = null;
            Destroy(gameObject);
        }
    }
}
