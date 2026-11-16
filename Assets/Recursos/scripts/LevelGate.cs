using UnityEngine;

public class LevelGate : MonoBehaviour
{
    public GameObject barreraVisual;
    public Collider barreraCollider;

    public int basuraActual = 0;
    public int cofresActual = 0;

    public int basuraNecesaria = 3;
    public int cofresNecesarios = 3;

    public void RecogerBasura()
    {
        basuraActual++;
        RevisarPuerta();
    }

    public void AbrirCofre()
    {
        cofresActual++;
        RevisarPuerta();
    }

    void RevisarPuerta()
    {
        if (basuraActual >= basuraNecesaria &&
            cofresActual >= cofresNecesarios)
        {
            barreraVisual.SetActive(false);
            barreraCollider.enabled = false;
        }
    }
}