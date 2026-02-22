using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    [Header("Planeta que atrae al jugador")]
    public ProceduralPlanet planet; // Nombrado en inglés para conectar con tus otros scripts

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (planet != null)
        {
            planet.Attract(this.transform);
        }
    }
}