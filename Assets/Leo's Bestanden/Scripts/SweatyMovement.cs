using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float acceleration = 40f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float friction = 8f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    private CharacterController controller;

    private Vector3 velocity;
    private float verticalVelocity;
    private float cameraRotation;

    private bool HasLocalControl => !IsSpawned || IsOwner;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
            {
                cameraTransform = childCamera.transform;
            }
        }
    }

    private void Start()
    {
        if (HasLocalControl)
        {
            LockCursor();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (HasLocalControl)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        if (!HasLocalControl)
            return;

        HandleCursorLockInput();

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleCursorLockInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection =
            transform.right * horizontal +
            transform.forward * vertical;

        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        Vector3 targetVelocity = inputDirection * walkSpeed;

        float currentAcceleration;

        if (controller.isGrounded)
        {
            currentAcceleration = acceleration;
        }
        else
        {
            currentAcceleration = airAcceleration;
        }

        velocity = Vector3.MoveTowards(
            velocity,
            targetVelocity,
            currentAcceleration * Time.deltaTime
        );

        if (inputDirection.magnitude < 0.1f && controller.isGrounded)
        {
            velocity = Vector3.MoveTowards(
                velocity,
                Vector3.zero,
                friction * Time.deltaTime
            );
        }

        // Jump
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 movement = velocity;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX;
        float mouseY;

        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            mouseX = mouseDelta.x * mouseSensitivity;
            mouseY = mouseDelta.y * mouseSensitivity;
        }
        else
        {
            // Fallback for legacy input handling.
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        transform.Rotate(Vector3.up * mouseX);

        cameraRotation -= mouseY;

        cameraRotation = Mathf.Clamp(
            cameraRotation,
            -maxLookAngle,
            maxLookAngle
        );

        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(
                    cameraRotation,
                    0f,
                    0f
                );
        }
    }

    // Used by ArenaSpawnManager to teleport players.
    [ClientRpc]
    public void TeleportClientRpc(Vector3 position)
    {
        controller.enabled = false;

        transform.position = position;

        velocity = Vector3.zero;
        verticalVelocity = 0f;

        controller.enabled = true;
    }
}