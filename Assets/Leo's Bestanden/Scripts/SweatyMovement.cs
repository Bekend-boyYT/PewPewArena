using UnityEngine;
using Unity.Netcode;

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

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        // Only the local player controls their own movement and camera.
        if (!IsOwner)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Do not control other players.
        if (!IsOwner)
            return;

        HandleLook();
        HandleMovement();
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
        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraRotation -= mouseY;

        cameraRotation = Mathf.Clamp(
            cameraRotation,
            -maxLookAngle,
            maxLookAngle
        );

        cameraTransform.localRotation =
            Quaternion.Euler(
                cameraRotation,
                0f,
                0f
            );
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