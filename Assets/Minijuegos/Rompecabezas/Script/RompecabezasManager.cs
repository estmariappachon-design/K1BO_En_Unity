using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class RompecabezasManager : MonoBehaviour
{
    public List<FichaRompecabezas> fichas;
    public Transform espacioVacio;
    private bool mezclando = false;

    // CRONOMETRO //

    public float tiempoLimite = 480f;
    public TMP_Text textoTiempo;

    private bool juegoActivo = true;

    public int posicionVacia = 7;

    //MENSAJE DE PERDER //
    public GameObject panelTiempoAgotado;
    public TMP_Text textoAnuncio;

    void Start()
    {
        ActualizarPosiciones();

        Invoke("MezclarFichas", 0.1f);
    }

    void Update()
    {
        if (!juegoActivo) return;

        tiempoLimite -= Time.deltaTime;

        if (tiempoLimite < 0)
        {
            tiempoLimite = 0;
        }

        int minutos = Mathf.FloorToInt(tiempoLimite / 60);
        int segundos = Mathf.FloorToInt(tiempoLimite % 60);

        textoTiempo.text = minutos.ToString("00") + ":" + segundos.ToString("00");

        if (tiempoLimite <= 0)
        {
            TiempoAgotado();
        }
    }

    public void IntentarMoverFicha(FichaRompecabezas ficha)
    {
        if (EsAdyacente(ficha.posicionActual, posicionVacia))
        {
            MoverFicha(ficha);
            VerificarVictoria();
        }
    }

    bool EsAdyacente(int posFicha, int posVacia)
    {
        int filaFicha = posFicha / 4;
        int columnaFicha = posFicha % 4;

        int filaVacia = posVacia / 4;
        int columnaVacia = posVacia % 4;

        int diferenciaFila = Mathf.Abs(filaFicha - filaVacia);
        int diferenciaColumna = Mathf.Abs(columnaFicha - columnaVacia);

        return diferenciaFila + diferenciaColumna == 1;
    }

    void MoverFicha(FichaRompecabezas ficha)
    {
        int posicionAnterior = ficha.posicionActual;

        ficha.transform.SetSiblingIndex(posicionVacia);
        espacioVacio.SetSiblingIndex(posicionAnterior);

        ficha.posicionActual = posicionVacia;
        posicionVacia = posicionAnterior;

        ActualizarPosiciones();
    }

    void ActualizarPosiciones()
    {
        foreach (FichaRompecabezas ficha in fichas)
        {
            ficha.posicionActual = ficha.transform.GetSiblingIndex();
        }
    }

    void VerificarVictoria()
    {
        if (mezclando) return;

        foreach (FichaRompecabezas ficha in fichas)
        {
            if (ficha.posicionActual != ficha.numeroCorrecto)
            {
                return;
            }
        }

        Debug.Log("¡Ganaste el rompecabezas!");

        // 🔥 Avisar al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GanarRompecabezas();
            GameManager.Instance.SalirMinijuego();
        }

        // 🔥 Mostrar insignia
        InsigniaUI insigniaUI = FindAnyObjectByType<InsigniaUI>();

        if (insigniaUI != null)
        {
            insigniaUI.MostrarInsignia();
        }

        // 🔥 Cerrar escena del minijuego
        SceneManager.UnloadSceneAsync("Minijuego_Rompecabezas");
    }

    void MezclarFichas()
    {
        mezclando = true;

        for (int i = 0; i < 500; i++)
        {
            ActualizarPosiciones();

            List<FichaRompecabezas> movibles = new List<FichaRompecabezas>();

            foreach (FichaRompecabezas ficha in fichas)
            {
                if (EsAdyacente(ficha.posicionActual, posicionVacia))
                {
                    movibles.Add(ficha);
                }
            }

            if (movibles.Count > 0)
            {
                FichaRompecabezas fichaAleatoria = movibles[Random.Range(0, movibles.Count)];
                MoverFicha(fichaAleatoria);
            }
        }

        ActualizarPosiciones();
        mezclando = false;

        Debug.Log("Rompecabezas mezclado");
    }

    void TiempoAgotado()
    {
        juegoActivo = false;

        Debug.Log("Tiempo agotado ejecutado");

        if (GameManager.Instance != null)
        {
            Debug.Log("Vida antes: " + GameManager.Instance.vidasActuales);
            GameManager.Instance.PerderVida();
            Debug.Log("Vida después: " + GameManager.Instance.vidasActuales);
        }
        else
        {
            Debug.LogError("No existe GameManager.Instance");
        }

        panelTiempoAgotado.SetActive(true);
        textoAnuncio.text = "¡Perdiste!";

        foreach (FichaRompecabezas ficha in fichas)
        {
            ficha.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }

        Debug.Log("Perdiste");
    }

    public void VolverAlMain()
    {
        // 🔥 Quitar bloqueo del MainScene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SalirMinijuego();
        }

        // 🔥 Cerrar escena del rompecabezas
        SceneManager.UnloadSceneAsync("Minijuego_Rompecabezas");
    }

}