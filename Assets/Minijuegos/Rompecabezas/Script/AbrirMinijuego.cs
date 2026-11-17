using UnityEngine;
using UnityEngine.SceneManagement;

public enum TipoMinijuego { Alternar, Rompecabezas, Transmilenio }

// Va en la tarjeta del minijuego. Al hacerle clic abre el minijuego que eligió el cofre.
public class AbrirMinijuego : MonoBehaviour
{
    public GameObject panelBlur;

    // Lo decide el cofre que se abrió (CofreClick)
    public static TipoMinijuego Siguiente = TipoMinijuego.Rompecabezas;

    void OnMouseDown()
    {
        // No abrir mientras hay una ventana abierta o el clic fue sobre la interfaz
        if (KiboElOrigen.KiboJuego.UIAbierta)
            return;
        if (KiboElOrigen.KiboUtil.PunteroSobreUI())
            return;

        AbrirElegido();
    }

    // También lo usa el anuncio "CLICK PARA JUGAR EL MINIJUEGO"
    public void AbrirElegido()
    {
        // Evita abrir múltiples veces el minijuego
        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego)
            return;
        if (KiboElOrigen.KiboMinijuegoTransmilenio.Activo)
            return;

        // Oculta la tarjeta
        gameObject.SetActive(false);

        if (Siguiente == TipoMinijuego.Transmilenio)
            KiboElOrigen.KiboMinijuegoTransmilenio.Abrir();
        else
            IrAlRompecabezas();
    }

    public void IrAlRompecabezas()
    {
        AbrirRompecabezas(panelBlur);
    }

    public static void AbrirRompecabezas(GameObject panelBlur)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EntrarMinijuego();
        }

        if (panelBlur != null)
        {
            panelBlur.SetActive(true);
        }

        SceneManager.LoadScene("Minijuego_Rompecabezas", LoadSceneMode.Additive);
    }

    // Abre el minijuego indicado (lo usa la tarjeta 2D de los cofres)
    public static void Abrir(TipoMinijuego tipo, GameObject panelBlur)
    {
        if (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego) return;
        if (KiboElOrigen.KiboMinijuegoTransmilenio.Activo) return;
        if (tipo == TipoMinijuego.Transmilenio) KiboElOrigen.KiboMinijuegoTransmilenio.Abrir();
        else AbrirRompecabezas(panelBlur);
    }
}
