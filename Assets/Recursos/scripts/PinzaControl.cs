//using UnityEngine;

//public class PinzaControl : MonoBehaviour
//{
//    Animator anim;

//    void Start()
//    {
//        anim = GetComponent<Animator>();
//    }

//    void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            anim.SetTrigger("Agarrar");
//        }
//    }
//}

using UnityEngine;

public class PinzaControl : MonoBehaviour
{
    Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        anim.speed = 5f; // velocidad de animaciones
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            anim.SetTrigger("Agarrar");
        }
    }
}