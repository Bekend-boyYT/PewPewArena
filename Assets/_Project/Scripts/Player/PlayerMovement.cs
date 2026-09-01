using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using SniperGame.Gameplay;
using SniperGame.UI;

namespace SniperGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private AudioSource footstepAudioSource;
        private CharacterController _characterController;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 6.0f;
        [SerializeField] private float sprintSpeed = 9.5f;
        [SerializeField] private float crouchSpeed = 3.0f;
        [SerializeField] private float acceleration = 18.0f;
        [SerializeField] private float deceleration = 22.0f;
        [SerializeField] private float airControlMultiplier = 0.4f;

        [Header("Stamina Configuration")]
        [SerializeField] private float maxStamina = 100.0f;
        [SerializeField] private float staminaDrainRate = 28.0f; // Verbruikt stamina in ~3.5 seconden
        [SerializeField] private float staminaRegenRate = 22.0f; // Herstelt in ~4.5 seconden
        [SerializeField] private float regenDelay = 1.0f;        // Wachttijd voor herstel start

        [Header("Jump & Physics")]
        [SerializeField] private float jumpForce = 6.5f;
        [SerializeField] private float baseGravity = -18.0f;
        [SerializeField] private float fallGravityMultiplier = 2.2f;

        [Header("Crouch Configuration")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float standingCameraY = 1.6f;
        [SerializeField] private float crouchCameraY = 0.9f;
        [SerializeField] private float crouchSmoothSpeed = 14.0f;

        [Header("Footstep Audio Settings")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float walkStepInterval = 0.45f;
        [SerializeField] private float sprintStepInterval = 0.30f;

        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
        private bool _isGrounded;
        private bool _isCrouching;
        private float _stepTimer;

        private float _currentStamina;
        private float _regenTimer;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _currentStamina = maxStamina;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _currentStamina = maxStamina;
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
            }
        }

private void Update()
        {
            if (!IsOwner) return;

            // Blokkeer beweging tijdens countdown, pauzemenu of na matcheinde
            if (PauseMenu.IsPaused || (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight()))
            {
                _currentHorizontalVelocity = Vector3.zero;
                return;
            }

            UpdateGroundCheck();
            HandleCrouch();
            HandleHorizontalMovement();
            HandleJumpAndGravity();
            SmoothCameraHeight();
            HandleFootsteps();
        }

        private void UpdateGroundCheck()
        {
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -4.0f;
            }
        }

        private void HandleHorizontalMovement()
        {
            if (Keyboard.current == null) return;

            float inputX = 0f;
            float inputZ = 0f;

            if (Keyboard.current.aKey.isPressed) inputX -= 1f;
            if (Keyboard.current.dKey.isPressed) inputX += 1f;
            if (Keyboard.current.sKey.isPressed) inputZ -= 1f;
            if (Keyboard.current.wKey.isPressed) inputZ += 1f;

            Vector3 moveInput = new Vector3(inputX, 0f, inputZ).normalized;
            bool isMoving = moveInput.magnitude > 0.01f;

            // Sprint check: Alleen sprinten als er stamina is en je voorwaarts beweegt
            bool wantsToSprint = Keyboard.current.leftShiftKey.isPressed && !_isCrouching && inputZ > 0.1f;
            bool isSprinting = wantsToSprint && _currentStamina > 0f && isMoving;

            // Stamina verbruik & herstel logica
            if (isSprinting)
            {
                _currentStamina = Mathf.Max(0f, _currentStamina - staminaDrainRate * Time.deltaTime);
                _regenTimer = regenDelay;
            }
            else
            {
                if (_regenTimer > 0f)
                {
                    _regenTimer -= Time.deltaTime;
                }
                else
                {
                    _currentStamina = Mathf.Min(maxStamina, _currentStamina + staminaRegenRate * Time.deltaTime);
                }
            }

            // Werk de Stamina UI bij
            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
            }

            float targetMaxSpeed = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
            Vector3 targetVelocity = (transform.right * moveInput.x + transform.forward * moveInput.z) * targetMaxSpeed;

            float rate = isMoving ? acceleration : deceleration;
            if (!_isGrounded) rate *= airControlMultiplier;

            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, rate * Time.deltaTime);
            _characterController.Move(_currentHorizontalVelocity * Time.deltaTime);
        }

        private void HandleFootsteps()
        {
            if (!_isGrounded || _isCrouching || _currentHorizontalVelocity.magnitude < 1.0f)
            {
                return;
            }

            bool isSprinting = _currentHorizontalVelocity.magnitude > (walkSpeed + 1.0f);
            float currentInterval = isSprinting ? sprintStepInterval : walkStepInterval;

            _stepTimer += Time.deltaTime;

            if (_stepTimer >= currentInterval)
            {
                _stepTimer = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;

            int randomIndex = Random.Range(0, footstepClips.Length);
            PlayFootstepClientRpc(randomIndex);
        }

        [ClientRpc]
        private void PlayFootstepClientRpc(int clipIndex)
        {
            if (footstepAudioSource == null || footstepClips == null || clipIndex >= footstepClips.Length) return;

            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.PlayOneShot(footstepClips[clipIndex], 0.6f);
        }

        private void HandleJumpAndGravity()
        {
            if (Keyboard.current == null) return;

            if (_isGrounded && !_isCrouching && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _verticalVelocity = jumpForce;
            }

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

        [ClientRpc]
        public void TeleportClientRpc(Vector3 targetPosition, Quaternion targetRotation)
        {
            if (_characterController != null) _characterController.enabled = false;

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            _currentHorizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _stepTimer = 0f;
            _currentStamina = maxStamina;

            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
            }

            if (cameraHolder != null) cameraHolder.localRotation = Quaternion.identity;
            if (_characterController != null) _characterController.enabled = true;
        }
    }
}