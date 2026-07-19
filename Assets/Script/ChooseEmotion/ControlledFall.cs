using UnityEngine;

public class ControlledFall : MonoBehaviour
{
    [SerializeField] private float gravityScale = 0.5f;
    [SerializeField] private float maxFallSpeed = 3f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        rb.AddForce(
            Physics.gravity * (gravityScale - 1f),
            ForceMode.Acceleration
        );

        Vector3 velocity = rb.linearVelocity;

        if (velocity.y < -maxFallSpeed)
        {
            velocity.y = -maxFallSpeed;
            rb.linearVelocity = velocity;
        }
    }
}