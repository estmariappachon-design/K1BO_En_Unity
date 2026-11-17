using UnityEngine;

namespace KiboElOrigen
{
    // Componente de la versión anterior (ya no se usa).
    // Los residuos ahora se reconocen por su ItemData (ItemRecolectable) y se clasifican en la ventana de las canecas.
    // "Kibo el Origen > Crear Todo" lo quita de la escena automáticamente.
    [AddComponentMenu("")]
    public class Residuo : MonoBehaviour
    {
        public string nombre = "Residuo";
        public TipoCaneca tipo = TipoCaneca.Blanca;
    }
}
