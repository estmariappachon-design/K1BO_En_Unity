using UnityEngine;
using System.Collections;

public class InsigniaUI : MonoBehaviour
{
    public GameObject insignia;

    public void MostrarInsignia()
    {
        StartCoroutine(MostrarTemporalmente());
    }

    IEnumerator MostrarTemporalmente()
    {
        insignia.SetActive(true);

        yield return new WaitForSeconds(3f);

        insignia.SetActive(false);
    }
}