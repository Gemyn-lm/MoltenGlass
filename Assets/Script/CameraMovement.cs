using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 3f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxVerticalAngle = 85f;

    float rotationX = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if(Input.GetMouseButton(1))
        {
            HandleMouseLook();
            HandleMovement();
        }

        // Échap pour libérer la souris
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 10f;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.parent.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMoveMultiplier;

        // ZQSD (AZERTY)
        float forward = Input.GetAxisRaw("Vertical");

        float right = Input.GetAxisRaw("Horizontal");
        
        float up = Input.GetKey(KeyCode.Space) ? 1f : Input.GetKey(KeyCode.LeftShift) ? -1f : 0f;

        Vector3 move =
            transform.parent.forward * forward +
            transform.parent.right * right +
            transform.parent.up * up;

        transform.parent.position += move.normalized * speed * Time.deltaTime;
    }
}