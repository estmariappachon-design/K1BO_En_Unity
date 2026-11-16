using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimacionesOjos : MonoBehaviour
{
    [Header("Imagen UI")]
    public Image imagenOjos;

    [Header("Animación Dormir")]
    public Sprite[] framesDormir;

    [Header("Animación Despertar")]
    public Sprite[] framesDespertar;

    [Header("Loop ZZZ")]
    public Sprite[] framesLoop;

    public Sprite spriteIdle;

    [Header("Velocidad")]
    public float velocidadDormir = 0.04f;
    public float velocidadLoop = 1.5f;

    private Coroutine loopCoroutine;

    public void Dormir()
    {
        StopAllCoroutines();
        StartCoroutine(ReproducirDormir());
    }

    public void Despertar()
    {
        StopAllCoroutines();
        StartCoroutine(ReproducirDespertar());
    }

    IEnumerator ReproducirDormir()
    {
        foreach (Sprite frame in framesDormir)
        {
            imagenOjos.sprite = frame;
            yield return new WaitForSeconds(velocidadDormir);
        }

        loopCoroutine = StartCoroutine(LoopDormido());
    }

    IEnumerator LoopDormido()
    {
        while (true)
        {
            foreach (Sprite frame in framesLoop)
            {
                imagenOjos.sprite = frame;
                yield return new WaitForSeconds(velocidadLoop);
            }
        }
    }

    IEnumerator ReproducirDespertar()
    {
        foreach (Sprite frame in framesDespertar)
        {
            imagenOjos.sprite = frame;
            yield return new WaitForSeconds(velocidadDormir);
        }

        imagenOjos.sprite = spriteIdle;
    }
}