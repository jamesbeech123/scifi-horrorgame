using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasicPlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    private Rigidbody rb;
    private bool jumpRequested; // Flag to store jump input.

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Capture Jump input here.
        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        // Get movement input.
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 movement = transform.TransformDirection(new Vector3(moveX, 0, moveZ)) * speed;
        
        // Preserve vertical velocity from physics.
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        
        // If jump was requested and the player is grounded, apply jump.
        if (jumpRequested && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
        jumpRequested = false;
    }
    
    // A basic ground check using a raycast downwards.
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f);
    }
}
