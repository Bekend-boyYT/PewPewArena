using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using SniperGame.Gameplay;

namespace SniperGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraHolder;
        private CharacterController _characterController;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 6.0f;
        [SerializeField] private float sprintSpeed = 9.5f;
        [SerializeField] private float crouchSpeed = 3.0f;
        [SerializeField] private float acceleration = 18.0f;
        [SerializeField] private float deceleration = 22.0f;
        [SerializeField] private float airControlMultiplier = 0.4f;

        [Header("Jump & Snappy Physics")]
        [SerializeField] private float jumpForce = 6.5f;
        [SerializeField] private float baseGravity = -18.0f;
        [SerializeField] private float fallGravityMultiplier = 2.2f;

        [Header("Crouch Configuration")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float standingCameraY = 1.6f;
        [SerializeField] private float crouchCameraY = 0.9f;
        [SerializeField] private float crouchSmoothSpeed = 14.0f;

        // Runtime Velocity State
        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
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
                enabled = false;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Blokkeer beweging tijdens de aftelling van de ronde (Round Countdown)
            if (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight())
            {
                _currentHorizontalVelocity = Vector3.zero;
                return;
            }

            UpdateGroundCheck();
            HandleCrouch();
            HandleHorizontalMovement();
            HandleJumpAndGravity();
            SmoothCameraHeight();
        }

        private void UpdateGroundCheck()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -4.0f; // Drukt het karakter stevig tegen hellingen en de vloer
            }
        }

        private void HandleHorizontalMovement()
        {
            if (Keyboard.current == null) return;

            // Directe WASD polling
            float inputX = 0f;
            float inputZ = 0f;

            if (Keyboard.current.aKey.isPressed) inputX -= 1f;
            if (Keyboard.current.dKey.isPressed) inputX += 1f;
            if (Keyboard.current.sKey.isPressed) inputZ -= 1f;
            if (Keyboard.current.wKey.isPressed) inputZ += 1f;

            Vector3 moveInput = new Vector3(inputX, 0f, inputZ).normalized;

            // Snelheidskeuze op basis van sprint/crouch
            bool isSprinting = Keyboard.current.leftShiftKey.isPressed && !_isCrouching && inputZ > 0.1f;
            float targetMaxSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

            // Relatieve bewegingsrichting op basis van kijkrichting
            Vector3 targetVelocity = (transform.right * moveInput.x + transform.forward * moveInput.z) * targetMaxSpeed;

            // Snappy acceleratie & decceleratie (direct stoppen zonder uitglijden)
            float rate = moveInput.magnitude > 0.01f ? acceleration : deceleration;
            if (!_isGrounded) rate *= airControlMultiplier;

            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, rate * Time.deltaTime);

            _characterController.Move(_currentHorizontalVelocity * Time.deltaTime);
        }

        private void HandleJumpAndGravity()
        {
            if (Keyboard.current == null) return;

            // Sprong uitvoeren
            if (_isGrounded && !_isCrouching && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = jumpForce;
            }

            // Snappy Gravity: dubbele zwaartekracht tijdens het vallen (geen zweverige sprongen)
            float appliedGravity = baseGravity;
            if (_verticalVelocity < 0f)
            {
                appliedGravity *= fallGravityMultiplier;
            }

            _verticalVelocity += appliedGravity * Time.deltaTime;

            _characterController.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (Keyboard.current == null) return;

            bool crouchInput = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;

            if (crouchInput != _isCrouching)
            {
                _isCrouching = crouchInput;

                _characterController.height = _isCrouching ? crouchHeight : standingHeight;
                _characterController.center = new Vector3(0f, _characterController.height / 2f, 0f);
            }
        }

        private void SmoothCameraHeight()
        {
            if (cameraHolder == null) return;

            float targetY = _isCrouching ? crouchCameraY : standingCameraY;
            Vector3 currentPos = cameraHolder.localPosition;
            float smoothedY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * crouchSmoothSpeed);

            cameraHolder.localPosition = new Vector3(currentPos.x, smoothedY, currentPos.z);
        }

        /// <summary>
        /// Teleporteert de speler betrouwbaar naar een spawnpositie door de CharacterController kort te resetten.
        /// </summary>
        [ClientRpc]
        public void TeleportClientRpc(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            _currentHorizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.identity;
            }

            if (_characterController != null)
            {
                _characterController.enabled = true;
            }
        }
    }
}