using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 4f;
    public float superSpeed = 8f;

    [Header("Rotacion")]
    public float rotationSpeed = 10f;

    [Header("Inclinacion")]
    public float forwardTilt = 10f;

    [Header("Referencia visual")]
    public Transform model;

    [Header("Cámara")]
    [Tooltip("W camina hacia donde mira la cámara (útil al girar la cámara o en primera persona)")]
    public bool relativoACamara = true;

    private Rigidbody rb;

    // INPUT
    private float x;
    private float z;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Configuración física estable
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // SUAVIDAD VISUAL 🔥
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // Quieto mientras hay una ventana abierta (clasificar, pausa, ¡ganaste!)
        if (KiboElOrigen.KiboJuego.UIAbierta)
        {
            x = 0;
            z = 0;
            return;
        }

        // Captura de input (SIEMPRE en Update)
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");

        Vector3 movement = Direccion();

        // ROTACIÓN SUAVE
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            if (model != null)
            {
                model.localRotation = Quaternion.Slerp(
                    model.localRotation,
                    Quaternion.Euler(forwardTilt, 0, 0),
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            if (model != null)
            {
                model.localRotation = Quaternion.Slerp(
                    model.localRotation,
                    Quaternion.identity,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // Dirección del movimiento en el mundo según la cámara
    Vector3 Direccion()
    {
        Vector3 m = new Vector3(x, 0, z);
        Camera cam = Camera.main;
        if (relativoACamara && cam != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0;
            Vector3 r = cam.transform.right; r.y = 0;
            if (f.sqrMagnitude > 0.001f && r.sqrMagnitude > 0.001f)
                m = f.normalized * z + r.normalized * x;
        }
        return m.sqrMagnitude > 1f ? m.normalized : m;
    }

    void FixedUpdate()
    {
        Vector3 movement = Direccion();

        float currentSpeed = (movement.magnitude > 0 && Input.GetKey(KeyCode.Space))
            ? superSpeed
            : speed;

        // Movimiento físico fluido
        rb.linearVelocity = movement * currentSpeed;
    }

    public bool EstaQuieto()
    {
        Vector3 movement = new Vector3(x, 0, z).normalized;

        return movement.magnitude < 0.1f;
    }

}

//using UnityEngine;

//public class PlayerMovement : MonoBehaviour
//{
//    [Header("Movimiento")]
//    public float speed = 5f;
//    public float superSpeed = 12f;

//    [Header("Rotacion")]
//    public float rotationSpeed = 10f;

//    [Header("Inclinacion")]
//    public float forwardTilt = 10f;

//    [Header("Referencia visual")]
//    public Transform model; // arrastra aquí el modelo visual de Kibo

//    private Rigidbody rb;

//    void Start()
//    {
//        rb = GetComponent<Rigidbody>();
//    }

//    void FixedUpdate()
//    {
//        float x = Input.GetAxis("Horizontal");
//        float z = Input.GetAxis("Vertical");

//        Vector3 movement = new Vector3(x, 0, z).normalized;

//        float currentSpeed = speed;

//        if (movement.magnitude > 0 && Input.GetKey(KeyCode.Space))
//        {
//            currentSpeed = superSpeed;
//        }

//        // MOVIMIENTO CON FÍSICA (respeta colliders)
//        Vector3 newPosition = rb.position + movement * currentSpeed * Time.fixedDeltaTime;
//        rb.MovePosition(newPosition);

//        // ROTACIÓN DEL PERSONAJE
//        if (movement.magnitude > 0.1f)
//        {
//            Quaternion targetRotation = Quaternion.LookRotation(movement);

//            transform.rotation = Quaternion.Slerp(
//                transform.rotation,
//                targetRotation,
//                rotationSpeed * Time.fixedDeltaTime
//            );

//            // INCLINACIÓN VISUAL DEL MODELO
//            if (model != null)
//            {
//                model.localRotation = Quaternion.Slerp(
//                    model.localRotation,
//                    Quaternion.Euler(forwardTilt, 0, 0),
//                    rotationSpeed * Time.fixedDeltaTime
//                );
//            }
//        }
//        else
//        {
//            // VOLVER NORMAL
//            if (model != null)
//            {
//                model.localRotation = Quaternion.Slerp(
//                    model.localRotation,
//                    Quaternion.Euler(0, 0, 0),
//                    rotationSpeed * Time.fixedDeltaTime
//                );
//            }
//        }
//    }
//}

//using UnityEngine;

//public class PlayerMovement : MonoBehaviour
//{
//    [Header("Movimiento")]
//    public float speed = 5f;
//    public float superSpeed = 12f;

//    [Header("Rotacion")]
//    public float rotationSpeed = 10f;

//    [Header("Inclinacion")]
//    public float forwardTilt = 10f;

//    [Header("Referencia visual")]
//    public Transform model; // ← arrastra aquí el modelo de Kibo

//    void Update()
//    {
//        float x = Input.GetAxis("Horizontal");
//        float z = Input.GetAxis("Vertical");

//        Vector3 movement = new Vector3(x, 0, z).normalized;

//        float currentSpeed = speed;

//        if (movement.magnitude > 0 && Input.GetKey(KeyCode.Space))
//        {
//            currentSpeed = superSpeed;
//        }

//        // Movimiento del personaje
//        transform.Translate(movement * currentSpeed * Time.deltaTime, Space.World);

//        // Rotación del personaje (NO inclinación)
//        if (movement.magnitude > 0.1f)
//        {
//            Quaternion targetRotation = Quaternion.LookRotation(movement);

//            transform.rotation = Quaternion.Slerp(
//                transform.rotation,
//                targetRotation,
//                rotationSpeed * Time.deltaTime
//            );

//            // 👉 INCLINACIÓN SOLO DEL MODELO
//            model.localRotation = Quaternion.Slerp(
//                model.localRotation,
//                Quaternion.Euler(forwardTilt, 0, 0),
//                rotationSpeed * Time.deltaTime
//            );
//        }
//        else
//        {
//            // Volver a posición normal
//            model.localRotation = Quaternion.Slerp(
//                model.localRotation,
//                Quaternion.Euler(0, 0, 0),
//                rotationSpeed * Time.deltaTime
//            );
//        }
//    }
//}

