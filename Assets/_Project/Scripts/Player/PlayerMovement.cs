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
        [Tooltip("Sleep hier het 3D-lichaamsmodel van de soldaat in")]
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

        [Header("Wall Running (Straight Line & Stick)")]
        [SerializeField] private LayerMask wallRunMask = ~0;
        [SerializeField] private float wallRunSpeed = 16f;
        [Tooltip("Houdt de lijn horizontaal. 0 = perfect vlakke lijn, 0.5 - 1.5 = heel subtiele daling")]
        [SerializeField] private float wallRunDownwardDrift = 0.5f;
        [Tooltip("Kracht waarmee de speler tegen de muur gezogen blijft")]
        [SerializeField] private float wallStickForce = 6f;
        [SerializeField] private float wallRunCameraLean = 14f;
        [SerializeField] private float wallJumpUpForce = 9f;
        [SerializeField] private float wallJumpSideForce = 13f;

        [Header("Camera Tilt & Effects")]
        [SerializeField] private float cameraTiltSpeed = 12f;
        [SerializeField] private float fovChangeSpeed = 8f;
        [SerializeField] private float runFovIncrease = 8f;
        [SerializeField] private float slideFovIncrease = 14f;

        // Statussen
        private Vector3 _velocity;
        private Vector3 _horizontalVelocity;
        private float _currRotationX = 0f;
        private float _currentTilt = 0f;
        private Camera _cameraComponent;

        private bool _isGrounded;
        private bool _hasDoubleJump;
        private float _lastGroundedTime;
        private float _lastJumpPressedTime;

        // Slide variabelen
        private bool _isSliding;
        private Vector3 _slideDirection;
        private float _slideTimer;
        private float _lastSlideEndTime = -10f;
        private float _defaultHeight;
        private Vector3 _defaultCenter;
        private Vector3 _defaultCamPos;

        // Wall Run variabelen
        private bool _isWallRunning;
        private bool _wallIsOnRight;
        private RaycastHit _wallHit;
        private float _wallRunStartTime;

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
        }

        private void Start()
        {
            if (_cameraComponent != null)
            {
                _cameraComponent.fieldOfView = baseFOV;
            }
        }

        private void HideLocalPlayerBody()
        {
            if (visualsRoot != null)
            {
                var renderers = visualsRoot.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (SceneManager.GetActiveScene().name != "Maintestgameplay" || PauseMenu.IsPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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
                return;
            }

            CheckGroundedStatus();
            CheckWallRun();
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

                _cameraComponent.fieldOfView = Mathf.Lerp(_cameraComponent.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
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
                // Wall Run: Vaste vlakke beweging langs de muur
                Vector3 wallNormal = _wallHit.normal;
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

                // Zorg dat we altijd vooruit bewegen ten opzichte van de kijkrichting
                if (Vector3.Dot(wallForward, transform.forward) < 0f)
                {
                    wallForward = -wallForward;
                }

                // Snelheid langs de wand + lichte zuigkracht TEGEN de muur (voorkomt loslaten)
                Vector3 forwardMovement = wallForward * wallRunSpeed;
                Vector3 stickToWall = -wallNormal * wallStickForce;

                _horizontalVelocity = forwardMovement + stickToWall;

                // Kaarsrechte lijn (Y blijft perfect vlak)
                _velocity.y = -wallRunDownwardDrift;
            }
            else
            {
                bool isRunning = Input.GetKey(KeyCode.LeftShift) && !_isScoped;
                bool isCrouching = Input.GetKey(KeyCode.LeftControl);

                float targetSpeed = walkSpeed;
                if (isCrouching) targetSpeed = crouchSpeed;
                else if (isRunning && inputZ > 0.1f) targetSpeed = runSpeed;

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
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _lastJumpPressedTime = Time.time;
            }

            bool wantsToJump = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;

            if (wantsToJump)
            {
                if (_isWallRunning)
                {
                    // Wall Jump: krachtig wegschieten van de muur
                    _lastJumpPressedTime = -10f;
                    _isWallRunning = false;

                    Vector3 jumpAway = _wallHit.normal * wallJumpSideForce + Vector3.up * wallJumpUpForce + transform.forward * (runSpeed * 0.75f);
                    _horizontalVelocity = new Vector3(jumpAway.x, 0, jumpAway.z);
                    _velocity.y = jumpAway.y;

                    _hasDoubleJump = true;
                    PlaySound(soundPlayer != null ? soundPlayer.jumpSound : null);
                }
                else if (Time.time - _lastGroundedTime <= coyoteTime)
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
                else if (_hasDoubleJump)
                {
                    _lastJumpPressedTime = -10f;
                    _hasDoubleJump = false;

                    _velocity.y = Mathf.Sqrt(doubleJumpHeight * 2f * gravity);
                    PlaySound(soundPlayer != null ? soundPlayer.jumpSound : null);
                }
            }
        }

        private void CheckWallRun()
        {
            // Niet wallrunnen als je al stevig op de vloer staat
            if (_isGrounded)
            {
                _isWallRunning = false;
                return;
            }

            float checkDistance = controller.radius + 0.65f;
            Vector3 center = transform.position + Vector3.up * (controller.height * 0.5f);

            // Controleer rechts, links en licht diagonaal zodat rondkijken de raycast niet breekt
            bool hitRight = Physics.Raycast(center, transform.right, out RaycastHit hitR, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore)
                         || Physics.Raycast(center, (transform.right + transform.forward * 0.5f).normalized, out hitR, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore);

            bool hitLeft = Physics.Raycast(center, -transform.right, out RaycastHit hitL, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore)
                        || Physics.Raycast(center, (-transform.right + transform.forward * 0.5f).normalized, out hitL, checkDistance, wallRunMask, QueryTriggerInteraction.Ignore);

            float forwardInput = Input.GetAxisRaw("Vertical");

            if ((hitRight || hitLeft) && forwardInput > 0.1f && !_isSliding)
            {
                RaycastHit activeHit = hitRight ? hitR : hitL;

                // Controleer of het een verticale muur is (geen vloer of hellend dak)
                if (Mathf.Abs(activeHit.normal.y) < 0.2f)
                {
                    if (!_isWallRunning)
                    {
                        // Eerste frame van contact: neutraliseer verticale jumpsnelheid
                        _velocity.y = 0f;
                        _wallRunStartTime = Time.time;
                    }

                    _wallHit = activeHit;
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