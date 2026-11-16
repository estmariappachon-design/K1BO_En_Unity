//using UnityEngine;

//public class CrashCamera : MonoBehaviour
//{
//    public Transform target;

//    [Header("Offset fijo")]
//    public Vector3 offset = new Vector3(0f, 5f, -9f);

//    [Header("Suavizado")]
//    public float smoothSpeed = 5f;

//    void LateUpdate()
//    {
//        if (target == null) return;

//        // Solo seguir posición
//        Vector3 desiredPosition = target.position + offset;

//        transform.position = Vector3.Lerp(
//            transform.position,
//            desiredPosition,
//            smoothSpeed * Time.deltaTime
//        );

//        // Rotación FIJA (quieta)
//        transform.rotation = Quaternion.Euler(30f, 0f, 0f);
//    }
//}

using UnityEngine;

public class CrashCamera : MonoBehaviour
{
    public Transform target;

    public Vector3 offset = new Vector3(0, 5, -7);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(target);
    }
}