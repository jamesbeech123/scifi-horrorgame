using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;           // Reference to the player transform (should be set in Inspector)
    public float mouseSensitivity = 100f;
    
    private float xRotation = 0f;        // For vertical (pitch) rotation

    void Start()
    {
        // Lock the cursor for FPS control
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Optionally, replace Update() with LateUpdate() for smoother synchronization with the rigidbody.
    void LateUpdate()
    {
        // Get mouse movement
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Vertical rotation (clamped)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal rotation: rotate the player object so movement aligns with camera direction
        if (player)
        {
            player.Rotate(Vector3.up * mouseX);
        }
    }
}
