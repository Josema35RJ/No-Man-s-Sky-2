using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Motores y Velocidad")]
    public float thrustSpeed = 500f; // Fuerza de empuje hacia adelante
    public float boostMultiplier = 5f; // Multiplicador al usar el turbo (Shift)
    
    [Header("Maniobrabilidad (Rotación)")]
    public float pitchSpeed = 100f;  // Arriba/Abajo (Ratón Y)
    public float yawSpeed = 100f;    // Izquierda/Derecha (Ratón X)
    public float rollSpeed = 100f;   // Rotar sobre sí mismo (A y D)

    private Rigidbody rb;
    private float activeForwardSpeed;
    private Vector2 lookInput;
    private float rollInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // En el espacio no queremos gravedad estándar ni que la nave caiga como una piedra
        rb.useGravity = false; 
        // Añadimos un poco de fricción espacial ("drag") para que la nave frene al soltar el acelerador
        rb.linearDamping = 1.5f; 
        rb.angularDamping = 2.0f;
        
        // Ocultar el cursor del ratón para pilotar cómodamente
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Controles de Rotación (Ratón y Teclado)
        lookInput.x = Input.GetAxis("Mouse X"); // Guiñada (Yaw)
        lookInput.y = Input.GetAxis("Mouse Y"); // Cabeceo (Pitch)
        
        // Alabeo (Roll) con las teclas A y D
        rollInput = Input.GetAxis("Horizontal"); 

        // 2. Acelerador (W y S)
        float thrustInput = Input.GetAxis("Vertical");
        
        // Si pulsamos Shift, metemos el turbo
        float currentThrust = thrustSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentThrust *= boostMultiplier;
        }

        // Interpolamos suavemente la velocidad para que no sea un tirón brusco
        activeForwardSpeed = Mathf.Lerp(activeForwardSpeed, thrustInput * currentThrust, Time.deltaTime * 3f);
    }

    void FixedUpdate()
    {
        // APLICAR FÍSICAS DE VUELO
        
        // 1. Aplicar rotación a la nave
        rb.AddRelativeTorque(
            -lookInput.y * pitchSpeed * Time.fixedDeltaTime, 
            lookInput.x * yawSpeed * Time.fixedDeltaTime, 
            -rollInput * rollSpeed * Time.fixedDeltaTime, 
            ForceMode.Acceleration
        );

        // 2. Aplicar empuje hacia adelante
        rb.AddRelativeForce(Vector3.forward * activeForwardSpeed, ForceMode.Acceleration);
    }
}