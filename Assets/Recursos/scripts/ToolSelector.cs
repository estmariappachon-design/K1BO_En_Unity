//using UnityEngine;
//using System.Collections;

//public class ToolSelector : MonoBehaviour
//{
//    public Animator animator;

//    int herramientaActual = 0;

//    public void SelectTool(int nuevaHerramienta)
//    {
//        StopAllCoroutines();
//        StartCoroutine(CambiarHerramienta(nuevaHerramienta));
//    }

//    IEnumerator CambiarHerramienta(int nuevaHerramienta)
//    {
//        animator.ResetTrigger("Tool1");
//        animator.ResetTrigger("Tool2");
//        animator.ResetTrigger("Tool3");
//        animator.ResetTrigger("Tool4");

//        animator.ResetTrigger("Guardar1");
//        animator.ResetTrigger("Guardar2");
//        animator.ResetTrigger("Guardar3");
//        animator.ResetTrigger("Guardar4");

//        // quitar herramienta actual
//        if (herramientaActual != 0)
//        {
//            animator.SetTrigger("Guardar" + herramientaActual);

//            yield return new WaitForSeconds(1.83f);
//        }

//        // poner nueva
//        animator.SetTrigger("Tool" + nuevaHerramienta);

//        herramientaActual = nuevaHerramienta;
//    }
//}

//using UnityEngine;
//using System.Collections;

//public class ToolSelector : MonoBehaviour
//{
//    public Animator animator;

//    int herramientaActual = 0;

//    void Update()
//    {
//        // Click izquierdo para usar herramienta
//        if (Input.GetMouseButtonDown(0))
//        {
//            UsarHerramienta();
//        }
//    }

//    public void SelectTool(int nuevaHerramienta)
//    {
//        StopAllCoroutines();
//        StartCoroutine(CambiarHerramienta(nuevaHerramienta));
//    }

//    IEnumerator CambiarHerramienta(int nuevaHerramienta)
//    {
//        // limpiar triggers de poner herramienta
//        animator.ResetTrigger("Tool1");
//        animator.ResetTrigger("Tool2");
//        animator.ResetTrigger("Tool3");
//        //animator.ResetTrigger("Tool4");

//        // limpiar triggers de guardar herramienta
//        animator.ResetTrigger("Guardar1");
//        animator.ResetTrigger("Guardar2");
//        animator.ResetTrigger("Guardar3");
//       // animator.ResetTrigger("Guardar4");

//        // limpiar triggers de usar herramienta
//        animator.ResetTrigger("Usar1");
//        animator.ResetTrigger("Usar2");
//        //animator.ResetTrigger("Usar3");
//        //animator.ResetTrigger("Usar4");

//        // guardar herramienta actual
//        if (herramientaActual != 0)
//        {
//            animator.SetTrigger("Guardar" + herramientaActual);

//            // duración de animación guardar
//            yield return new WaitForSeconds(1.83f);
//        }

//        // sacar nueva herramienta
//        animator.SetTrigger("Tool" + nuevaHerramienta);

//        herramientaActual = nuevaHerramienta;
//    }

//    void UsarHerramienta()
//    {
//        // si no hay herramienta equipada
//        if (herramientaActual == 0)
//            return;

//        animator.SetTrigger("Usar" + herramientaActual);
//    }
//}

using UnityEngine;
using System.Collections;

public class ToolSelector : MonoBehaviour
{
    public Animator animator;

    // Objetos de herramientas
    public GameObject tool1;
    public GameObject tool2;
    public GameObject tool3;

    int herramientaActual = 0;
    bool cambiandoHerramienta = false;

    void Update()
    {
        // Click izquierdo para usar herramienta
        if (Input.GetMouseButtonDown(0))
        {
            UsarHerramienta();
        }
    }

    public void SelectTool(int nuevaHerramienta)
    {
        // evitar errores mientras cambia
        if (cambiandoHerramienta)
            return;

        // evitar seleccionar la misma herramienta
        if (herramientaActual == nuevaHerramienta)
            return;

        StopAllCoroutines();
        StartCoroutine(CambiarHerramienta(nuevaHerramienta));
    }

    IEnumerator CambiarHerramienta(int nuevaHerramienta)
    {
        cambiandoHerramienta = true;

        // limpiar triggers de poner herramienta
        animator.ResetTrigger("Tool1");
        animator.ResetTrigger("Tool2");
        animator.ResetTrigger("Tool3");

        // limpiar triggers de guardar herramienta
        animator.ResetTrigger("Guardar1");
        animator.ResetTrigger("Guardar2");
        animator.ResetTrigger("Guardar3");

        // limpiar triggers de usar herramienta
        animator.ResetTrigger("Usar1");
        animator.ResetTrigger("Usar2");
        //animator.ResetTrigger("Usar3");

        // GUARDAR herramienta actual
        if (herramientaActual != 0)
        {
            animator.SetTrigger("Guardar" + herramientaActual);

            // esperar animación de guardar
            yield return new WaitForSeconds(1.83f);

            // apagar herramienta actual
            if (herramientaActual == 1)
                tool1.SetActive(false);

            if (herramientaActual == 2)
                tool2.SetActive(false);

            if (herramientaActual == 3)
                tool3.SetActive(false);
        }

        // ACTIVAR nueva herramienta
        if (nuevaHerramienta == 1)
            tool1.SetActive(true);

        if (nuevaHerramienta == 2)
            tool2.SetActive(true);

        if (nuevaHerramienta == 3)
            tool3.SetActive(true);

        // animación sacar herramienta
        animator.SetTrigger("Tool" + nuevaHerramienta);

        // esperar animación
        yield return new WaitForSeconds(1.0f);

        herramientaActual = nuevaHerramienta;

        cambiandoHerramienta = false;
    }

    void UsarHerramienta()
    {
        // si no hay herramienta equipada
        if (herramientaActual == 0)
            return;

        // evitar usar mientras cambia
        if (cambiandoHerramienta)
            return;

        // limpiar triggers
        animator.ResetTrigger("Usar1");
        animator.ResetTrigger("Usar2");
        //animator.ResetTrigger("Usar3");

        // usar herramienta actual
        animator.SetTrigger("Usar" + herramientaActual);
    }
}