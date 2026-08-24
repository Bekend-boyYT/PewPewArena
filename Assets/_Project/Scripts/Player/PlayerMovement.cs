using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SniperGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("References")]
        [Tooltip("Sleep hier het CameraHolder GameObject in voor crouch-animatie.")]
        [SerializeField] private Transform cameraHolder;
        private CharacterController _characterController;

        [Header("Speeds")]
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.5f;
        [SerializeField] private float crouchSpeed = 2.5f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -19.62f;

        [Header("Crouch Settings")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float standingCameraY = 1.6f;
        [SerializeField] private float crouchCameraY = 0.9f;
        [SerializeField] private float crouchTransitionSpeed = 10f;

        // State variabelen
        private Vector3 _verticalVelocity;
        private bool _isGrounded;
        private bool _isCrouching;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false; // Schakel bewegingsupdate uit op proxies van de tegenstander
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            CheckGrounded();
            HandleCrouch();
            HandleMovement();
            HandleJumpAndGravity();
            SmoothCameraCrouch();
        }

        private void CheckGrounded()
        {
            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _verticalVelocity.y < 0f)
            {
                // Korte negatieve offset om de speler stevig op de grond te houden
                _verticalVelocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            if (Keyboard.current == null) return;

            // Input uitlezen via het nieuwe Input System
            float moveX = 0f;
            float moveZ = 0f;

            if (Keyboard.current.aKey.isPressed) moveX -= 1f;
            if (Keyboard.current.dKey.isPressed) moveX += 1f;
            if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.wKey.isPressed) moveZ += 1f;

            Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

            // Bepaal de huidige snelheid
            bool isSprinting = Keyboard.current.leftShiftKey.isPressed && !_isCrouching && inputDirection.z > 0f;
            float currentSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            // Verplaatsing relatief aan de kijkrichting van het lichaam
            Vector3 moveVector = (transform.right * inputDirection.x + transform.forward * inputDirection.z) * currentSpeed;

            _characterController.Move(moveVector * Time.deltaTime);
        }

        private void HandleJumpAndGravity()
        {
            if (Keyboard.current == null) return;

            // Springen (alleen wanneer speler op de grond staat en niet hurkt)
            if (_isGrounded && !_isCrouching && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Zwaartekracht toepassen
            _verticalVelocity.y += gravity * Time.deltaTime;
            _characterController.Move(_verticalVelocity * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (Keyboard.current == null) return;

            // Toggle of ingedrukt houden van Left Ctrl / C
            bool crouchPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;

            if (crouchPressed != _isCrouching)
            {
                _isCrouching = crouchPressed;

                // Pas de CharacterController hoogte en center aan
                _characterController.height = _isCrouching ? crouchHeight : standingHeight;
                _characterController.center = new Vector3(0f, _characterController.height / 2f, 0f);
            }
        }

        private void SmoothCameraCrouch()
        {
            if (cameraHolder == null) return;

            float targetCameraY = _isCrouching ? crouchCameraY : standingCameraY;
            Vector3 currentPos = cameraHolder.localPosition;
            float newY = Mathf.Lerp(currentPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);

            cameraHolder.localPosition = new Vector3(currentPos.x, newY, currentPos.z);
        }
    }
}