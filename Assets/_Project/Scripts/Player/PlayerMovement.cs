using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SniperGame.UI;
using SniperGame.Gameplay;
using ParkourFPS;

namespace SniperGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : NetworkBehaviour
    {
        #region Components
        private CharacterController controller;
        private SoundPlayer soundPlayer;
        #endregion

        [Header("Camera & Visuals")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Assign the 3D soldier model GameObject here (auto-detected if left empty)")]
        [SerializeField] private GameObject visualsRoot;
        [SerializeField] private float baseFOV = 80f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float lookXLimit = 85f;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 9f;
        [SerializeField] private float runSpeed = 15f;
        [SerializeField] private float crouchSpeed = 5f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float airControl = 0.6f;

        [Header("Stamina Settings")]
        [SerializeField] private bool staminaEnabled = true;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainRate = 22f;
        [SerializeField] private float staminaRegenRate = 18f;
        [SerializeField] private float staminaRegenDelay = 0.9f;
        [SerializeField] private float minStaminaToSprint = 15f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpHeight = 2.4f;
        [SerializeField] private float doubleJumpHeight = 2.0f;
        [SerializeField] private float gravity = 28f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float coyoteTime = 0.15f;

        [Header("Sliding")]
        [SerializeField] private KeyCode slideKey = KeyCode.C;
        [SerializeField] private float slideBoostSpeed = 20f;
        [SerializeField] private float slideDuration = 0.75f;
        [SerializeField] private float slideFriction = 12f;
        [SerializeField] private float slideCooldown = 0.4f;
        [SerializeField] private float slideCameraLean = 10f;

        [Header("Wall Running (Hold Space)")]
        [SerializeField] private LayerMask wallRunMask = ~0;
        [SerializeField] private float wallRunHoldTime = 0.4f;
        [SerializeField] private float wallRunSpeed = 16f;
        [SerializeField] private float wallRunDownwardDrift = 0.5f;
        [SerializeField] private float wallStickForce = 6f;
        [SerializeField] private float wallRunCameraLean = 14f;
        [SerializeField] private float wallJumpUpForce = 9f;
        [SerializeField] private float wallJumpSideForce = 13f;

        [Header("Wall Run UI Prompt")]
        [SerializeField] private float promptDistance = 3.5f;

        [Header("Camera Tilt & Effects")]
        [SerializeField] private float cameraTiltSpeed = 12f;
        [SerializeField] private float fovChangeSpeed = 8f;
        [SerializeField] private float runFovIncrease = 8f;
        [SerializeField] private float slideFovIncrease = 14f;

        // Runtime states
        private Vector3 _velocity;
        private Vector3 _horizontalVelocity;
        private float _currRotationX = 0f;
        private float _currentTilt = 0f;
        private Camera _cameraComponent;

        private bool _isGrounded;
        private bool _hasDoubleJump;
        private float _lastGroundedTime;
        private float _lastJumpPressedTime;
        private float _spaceHoldTimer;

        // Stamina runtime states
        private float _currentStamina;
        private float _lastSprintTime;
        private bool _isExhausted;

        // Slide variables
        private bool _isSliding;
        private Vector3 _slideDirection;
        private float _slideTimer;
        private float _lastSlideEndTime = -10f;
        private float _defaultHeight;
        private Vector3 _defaultCenter;
        private Vector3 _defaultCamPos;

        // Wall Run variables
        private bool _isWallRunning;
        private bool _wallIsOnRight;
        private RaycastHit _wallHit;
        private bool _canWallRunNear;
        private int _wallHoldLayerIndex = -1;

        // Sniper ADS & Recoil hooks
        private bool _isScoped = false;
        private float _scopedFov = 18f;
        private float _sensitivityMultiplier = 1.0f;
        private float _recoilPitch = 0f;
        private float _recoilYaw = 0f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            HideLocalPlayerBody();
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            soundPlayer = GetComponent<SoundPlayer>();

            _defaultHeight = controller.height;
            _defaultCenter = controller.center;

            if (cameraTransform != null)
            {
                _cameraComponent = cameraTransform.GetComponent<Camera>();
                _defaultCamPos = cameraTransform.localPosition;
            }

            _wallHoldLayerIndex = LayerMask.NameToLayer("wallHold");
            if (_wallHoldLayerIndex == -1)
            {
                _wallHoldLayerIndex = LayerMask.NameToLayer("WallHold");
            }

            _currentStamina = maxStamina;
        }

        private void Start()
        {
            if (_cameraComponent != null)
            {
                _cameraComponent.fieldOfView = baseFOV;
            }

            if (CombatHUD.Instance != null && staminaEnabled)
            {
                CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
            }

            if (IsOwner)
            {
                HideLocalPlayerBody();
            }
        }

        private void HideLocalPlayerBody()
        {
            if (visualsRoot == null)
            {
                foreach (Transform child in transform)
                {
                    if (cameraTransform != null && (child == cameraTransform || child.IsChildOf(cameraTransform)))
                        continue;

                    if (child.name.ToLower().Contains("camera") || child.name.ToLower().Contains("weapon"))
                        continue;

                    if (child.GetComponentInChildren<Renderer>() != null)
                    {
                        visualsRoot = child.gameObject;
                        break;
                    }
                }
            }

            if (visualsRoot != null)
            {
                var renderers = visualsRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (cameraTransform != null && r.transform.IsChildOf(cameraTransform))
                        continue;

                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Release cursor in menus, when game is paused, or when Match End / Rematch screen is open
            bool isMatchEnd = CombatHUD.Instance != null && CombatHUD.Instance.IsMatchEndActive;

            if (SceneManager.GetActiveScene().name != "Maintestgameplay" || PauseMenu.IsPaused || isMatchEnd)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                UpdatePromptVisuals(false);
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            HandleLook();

            if (RoundManager.Instance != null && !RoundManager.Instance.CanPlayersFight())
            {
                UpdatePromptVisuals(false);
                return;
            }

            bool isPressingSpace = Input.GetKey(KeyCode.Space);
            float targetTimer = isPressingSpace ? wallRunHoldTime : 0f;
            _spaceHoldTimer = Mathf.MoveTowards(_spaceHoldTimer, targetTimer, Time.deltaTime);

            CheckGroundedStatus();
            CheckWallRun();
            UpdateWallRunPromptUI();
            HandleSlideInput();
            HandleJumpInput();
            CalculateMovement();
            ApplyCameraTiltAndFOV();
        }

        #region Kantenwissel & Netcode

        [ClientRpc]
        public void TeleportClientRpc(Vector3 newPosition, Quaternion newRotation)
        {
            if (controller == null) controller = GetComponent<CharacterController>();

            controller.enabled = false;
            transform.position = newPosition;
            transform.rotation = newRotation;
            controller.enabled = true;

            _velocity = Vector3.zero;
            _horizontalVelocity = Vector3.zero;
            _currRotationX = 0f;
            _isSliding = false;
            _isWallRunning = false;
            _spaceHoldTimer = 0f;

            _currentStamina = maxStamina;
            _isExhausted = false;

            if (CombatHUD.Instance != null && staminaEnabled)
            {
                CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
            }
        }

        #endregion

        #region Sniper ADS & Recoil Hooks

        public void SetScopeState(bool scoped, float targetScopedFov, float sensitivityMult)
        {
            _isScoped = scoped;
            _scopedFov = targetScopedFov;
            _sensitivityMultiplier = sensitivityMult;
        }

        public void AddRecoil(float pitch, float yaw)
        {
            _recoilPitch += pitch;
            _recoilYaw += yaw;
        }

        #endregion

        #region Look & Camera

        private void HandleLook()
        {
            float currentSensitivity = lookSensitivity * _sensitivityMultiplier;

            _currRotationX -= (Input.GetAxis("Mouse Y") * currentSensitivity) + _recoilPitch;
            _currRotationX = Mathf.Clamp(_currRotationX, -lookXLimit, lookXLimit);
            _recoilPitch = Mathf.MoveTowards(_recoilPitch, 0f, Time.deltaTime * 18f);

            float mouseX = (Input.GetAxis("Mouse X") * currentSensitivity) + _recoilYaw;
            _recoilYaw = Mathf.MoveTowards(_recoilYaw, 0f, Time.deltaTime * 18f);

            transform.rotation *= Quaternion.Euler(0, mouseX, 0);

            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.Euler(_currRotationX, 0f, _currentTilt);
            }
        }

        private void ApplyCameraTiltAndFOV()
        {
            float targetTilt = 0f;
            if (_isWallRunning)
            {
                targetTilt = _wallIsOnRight ? wallRunCameraLean : -wallRunCameraLean;
            }
            else if (_isSliding)
            {
                targetTilt = slideCameraLean;
            }

            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * cameraTiltSpeed);

            if (_cameraComponent != null)
            {
                float targetFOV = baseFOV;

                if (_isScoped)
                {
                    targetFOV = _scopedFov;
                }
                else if (_isSliding)
                {
                    targetFOV = baseFOV + slideFovIncrease;
                }
                else if (Input.GetKey(KeyCode.LeftShift) && _horizontalVelocity.magnitude > 10f)
                {
                    targetFOV = baseFOV + runFovIncrease;
                }

                float fovSpeed = (_isScoped || _cameraComponent.fieldOfView < (baseFOV - 1f)) ? 22f : fovChangeSpeed;
                _cameraComponent.fieldOfView = Mathf.Lerp(_cameraComponent.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
            }
        }

        #endregion

        #region UI Prompt & Detection

        private bool IsValidWallRunObject(GameObject obj)
        {
            if (obj == null) return false;

            if (_wallHoldLayerIndex != -1)
            {
                return obj.layer == _wallHoldLayerIndex;
            }

            return (wallRunMask.value & (1 << obj.layer)) != 0;
        }

        private void UpdateWallRunPromptUI()
        {
            if (_isWallRunning)
            {
                UpdatePromptVisuals(false);
                return;
            }

            bool closeToWall = false;

            if (cameraTransform != null)
            {
                if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, promptDistance, wallRunMask, QueryTriggerInteraction.Ignore))
                {
                    if (Mathf.Abs(hit.normal.y) < 0.25f && !hit.collider.transform.IsChildOf(transform) && IsValidWallRunObject(hit.collider.gameObject))
                    {
                        closeToWall = true;
                    }
                }
            }

            if (!closeToWall && _canWallRunNear)
            {
                closeToWall = true;
            }

            UpdatePromptVisuals(closeToWall);
        }

        private void UpdatePromptVisuals(bool shouldShow)
        {
            float progress = Mathf.Clamp01(_spaceHoldTimer / Mathf.Max(0.01f, wallRunHoldTime));

            if (CombatHUD.Instance != null)
            {
                CombatHUD.Instance.SetWallRunPrompt(shouldShow, progress);
            }
        }

        #endregion

        #region Ground & Movement

        private void CheckGroundedStatus()
        {
            _isGrounded = controller.isGrounded;

            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
                _hasDoubleJump = true;

                if (_velocity.y < 0f)
                {
                    _velocity.y = -2f;
                }
            }
        }

        private void CalculateMovement()
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 wishDir = (transform.forward * inputZ + transform.right * inputX).normalized;

            if (_isSliding)
            {
                _slideTimer -= Time.deltaTime;
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, wishDir * crouchSpeed, slideFriction * Time.deltaTime);

                if (_slideTimer <= 0f || _horizontalVelocity.magnitude < crouchSpeed)
                {
                    StopSlide();
                }
            }
            else if (_isWallRunning)
            {
                Vector3 wallNormal = _wallHit.normal;
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

                if (Vector3.Dot(wallForward, transform.forward) < 0f)
                {
                    wallForward = -wallForward;
                }

                Vector3 forwardMovement = wallForward * wallRunSpeed;
                Vector3 stickToWall = -wallNormal * wallStickForce;

                _horizontalVelocity = forwardMovement + stickToWall;
                _velocity.y = -wallRunDownwardDrift;
            }
            else
            {
                bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && !_isScoped && inputZ > 0.1f && !_isSliding;
                bool isCrouching = Input.GetKey(KeyCode.LeftControl);

                if (_isExhausted && _currentStamina >= minStaminaToSprint)
                {
                    _isExhausted = false;
                }

                bool isRunning = wantsToRun && !_isExhausted && (!staminaEnabled || _currentStamina > 0f);

                if (staminaEnabled)
                {
                    if (isRunning && _horizontalVelocity.magnitude > 6f)
                    {
                        _currentStamina = Mathf.Max(0f, _currentStamina - staminaDrainRate * Time.deltaTime);
                        _lastSprintTime = Time.time;

                        if (_currentStamina <= 0f)
                        {
                            _isExhausted = true;
                        }
                    }
                    else if (Time.time >= _lastSprintTime + staminaRegenDelay)
                    {
                        _currentStamina = Mathf.Min(maxStamina, _currentStamina + staminaRegenRate * Time.deltaTime);
                    }

                    if (CombatHUD.Instance != null)
                    {
                        CombatHUD.Instance.UpdateStamina(_currentStamina, maxStamina);
                    }
                }

                float targetSpeed = walkSpeed;
                if (isCrouching) targetSpeed = crouchSpeed;
                else if (isRunning) targetSpeed = runSpeed;

                Vector3 targetVelocity = wishDir * targetSpeed;
                float currentAccel = _isGrounded ? acceleration : acceleration * airControl;

                _horizontalVelocity = Vector3.Lerp(_horizontalVelocity, targetVelocity, currentAccel * Time.deltaTime);

                if (!_isGrounded)
                {
                    _velocity.y -= gravity * Time.deltaTime;
                }
            }

            Vector3 finalMove = (_horizontalVelocity + Vector3.up * _velocity.y) * Time.deltaTime;
            controller.Move(finalMove);

            HandleFootstepSounds();
        }

        #endregion

        #region Jumping & Wall Run

        private void HandleJumpInput()
        {
            if (_isWallRunning && Input.GetKeyUp(KeyCode.Space))
            {
                ExecuteWallJump();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _lastJumpPressedTime = Time.time;
            }

            bool wantsToJump = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;

            if (wantsToJump)
            {
                if (Time.time - _lastGroundedTime <= coyoteTime)
                {
                    _lastJumpPressedTime = -10f;
                    _lastGroundedTime = -10f;

                    if (_isSliding)
                    {
                        _velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
                        StopSlide();
                    }
                    else
                    {
                        _velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
                    }

                    PlaySound(soundPlayer != null ? soundPlayer.jumpSound : null);
                }
                else if (_hasDoubleJump && !_isWallRunning)
                {
                    _lastJumpPressedTime = -10f;
                    _hasDoubleJump = false;

                    _velocity.y = Mathf.Sqrt(doubleJumpHeight * 2f * gravity);
                    PlaySound(soundPlayer != null ? soundPlayer.jumpSound : null);
                }
            }
        }

        private void ExecuteWallJump()
        {
            _isWallRunning = false;
            _lastJumpPressedTime = -10f;
            _spaceHoldTimer = 0f;

            Vector3 jumpAway = _wallHit.normal * wallJumpSideForce + Vector3.up * wallJumpUpForce + transform.forward * (runSpeed * 0.75f);
            _horizontalVelocity = new Vector3(jumpAway.x, 0, jumpAway.z);
            _velocity.y = jumpAway.y;

            _hasDoubleJump = true;
            PlaySound(soundPlayer != null ? soundPlayer.jumpSound : null);
        }

        private void CheckWallRun()
        {
            if (_isGrounded)
            {
                _isWallRunning = false;
                _canWallRunNear = false;
                return;
            }

            float checkDistance = controller.radius + 0.65f;
            Vector3 center = transform.position + Vector3.up * (controller.height * 0.5f);

            bool hitRight = Physics.Raycast(center, transform.right, out RaycastHit hitR, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore)
                         || Physics.Raycast(center, (transform.right + transform.forward * 0.5f).normalized, out hitR, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore);

            bool hitLeft = Physics.Raycast(center, -transform.right, out RaycastHit hitL, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore)
                        || Physics.Raycast(center, (-transform.right + transform.forward * 0.5f).normalized, out hitL, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore);

            RaycastHit candidateHit = hitRight ? hitR : hitL;
            bool hitWall = (hitRight || hitLeft) && IsValidWallRunObject(candidateHit.collider.gameObject);

            _canWallRunNear = hitWall;

            float forwardInput = Input.GetAxisRaw("Vertical");
            bool isHoldingSpaceLongEnough = _spaceHoldTimer >= (wallRunHoldTime * 0.95f);

            if (hitWall && forwardInput > 0.1f && !_isSliding && (_isWallRunning ? Input.GetKey(KeyCode.Space) : isHoldingSpaceLongEnough))
            {
                if (Mathf.Abs(candidateHit.normal.y) < 0.2f)
                {
                    if (!_isWallRunning)
                    {
                        _velocity.y = 0f;
                    }

                    _wallHit = candidateHit;
                    _wallIsOnRight = hitRight;
                    _isWallRunning = true;
                    _hasDoubleJump = true;
                    return;
                }
            }

            _isWallRunning = false;
        }

        #endregion

        #region Sliding

        private void HandleSlideInput()
        {
            bool slidePressed = Input.GetKeyDown(slideKey) || (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift));

            if (slidePressed && !_isSliding && _isGrounded && (Time.time >= _lastSlideEndTime + slideCooldown))
            {
                StartSlide();
            }

            if (_isSliding && (Input.GetKeyUp(slideKey) || Input.GetKeyUp(KeyCode.LeftControl)))
            {
                StopSlide();
            }
        }

        private void StartSlide()
        {
            _isSliding = true;
            _slideTimer = slideDuration;

            Vector3 inputDir = (transform.forward * Input.GetAxisRaw("Vertical") + transform.right * Input.GetAxisRaw("Horizontal")).normalized;
            _slideDirection = inputDir.magnitude > 0.1f ? inputDir : transform.forward;

            _horizontalVelocity = _slideDirection * Mathf.Max(_horizontalVelocity.magnitude + 4f, slideBoostSpeed);

            controller.height = _defaultHeight * 0.5f;
            controller.center = new Vector3(0, _defaultHeight * 0.25f, 0);

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(_defaultCamPos.x, _defaultCamPos.y - 0.55f, _defaultCamPos.z);
            }

            PlaySound(soundPlayer != null ? soundPlayer.slidingSound : null);
        }

        private void StopSlide()
        {
            if (!_isSliding) return;
            _isSliding = false;
            _lastSlideEndTime = Time.time;

            controller.height = _defaultHeight;
            controller.center = _defaultCenter;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = _defaultCamPos;
            }
        }

        #endregion

        #region Audio Feedback

        private void HandleFootstepSounds()
        {
            if (soundPlayer == null) return;

            if (_isGrounded && _horizontalVelocity.magnitude > 1.5f && !_isSliding)
            {
                bool isRunning = _horizontalVelocity.magnitude > 10f;
                AudioClip clipToPlay = isRunning ? soundPlayer.runningSound : soundPlayer.walkingSound;

                if (!soundPlayer.isPlaying || soundPlayer.clip != clipToPlay)
                {
                    soundPlayer.PlaySound(clipToPlay, loop: true, volume: isRunning ? 0.8f : 0.5f);
                }
            }
            else if (soundPlayer.isPlaying && (soundPlayer.clip == soundPlayer.runningSound || soundPlayer.clip == soundPlayer.walkingSound))
            {
                soundPlayer.Stop();
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (soundPlayer != null && clip != null)
            {
                soundPlayer.PlaySound(clip, loop: false, volume: 0.7f);
            }
        }

        #endregion
    }
} 