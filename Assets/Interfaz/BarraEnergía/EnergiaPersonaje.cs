using UnityEngine;
using UnityEngine.UI;

public class EnergiaPersonaje : MonoBehaviour
{
    [Header("UI")]
    public Slider sliderEnergia;

    [Header("Valores de energía")]
    public float energiaMaxima = 100f;
    public float energiaActual = 100f;

    [Header("Disminución")]
    public float energiaQuePierdePorSegundo = 0.33f;

    [Header("Recarga")]
    public float energiaQueRecargaPorSegundo = 15f;
    public KeyCode teclaRecargar = KeyCode.N;

    [Header("Animación")]
    public Animator anim;

    private bool estaRecargando = false;

    void Start()
    {
        Debug.Log("EnergiaPersonaje sí inició en: " + gameObject.name);

        energiaMaxima = 100f;
        energiaActual = energiaMaxima;

        if (sliderEnergia != null)
        {
            sliderEnergia.minValue = 0;
            sliderEnergia.maxValue = energiaMaxima;
            sliderEnergia.value = energiaActual;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("No hay GameManager.Instance");
            return;
        }

        Debug.Log("Update energía | juegoIniciado: " + GameManager.Instance.juegoIniciado);

        if (!GameManager.Instance.juegoIniciado)
        {
            return;
        }

        estaRecargando = Input.GetKey(teclaRecargar);

        if (estaRecargando)
        {
            RecargarEnergia();

            if (anim != null)
                anim.SetBool("Dormir", true);
        }
        else
        {
            BajarEnergia();

            if (anim != null)
                anim.SetBool("Dormir", false);
        }

        ActualizarBarra();
    }

    void BajarEnergia()
    {
        float perdida = energiaQuePierdePorSegundo * Time.unscaledDeltaTime;

        energiaActual -= perdida;
        energiaActual = Mathf.Clamp(energiaActual, 0, energiaMaxima);

        Debug.Log(
            "Objeto: " + gameObject.name +
            " | Pierde por segundo: " + energiaQuePierdePorSegundo +
            " | DeltaTime: " + Time.unscaledDeltaTime +
            " | Pérdida real: " + perdida +
            " | Energía actual: " + energiaActual
        );
    }

    void RecargarEnergia()
    {
        energiaActual += energiaQueRecargaPorSegundo * Time.unscaledDeltaTime;
        energiaActual = Mathf.Clamp(energiaActual, 0, energiaMaxima);
    }

    void ActualizarBarra()
    {
        if (sliderEnergia != null)
        {
            sliderEnergia.maxValue = energiaMaxima;
            sliderEnergia.value = energiaActual;
        }
    }
}