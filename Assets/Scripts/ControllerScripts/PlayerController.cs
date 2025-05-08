using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;
    public float gravityMultiplier = 2f;
    public float mouseSensitivity = 2f;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private Transform cameraTransform;
    private bool isGrounded;
    private bool canJump;
    private float rotationX = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform; // Assign camera transform
        Cursor.lockState = CursorLockMode.Locked; // Lock cursor to center
        Cursor.visible = false; // Hide cursor
    }

    void Update()
    {
        isGrounded = CheckIfGrounded();
        if (isGrounded) canJump = true;

        HandleMouseLook();

        if (Input.GetButtonDown("Jump") && canJump)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
        ApplyExtraGravity();
    }

    private void HandleMouseLook()
    {
        // Get mouse input for X and Y axes
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the player body around the Y-axis using mouseX (left-right)
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera up/down (pitch) based on mouseY
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f); // Prevent excessive pitch rotation
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    private void MovePlayer()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D -> Left/Right
        float moveZ = Input.GetAxis("Vertical");   // W/S -> Forward/Backward

        // Move the player relative to the camera's rotation
        Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
        moveDirection = moveDirection.normalized;

        if (moveDirection.magnitude > 0)
        {
            rb.AddForce(moveDirection * speed * 10f, ForceMode.Force);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        canJump = false;
    }

    private void ApplyExtraGravity()
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * gravityMultiplier * Mathf.Abs(Physics.gravity.y), ForceMode.Acceleration);
        }
    }

    private bool CheckIfGrounded()
    {
        return Physics.SphereCast(transform.position, groundCheckRadius, Vector3.down, out _, 0.1f, groundLayer);
    }
}
