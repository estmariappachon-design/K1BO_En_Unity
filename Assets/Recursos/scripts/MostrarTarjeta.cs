using UnityEngine;

public class MostrarTarjeta : MonoBehaviour
{
    public GameObject tarjeta;
    public Transform camaraJugador;

    public Vector3 offset = new Vector3(0, 0, 2f);

    public void Mostrar()
    {
        tarjeta.SetActive(true);

        // posicionar frente a la cámara
        tarjeta.transform.position =
            camaraJugador.position +
            camaraJugador.forward * offset.z;

        // mirar hacia la cámara
        tarjeta.transform.LookAt(camaraJugador);

        // girarla correctamente
        tarjeta.transform.Rotate(0, 180, 0);
    }
}