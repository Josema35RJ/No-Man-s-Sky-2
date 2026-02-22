using UnityEngine;

public class SphericalGravity : MonoBehaviour
{
    public Transform planet; 
    public float gravityStrength = -9.81f;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        if (planet)
        {
            Vector3 gravityUp = (transform.position - planet.position).normalized;
            Vector3 localUp = transform.up;

            rb.AddForce(gravityUp * gravityStrength);

            Quaternion targetRotation = Quaternion.FromToRotation(localUp, gravityUp) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50f * Time.deltaTime);
        }
    }
}