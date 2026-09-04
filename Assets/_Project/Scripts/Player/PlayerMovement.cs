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
        [SerializeField] private float staminaDrainRate = 28.0f;
        [SerializeField] private float staminaRegenRate = 22.0f;
        [SerializeField] private float regenDelay = 1.0f;

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
        [Tooltip("Voeg hier meerdere verschillende voetstap-audioclips toe")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float walkStepInterval = 0.45f;
        [SerializeField] private float sprintStepInterval = 0.30f;
        [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.65f;

        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
        private bool _isGrounded;
        private bool _isCrouching;
        private float _stepTimer;
        private int _lastClipIndex = -1;

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

            bool wantsToSprint = Keyboard.current.leftShiftKey.isPressed && !_isCrouching && inputZ > 0.1f;
            bool isSprinting = wantsToSprint && _currentStamina > 0f && isMoving;

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
            // Geen voetstappen als we in de lucht zijn, bukken of stilstaan
            if (!_isGrounded || _isCrouching || _currentHorizontalVelocity.magnitude < 1.2f)
            {
                return;
            }

            bool isSprinting = _currentHorizontalVelocity.magnitude > (walkSpeed + 0.5f);
            float currentInterval = isSprinting ? sprintStepInterval : walkStepInterval;

            _stepTimer += Time.deltaTime;

            if (_stepTimer >= currentInterval)
            {
                _stepTimer = 0f;
                TriggerFootstep();
            }
        }

        private void TriggerFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;

            // Kies een willekeurige clip, maar nooit dezelfde twee keer achter elkaar
            int selectedIndex = 0;
            if (footstepClips.Length > 1)
            {
                do
                {
                    selectedIndex = Random.Range(0, footstepClips.Length);
                } while (selectedIndex == _lastClipIndex);
            }
            _lastClipIndex = selectedIndex;

            float randomPitch = Random.Range(0.92f, 1.08f);

            // 1. Speel lokaal direct af zonder vertraging
            PlayFootstepAudio(selectedIndex, randomPitch);

            // 2. Synchroniseer via Server naar de tegenstander
            PlayFootstepServerRpc(selectedIndex, randomPitch);
        }

        private void PlayFootstepAudio(int clipIndex, float pitch)
        {
            if (footstepAudioSource == null || footstepClips == null || clipIndex >= footstepClips.Length) return;

            AudioClip clip = footstepClips[clipIndex];
            if (clip != null)
            {
                footstepAudioSource.pitch = pitch;
                footstepAudioSource.PlayOneShot(clip, footstepVolume);
            }
        }

        [ServerRpc]
        private void PlayFootstepServerRpc(int clipIndex, float pitch)
        {
            // Stuur door naar alle andere clients (niet naar de eigenaar zelf, die hoort het lokaal al)
            PlayFootstepClientRpc(clipIndex, pitch);
        }

        [ClientRpc]
        private void PlayFootstepClientRpc(int clipIndex, float pitch)
        {
            if (IsOwner) return; // Dubbele audio bij de schutter voorkomen
            PlayFootstepAudio(clipIndex, pitch);
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