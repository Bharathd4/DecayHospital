
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float gravity = -20f;

    [Header("Jump")]
    public float jumpHeight = 1.5f;

    [Header("Camera Look")]
    public Transform cameraHolder;
    public float mouseSensitivity = 1.2f;
    public float touchSensitivity = 0.12f;
    public float lookLimit = 80f;

    private CharacterController controller;
    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (cameraHolder == null)
        {
            Debug.LogError("Assign CameraHolder in FPSMovement.");
            enabled = false;
            return;
        }

        pitch = cameraHolder.localEulerAngles.x;

        if (pitch > 180f)
            pitch -= 360f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleCameraLook();
        HandleCursor();
    }

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector2 mobileMove = MobileInput.MoveInput;

        if (mobileMove.sqrMagnitude > 0f)
        {
            x = mobileMove.x;
            z = mobileMove.y;
        }

        Vector3 direction =
            transform.right * x + transform.forward * z;

        direction = Vector3.ClampMagnitude(direction, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;

            if (Input.GetKeyDown(KeyCode.Space) ||
                MobileInput.ConsumeJump())
            {
                verticalVelocity =
                    Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            MobileInput.ConsumeJump();
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity =
            direction * moveSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleCameraLook()
    {
        // Mobile right-side touch rotation
        Vector2 touchDelta =
            MobileLookAreaTouch.ConsumeLookDelta();

        if (touchDelta.sqrMagnitude > 0f)
        {
            ApplyLook(
                touchDelta.x * touchSensitivity,
                touchDelta.y * touchSensitivity
            );
        }

        // Existing PC mouse rotation
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX =
                Input.GetAxis("Mouse X") * mouseSensitivity;

            float mouseY =
                Input.GetAxis("Mouse Y") * mouseSensitivity;

            ApplyLook(mouseX, mouseY);
        }
    }

    private void ApplyLook(float lookX, float lookY)
    {
        transform.Rotate(0f, lookX, 0f);

        pitch -= lookY;
        pitch = Mathf.Clamp(pitch, -lookLimit, lookLimit);

        cameraHolder.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
