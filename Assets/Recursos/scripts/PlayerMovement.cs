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

    [Header("Suelo (movimiento suave en cualquier terreno)")]
    [Tooltip("Mantiene al personaje pegado al suelo: sube y baja lomas y escalones sin trabarse")]
    public bool pegarAlSuelo = true;
    [Tooltip("Altura máxima de escalón/borde que puede subir caminando (metros)")]
    public float pasoMaximo = 0.5f;
    [Tooltip("Fuerza con la que cae si no hay suelo debajo")]
    public float gravedad = 20f;

    private Rigidbody rb;
    private Collider col;
    private float alturaPies;     // distancia del pivote a la planta de los pies
    private float velocidadY;
    private Quaternion rotacionObjetivo;
    private bool girar;

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
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rotacionObjetivo = rb.rotation;

        // Sin fricción: ya no se queda pegado al rozar paredes, árboles o bordes
        var sinFriccion = new PhysicsMaterial("Kibo_SinFriccion")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;
            c.sharedMaterial = sinFriccion;
            if (col == null) col = c;
        }
        if (col != null) alturaPies = transform.position.y - col.bounds.min.y;
    }

    void Update()
    {
        // Quieto mientras hay una ventana abierta (clasificar, pausa, ¡ganaste!)
        if (KiboElOrigen.KiboJuego.UIAbierta || (GameManager.Instance != null && GameManager.Instance.estaEnMinijuego))
        {
            x = 0;
            z = 0;
            return;
        }

        // Captura de input (SIEMPRE en Update)
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");

        Vector3 movement = Direccion();

        // ROTACIÓN SUAVE (se aplica en FixedUpdate con la física para que no tiemble)
        girar = movement.magnitude > 0.1f;
        if (girar)
        {
            rotacionObjetivo = Quaternion.LookRotation(movement);

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

        Vector3 vel = movement * currentSpeed;

        // Pegado al suelo: sigue lomas y pequeños escalones sin trabarse
        if (pegarAlSuelo && col != null)
        {
            float dt = Time.fixedDeltaTime;
            float pies = rb.position.y - alturaPies;
            float sueloY;
            if (BuscarSuelo(rb.position + movement * 0.1f, pies, out sueloY))
            {
                float dif = sueloY - pies;
                if (dif > -0.08f && dif <= pasoMaximo)
                {
                    // en el suelo (o subiendo un escalón pequeño)
                    velocidadY = 0f;
                    vel.y = Mathf.Clamp(dif * 15f, -8f, 8f);
                }
                else if (dif <= -0.08f)
                {
                    // hay un desnivel: cae hasta el suelo
                    velocidadY -= gravedad * dt;
                    vel.y = Mathf.Max(velocidadY, dif / dt);
                }
                else
                {
                    vel.y = 0f; // es una pared: la colisión la detiene
                }
            }
            else
            {
                velocidadY -= gravedad * dt;
                vel.y = Mathf.Max(velocidadY, -30f);
            }
        }

        // Movimiento físico fluido
        rb.linearVelocity = vel;

        if (girar)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivo, rotationSpeed * Time.fixedDeltaTime));
    }

    bool BuscarSuelo(Vector3 posicion, float pies, out float sueloY)
    {
        sueloY = 0f;
        Vector3 origen = new Vector3(posicion.x, pies + pasoMaximo + 0.3f, posicion.z);
        var hits = Physics.RaycastAll(origen, Vector3.down, pasoMaximo + 4f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        bool hay = false;
        foreach (var h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;          // el propio personaje
            if (h.collider.CompareTag("ObjetoAgarrable")) continue;  // residuos pequeños
            if (h.distance < mejor) { mejor = h.distance; sueloY = h.point.y; hay = true; }
        }
        return hay;
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

