using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Duración")]
    public float dayDuration = 180f;
    public float nightDuration = 180f;

    [Header("Luz Sol")]
    public Light sun;

    [Header("Luz del personaje")]
    public Light playerLight;

    [Header("Intensidad")]
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.08f;

    [Header("Luz personaje")]
    public float playerLightMaxIntensity = 2.5f;
    public float playerLightRange = 12f;

    private float timer = 0f;
    private bool isNight = false;

    void Start()
    {
        if (playerLight != null)
        {
            playerLight.enabled = true;
            playerLight.intensity = 0f;
            playerLight.range = playerLightRange;
        }
    }

    void Update()
    {
        if (sun == null) return;

        timer += Time.deltaTime;

        if (!isNight)
        {
            float t = timer / dayDuration;

            sun.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);

            RenderSettings.ambientLight = Color.Lerp(
                Color.white,
                new Color(0.08f, 0.08f, 0.15f),
                t
            );

            sun.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(50f, 180f, t),
                0f,
                0f
            );

            if (playerLight != null)
            {
                playerLight.intensity = Mathf.Lerp(0f, playerLightMaxIntensity, t);
            }

            if (timer >= dayDuration)
            {
                timer = 0f;
                isNight = true;
            }
        }
        else
        {
            float t = timer / nightDuration;

            sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);

            RenderSettings.ambientLight = Color.Lerp(
                new Color(0.08f, 0.08f, 0.15f),
                Color.white,
                t
            );

            sun.transform.rotation = Quaternion.Euler(
                Mathf.Lerp(180f, 410f, t),
                0f,
                0f
            );

            if (playerLight != null)
            {
                playerLight.intensity = Mathf.Lerp(playerLightMaxIntensity, 0f, t);
            }

            if (timer >= nightDuration)
            {
                timer = 0f;
                isNight = false;
            }
        }
    }
}










/*
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Tiempo total (día + noche)")]
    public float cycleDuration = 360f;

    [Header("Luz (Sol)")]
    public Light sun;

    [Header("Intensidad")]
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.02f;

    private float timer = 10f;

    [Header("Luz del personaje")]
    public Light playerLight;

    void Update()
    {
        if (sun == null) return;

        timer += Time.deltaTime;

        // Normalizado
        float t = timer / cycleDuration;

        // Rotación del sol
        float angle = t * 360f;
        sun.transform.rotation = Quaternion.Euler(angle - 90f, 0f, 0f);

        // 🔥 EMPIEZA DE DÍA
        float lightFactor = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) / 2f;

        // Intensidad de luz
        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, lightFactor);

        // Luz ambiental
        Color dayColor = Color.white;
        Color nightColor = new Color(0.05f, 0.05f, 0.1f);

        RenderSettings.ambientLight = Color.Lerp(nightColor, dayColor, lightFactor);

        if (playerLight != null)
        {
            playerLight.enabled = lightFactor < 0.3f;
        }

        // Reiniciar ciclo
        if (timer >= cycleDuration)
        {
            timer = 0f;
        }
    }
}
*/










//using UnityEngine;

//public class DayNightCycle : MonoBehaviour
//{
//    [Header("Tiempo total (día + noche)")]
//    public float cycleDuration = 20f; // 10 día + 10 noche

//    [Header("Luz (Sol)")]
//    public Light sun;

//    [Header("Intensidad")]
//    public float dayIntensity = 1.2f;
//    public float nightIntensity = 0.02f;

//    private float timer = 15f;

//    void Update()
//    {
//        if (sun == null) return;

//        timer += Time.deltaTime;

//        // Normalizado (0 a 1)
//        float t = timer / cycleDuration;

//        // 🔥 ROTACIÓN SUAVE 360°
//        float angle = t * 360f;
//        sun.transform.rotation = Quaternion.Euler(angle - 90f, 0f, 0f);

//        // 🔥 CURVA SUAVE (día → noche → día)
//        float lightFactor = (Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f;

//        // Intensidad
//        sun.intensity = Mathf.Lerp(nightIntensity, dayIntensity, lightFactor);

//        // Luz ambiental
//        Color dayColor = Color.white;
//        Color nightColor = new Color(0.05f, 0.05f, 0.1f);

//        RenderSettings.ambientLight = Color.Lerp(nightColor, dayColor, lightFactor);

//        // Reiniciar ciclo
//        if (timer >= cycleDuration)
//        {
//            timer = 0f;
//        }
//    }
//}