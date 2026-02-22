using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float mouseSensitivity = 0.2f;

    [Header("Salto Interplanetario")]
    [SerializeField] private float jumpForce = 60f;

    [Header("Interacción")]
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask interactableLayer;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Rigidbody rb;
    private GravityBody gravityBody;
    private float cameraPitch = 0f;
    private RockInteraction currentRock;
    private ProceduralPlanet[] allPlanets; // Actualizado a ProceduralPlanet

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gravityBody = GetComponent<GravityBody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null) cameraTransform = Camera.main.transform;

        // Actualizado para buscar planetas procedurales
        allPlanets = FindObjectsByType<ProceduralPlanet>(FindObjectsSortMode.None);
    }

    void Update()
    {
        HandleInteractionRaycast();
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            ProceduralPlanet currentPlanet = gravityBody.planet; // Usamos la nueva variable 'planet'
            ProceduralPlanet targetPlanet = allPlanets.FirstOrDefault(p => p != currentPlanet);

            if (targetPlanet != null)
            {
                gravityBody.planet = targetPlanet;

                Vector3 direction = (targetPlanet.transform.position - transform.position).normalized;

                rb.linearVelocity = direction * jumpForce;

                Debug.Log("¡Salto rápido al planeta: " + targetPlanet.name + "!");
            }
        }
    }

    void HandleInteractionRaycast()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            RockInteraction rock = hit.collider.GetComponent<RockInteraction>();

            if (rock != null)
            {
                if (currentRock != rock)
                {
                    if (currentRock != null) currentRock.ToggleHighlight(false);
                    currentRock = rock;
                    currentRock.ToggleHighlight(true);
                }
                return;
            }
        }

        if (currentRock != null)
        {
            currentRock.ToggleHighlight(false);
            currentRock = null;
        }
    }

    void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    void OnAttack(InputValue value)
    {
        if (value.isPressed && currentRock != null)
        {
            currentRock.Interact();
            currentRock = null;
        }
    }

    void LateUpdate()
    {
        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);
    }

    void FixedUpdate()
    {
        Vector3 targetMove = transform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y).normalized);
        rb.MovePosition(rb.position + targetMove * moveSpeed * Time.fixedDeltaTime);
    }
}