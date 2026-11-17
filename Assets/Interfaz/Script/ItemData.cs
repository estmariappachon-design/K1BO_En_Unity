using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Inventario/Item")]
public class ItemData : ScriptableObject
{
    public string nombreItem;
    public Sprite icono;

    [Header("Reciclaje (Kibo el Origen)")]
    [Tooltip("Nombre bonito que aparece en los mensajes. Si lo dejas vacío se genera solo.")]
    public string nombreVisible;

    [Tooltip("Marca esto para elegir tú la caneca. Si no, se detecta por el nombre del residuo.")]
    public bool canecaManual;

    [Tooltip("Caneca correcta para este residuo (solo si 'Caneca Manual' está marcado)")]
    public KiboElOrigen.TipoCaneca caneca;

    [Tooltip("Frase corta que explica por qué va en esa caneca")]
    [TextArea(1, 3)]
    public string explicacion;
}
