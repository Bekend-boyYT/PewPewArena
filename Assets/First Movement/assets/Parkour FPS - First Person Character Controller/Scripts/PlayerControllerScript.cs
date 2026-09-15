using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script to handle first person parkour controller movement.
 * Please read How_To_Use.pdf in the asset folder.
 * Credit to snon200.
 * For any questions: snon200@gmail.com
 */

namespace ParkourFPS

{

    [RequireComponent(typeof(CapsuleCollider))]

    [RequireComponent(typeof(Rigidbody))]

    [RequireComponent(typeof(SoundPlayer))]

    public class PlayerControllerScript : MonoBehaviour

    {

        #region components

        private CapsuleCollider capsuleCollider;

        private Rigidbody playerRigidbody;

        private SoundPlayer soundPlayer;

        #endregion

        [Header("Camera")]

        [Tooltip("player camera transform")]

        [SerializeField] private Transform cameraTransform;

        [Tooltip("speed lines object")]

        [SerializeField] private GameObject speedLines;

        [Tooltip("camera field of view")]

        [SerializeField] private float fieldOfView = 80;

        [Tooltip("mouse look sensitivity")]

        [SerializeField] private float lookSensitivity = 2;

        private Camera cameraComponent; // the player camera component

        private static float lookXLimit = 45; // player upward rotation limit

        private float currRotationX = 0; // current player rotation

        [Header("Camera Smoothing")]
        [Tooltip("Smooth all field of view changes instead of changing FOV instantly.")]
        [SerializeField] private bool smoothFovChanges = true;

        [Tooltip("How quickly the FOV reaches its target. Lower values are snappier, higher values are smoother.")]
        [SerializeField] private float fovSmoothTime = 0.08f;

        [Tooltip("Add a slight delay when the camera follows mouse look.")]
        [SerializeField] private bool smoothLook = true;

        [Tooltip("How quickly the camera catches up to the target look rotation. Lower values create more drag.")]
        [SerializeField] private float lookSmoothTime = 0.05f;

        private float currentCameraRotationX = 0f;
        private float cameraRotationXVelocity = 0f;
        private float currentCameraLeanSmooth = 0f;
        private float cameraLeanVelocity = 0f;
        private float fovVelocity = 0f;

        [Header("Walking")]

        [Tooltip("if to decrease movement speed when walking up a slope")]

        [SerializeField] private bool slopeSpeedDecrease = false;

        [Tooltip("base player walking speed")]

        [SerializeField] private float walkSpeed = 100;

        [Tooltip("velocity multiplier when player is in the air")]

        [SerializeField] private float airMultiplier = 1.1f;

        [Header("Running")]

        [Tooltip("if the player is able to run")]

        [SerializeField] private bool runningEnabled = true;

        [Tooltip("run button")]

        [SerializeField] private KeyCode runButton = KeyCode.LeftShift;

        [Tooltip("base player running speed")]

        [SerializeField] private float runSpeed = 130;

        [Tooltip("amount of fov increase when running")]

        [SerializeField] private float runFovIcrease = 10;

        private bool isRunning = false; // if currently running

        [Header("Stamina")]

        [Tooltip("if the player has a stamina limitation to running")]

        [SerializeField] private bool staminaEnabled = true;

        [Tooltip("stamina amount UI text")]

        [SerializeField] private Text staminaText;

        [Tooltip("the time before the stamina depletes")]

        [SerializeField] private float staminaDuration = 10;

        [Tooltip("minimum amount of stamina to regenerate before able to run again")]

        [SerializeField] private float staminaCooldown = 3;

        [Tooltip("amount of stamina to refill every second")]

        [SerializeField] private float staminaFillRate = 2;

        private float currStamina = 0; // current stamina amount

        private bool staminaEmpty = false; // if the stamina was emptied

        [Header("Wall Running")]

        [Tooltip("if the player is able to wall run")]

        [SerializeField] private bool wallrunningEnabled = true;

        [Tooltip("amount of camera lean during wall running")]

        [SerializeField] private int wallRunCameraLean = 10;

        [Tooltip("amount of gravity to reduce during wall running")]

        [SerializeField] private int wallRunGravityReduction = 70;

        private bool isWallrunning = false; // if currently wall running

        private int currentCameraLean = 0; // current camera lean

        [Header("Crouching")]

        [Tooltip("if the player is able to crouch")]

        [SerializeField] private bool crouchingEnabled = true;

        [Tooltip("crouch button")]

        [SerializeField] private KeyCode crouchButton = KeyCode.LeftControl;

        [Tooltip("base player crouching speed")]

        [SerializeField] private float crouchSpeed = 80;

        private bool isCrouching = false; // if currently crouching

        private bool changedPlayerHeight = false; // if player height was already changed

        [Header("Sliding")]

        [Tooltip("if the player is able to slide")]

        [SerializeField] private bool slidingEnabled = true;

        [Tooltip("slide button")]

        [SerializeField] private KeyCode slideButton = KeyCode.C;

        [Tooltip("sliding duration in seconds")]

        [SerializeField] private float slideDuration = 0.5f;

        [Tooltip("amount of camera lean during sliding")]

        [SerializeField] private int slideCameraLean = 15;

        [Tooltip("amount of fov increase when sliding")]

        [SerializeField] private float slideFovIncrease = 3;

        [Tooltip("Minimum delay in seconds before another slide can start.")]

        [SerializeField] private float slideCooldown = 0.15f;

        private bool isSliding = false;

        private float lastSlideEndTime = -Mathf.Infinity; // if currently sliding

        [Header("Jumping")]

        [Tooltip("if the player is able to jump")]

        [SerializeField] private bool jumpingEnabled = true;

        [Tooltip("jump button")]

        [SerializeField] private KeyCode jumpButton = KeyCode.Space;

        [Tooltip("the vertical force when jumping")]

        [SerializeField] private float jumpAmount = 80;

        private static float jumpBufferTime = 0.2f; // the duration the player is still allowed to jump after leaving a surface

        private float lastJumpTime = 0f; // the time when last jumped

        private float jumpTryTime = 0f; // time of last failed jump attempt

        [Header("Wall Jumping")]

        [Tooltip("if the player is able to wall jump")]

        [SerializeField] private bool wallJumpingEnabled = true;

        [Tooltip("the horizontal force added when wall jumping")]

        [SerializeField] private float wallJumpForce = 40;

        [Header("Double Jump")]

        [Tooltip("if the player is able to double jump")]

        [SerializeField] private bool doubleJumpingEnabled = true;

        private bool hasDoubleJump = false; // if the player currently has a double jump remaining

        [Header("Momentum")]

        [Tooltip("if to use momentum to increase the player's movement speed")]

        [SerializeField] private bool useMomentum = true;

        [Tooltip("momentum amount UI text")]

        [SerializeField] private Text momentumText;

        [Tooltip("the minimum velocity required to maintain momentum")]

        [SerializeField] private float momentumResetThreshold = 5;

        [Tooltip("amount of momentum to decrease every second")]

        [SerializeField] private float momentumDecreaseRate = 0.01f;

        [Tooltip("amount of momentum increase per frame when running")]

        [SerializeField] private float runMomentumIncrease = 0.001f;

        [Tooltip("amount of momentum icrease per frame when wall running")]

        [SerializeField] private float wallRunMomentumIncrease = 0.005f;

        [Tooltip("amount of momentum increase when starting a slide")]

        [SerializeField] private float slideMomentumIncrease = 0.2f;

        private float momentum = 1; // current player momentum

        [Header("Drag & Gravity")]

        [Tooltip("drag while grounded")]

        [SerializeField] private float groundDrag = 5;

        [Tooltip("drag while in the air")]

        [SerializeField] private float airDrag = 5;

        [Tooltip("gravity amount, the higher the stronger gravity is")]

        [SerializeField] private float gravity = 100;

        [Header("Ground Detection")]

        [Tooltip("the layers of the ground objects")]

        [SerializeField] private LayerMask groundMask;

        [Tooltip("ground check location")]

        [SerializeField] private Transform groundCheck;

        [Tooltip("right wall check location")]

        [SerializeField] private Transform wallCheckRight;

        [Tooltip("left wall check location")]

        [SerializeField] private Transform wallCheckLeft;

        private static float groundDistance = 0.4f; // the max distance from the ground to detect touch

        private bool isGrounded; // if currently touching the ground

        private RaycastHit slopeHit; // raycast for slope detection

// check if the player is standing on a slope

        private bool OnSlope()

        {

// check collision with a raycast

            if (Physics.Raycast(groundCheck.position, Vector3.down, out slopeHit, groundDistance))

                if (slopeHit.normal != Vector3.up) // if surface is angled

                    return true;

            return false;

        }

// Start is called before the first frame update

        private void Start()

        {

            #region initialize components

            capsuleCollider = GetComponent<CapsuleCollider>();

            playerRigidbody = GetComponent<Rigidbody>();

            soundPlayer = GetComponent<SoundPlayer>();

            cameraComponent = cameraTransform.GetComponent<Camera>();

            #endregion

// set fov

            cameraComponent.fieldOfView = fieldOfView;

// set gravity

            Physics.gravity = new Vector3(0, -gravity, 0);

// set momentum

            if (useMomentum) // if using momentum

            {

                if (momentumDecreaseRate != 0) // if reducing momentum

                    StartCoroutine(ReduceMomentum()); // start reducing momentum over time

                StartCoroutine(CheckMomentumReset()); // check when momentum needs to reset

            }

            else if (momentumText != null)

                momentumText.gameObject.SetActive(false); // disable momnetum text

// set stamina

            if (staminaEnabled) // if using stamina

                StartCoroutine(ControlStamina()); // start controlling the stamina amount

            else if (staminaText != null)

                staminaText.gameObject.SetActive(false); // disable stamina text

// start checking if the player is grounded

            StartCoroutine(CheckGrounded());

        }

// Update is called once per frame

        private void Update()

        {

/* handle player input */

            SetRotation(); // set player rotation

            CheckUserInput(); // check user key input

        }

// FixedUpdate is called once per physics frame

        private void FixedUpdate()

        {

/* handle physics changes */

// set momentum text

            momentumText.text = "Momentum - " + ((momentum - 1f) * 10f).ToString("0.0");

// check if touching a wall

            touchingWallRight = TouchingWallRight(); // set touching right wall status

            if (touchingWallRight) // if touching

                rightWallTouchTime = Time.time; // set touch time

            touchingWallLeft = TouchingWallLeft(); // set touching left wall status

            if (touchingWallLeft) // if touching

                leftWallTouchTime = Time.time; // set touch time

// check if touching the ground

            touchingGround = TouchingGround();

// set speed lines

            if (speedLines != null) // if speedlines object set

            {

                if (isWallrunning || isSliding) // if wallrunning or sliding

                    speedLines.SetActive(true); // enable speed lines

                else // not wallrunning and not sliding

                    speedLines.SetActive(false); // disable speed lines

            }

            SetPlayerHeight(); // set player height

            SetFov(); // set player field of view

            SetDrag(); // set player drag

            MovePlayer(); // set player movement

        }

// check user key presses and handle input

        private void CheckUserInput()

        {

/* running and crouching */

            if (Input.GetKey(runButton) && runningEnabled

                && (!staminaEnabled || !staminaEmpty)) // if holding the run button and player has stamina

            {

                isRunning = true; // set running true

                isCrouching = false; // set crouching false

            }

            else // not running

            {

                isRunning = false; // set running false

                if (Input.GetKey(crouchButton) && crouchingEnabled) // if holding the crouch button

                    isCrouching = true; //set crouching true

                else // not crouching

                    isCrouching = false; //set crouching false

            }

/* jumping */

            if (Input.GetKeyDown(jumpButton) && jumpingEnabled) // if player pressed jump button

                Jump(); // try to jump

/* sliding */

            if (Input.GetKeyDown(slideButton) && !isSliding && slidingEnabled && Time.time >= lastSlideEndTime + slideCooldown) // if player pressed slide and cooldown has expired

                StartCoroutine(Slide()); // try to slide

        }

// set camera field of view depending on player state

        private void SetFov()
        {
            float targetFov = fieldOfView;

            if (isRunning)
                targetFov = fieldOfView + runFovIcrease;
            else if (isSliding)
                targetFov = fieldOfView + slideFovIncrease;

            if (smoothFovChanges)
            {
                float smoothTime = Mathf.Max(0.0001f, fovSmoothTime);
                cameraComponent.fieldOfView = Mathf.SmoothDamp(
                    cameraComponent.fieldOfView,
                    targetFov,
                    ref fovVelocity,
                    smoothTime);
            }
            else
            {
                cameraComponent.fieldOfView = targetFov;
                fovVelocity = 0f;
            }
        }

        private void SetPlayerHeight()

        {

            if (isSliding || isCrouching) // if sliding or crouching

            {

                if (!changedPlayerHeight) // if height not changed yet

                {

                    changedPlayerHeight = true;

// decrease player height

                    capsuleCollider.height /= 2f;

                    capsuleCollider.center -= new Vector3(0, capsuleCollider.height / 2f, 0);

                    cameraTransform.localPosition /= 4f;

                }

            }

            else

            {

                if (changedPlayerHeight) // if height was changed

                {

                    changedPlayerHeight = false;

// increase player height

                    capsuleCollider.center += new Vector3(0, capsuleCollider.height / 2f, 0);

                    capsuleCollider.height *= 2f;

                    cameraTransform.localPosition *= 4f;

                }

            }

        }

        #region sliding

// try to make the player slide if able to

        private IEnumerator Slide(bool retry = false)

        {

            if (!isSliding) // if not already sliding

            {

                if (isGrounded && !touchingWallRight && !touchingWallLeft) // if touching a flat surface

                {

/* start sliding */

                    isSliding = true; // set player currently sliding

                    if (slideMomentumIncrease != 0) // if sliding increases momentum

                        momentum += slideMomentumIncrease; // increase momentum

                    soundPlayer.PlaySound(soundPlayer.slidingSound); // play sliding sound

                    yield return new WaitForSeconds(slideDuration); // wait for sliding duration

/* finish sliding */

                    isSliding = false; // reset player currently sliding
                    lastSlideEndTime = Time.time; // start slide cooldown

                }

                else if (!retry) // not touching a flat surface and not retrying to slide

                {

                    float tempRetryTime = Time.time; // retry start time

                    while (Time.time <= (tempRetryTime + jumpBufferTime) && !isSliding) // while not passed buffer time and didn't successfully slide

                    {

                        yield return new WaitForFixedUpdate(); // wait for next frame

                        StartCoroutine(Slide(retry: true)); // try to slide again

                    }

                }

            }

        }

        #endregion

        #region jumping

// make player jump if he is able to

        private void Jump(bool retry = false)

        {

            if ((isGrounded || hasDoubleJump) && (Time.time >= (lastJumpTime + jumpBufferTime + 0.1f))) // if player is touching the ground or able to double jump

            {

                if (!isGrounded) // if player used double jump

                    hasDoubleJump = false; // disable double jump

                isGrounded = false; // set not grounded

                lastJumpTime = Time.time; // set jump time

                float horizontalJumpForce = 0;

                if (wallJumpingEnabled && !touchingGround) // if wall jumping is enabled and not in the ground

                {

                    if (Time.time < (rightWallTouchTime + jumpBufferTime)) // if touching a wall to the right of the player

                        horizontalJumpForce = -wallJumpForce;

                    else if (Time.time < (leftWallTouchTime + jumpBufferTime)) // if touching a wall to the left of the player

                        horizontalJumpForce = wallJumpForce;

                    if (transform.right.x < 0) // if facing the opposite direction

                        horizontalJumpForce = -horizontalJumpForce; // reverse horizontal jump force direction

                }

                playerRigidbody.AddForce(new Vector3(horizontalJumpForce, jumpAmount, 0), ForceMode.VelocityChange); //add vertical jump force

// play jump sound

                soundPlayer.PlaySound(soundPlayer.jumpSound, volume: 0.6f);

                StartCoroutine(CheckLandingSound()); // queue playing landing sound after landing

            }

            else if (!retry) // not touching the floor

            {

                jumpTryTime = Time.time; // set last jump try time

            }

        }

// play landing sound after jumping when player is touching the floor

        private IEnumerator CheckLandingSound()

        {

            yield return new WaitForSeconds(jumpBufferTime + 0.1f);

// wait until player is grounded

            while (!isGrounded)

                yield return null;

// play landing sound

            soundPlayer.PlaySound(soundPlayer.landingSound, volume: 0.6f);

        }

        #endregion

        #region movement

// control player movement drag in the ground/air

        private void SetDrag()

        {

            if (isGrounded)

                playerRigidbody.linearDamping = groundDrag;

            else

                playerRigidbody.linearDamping = airDrag;

        }

// set player velocity according to input

        private void MovePlayer()

        {

// get player input

            float verticalMoveAmount = Input.GetAxisRaw("Vertical");

            float horizontalMoveAmount = Input.GetAxisRaw("Horizontal");

            #region wall running

            if (wallrunningEnabled && !isCrouching && !touchingGround) // if wallrunning is enabled and not currently crouching or touching the ground

            {

// set wallrunning if touching a wall and moving forward

                isWallrunning = (touchingWallRight || touchingWallLeft) && verticalMoveAmount > 0;

                if (isWallrunning) // if currently wallrunning

                {

                    Physics.gravity = new Vector3(0, wallRunGravityReduction - gravity, 0); // reduce gravity

                    momentum += wallRunMomentumIncrease; // increase momentum

                    verticalMoveAmount = 1; // move forward

                    horizontalMoveAmount *= 0.3f; // reduce side movement

                }

                else // not wallrunning

                    Physics.gravity = new Vector3(0, -gravity, 0); // reset gravity

            }

            else

                isWallrunning = false;

            #endregion

// prevent the player from sticking to walls

            if (((touchingWallRight && horizontalMoveAmount > 0) || (touchingWallLeft && horizontalMoveAmount < 0))

                && !isWallrunning) // if trying to move into a wall and not wallrunning

                horizontalMoveAmount = 0; // zero horizontal move force

// calculate movement direction based on player's input

            Vector3 moveDirection = transform.forward * verticalMoveAmount + transform.right * horizontalMoveAmount;

// set default movement force for moving on the ground

            Vector3 moveForce = moveDirection.normalized;

            if (!isGrounded) // if moving in the air

                moveForce *= airMultiplier; // set air movement force

            #region slope

            else if (OnSlope()) // if moving on a slope

            {

// set movement to the slope direction

                Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);

                moveForce = slopeMoveDirection.normalized;

                if (!slopeSpeedDecrease) // if not decreasing movement speed on slopes

                    moveForce *= (1 + slopeMoveDirection.y); // set movement force accordingly

            }

            #endregion

            if (isRunning) // if currently running

            {

                if (runMomentumIncrease != 0) // if running increases momentum

                    momentum += runMomentumIncrease; // increase momentum

                moveForce *= runSpeed; // set speed to running speed

            }

            else if (isCrouching) // if currently crouching

                moveForce *= crouchSpeed; // set speed to walking speed

            else // not running or crouching

                moveForce *= walkSpeed; // set speed to walking speed

            if (useMomentum) // if momentum is enabled

                moveForce *= momentum; // increase movement speed by momentum amount

// apply force to the rigidbody

            playerRigidbody.AddForce(moveForce, ForceMode.Acceleration);

            CheckFootstepSound(moveDirection.magnitude); // handle footsteps sound

        }

// check if need to play or stop footsteps sound

        private void CheckFootstepSound(float movementAmount)

        {

            if (isGrounded && movementAmount > 0.1f && !isSliding) // if player is grounded and moving and not sliding

            {

                if (!soundPlayer.isPlaying || (soundPlayer.clip != soundPlayer.jumpSound && soundPlayer.clip != soundPlayer.landingSound)) // if sound is not already playing

                {

                    if (isRunning || isWallrunning) // if running

                    {

                        if (soundPlayer.clip != soundPlayer.runningSound || !soundPlayer.isPlaying) // if not already playing running sound

                            soundPlayer.PlaySound(soundPlayer.runningSound, loop: true); // play running sound

                    }

                    else if (soundPlayer.clip != soundPlayer.walkingSound || !soundPlayer.isPlaying) // walking and not already playing walking sound

                        soundPlayer.PlaySound(soundPlayer.walkingSound, loop: true); // play walking sound

                }

            }

            else if (soundPlayer.isPlaying && (soundPlayer.clip == soundPlayer.runningSound || soundPlayer.clip == soundPlayer.walkingSound)) // player stopped moving

                soundPlayer.Stop(); // stop footstep sounds

        }

// increase or reduce stamina depending if the player is currently running

        private IEnumerator ControlStamina()

        {

            currStamina = staminaDuration; // set start stamina amount

            while (true)

            {

                staminaText.text = $"Stamina - {currStamina.ToString("0.0")}"; // set stamina text

                if (isRunning && currStamina > 0) // if currently running and stamina not empty

                    currStamina -= 0.1f; // decrease stamina

                else if (currStamina < staminaDuration) // not running and stamina not full

                {

                    if (currStamina + (staminaFillRate / 10f) > staminaDuration) // if exceeds max stamina

                        currStamina = staminaDuration; // set stamina to max

                    else

                        currStamina += (staminaFillRate / 10f); // increase stamina

                }

                if (currStamina <= 0.1f) // if stamina ran out

                    staminaEmpty = true; // set stamina empty

                else if (staminaEmpty && currStamina >= staminaCooldown) // if player has enough stamina

                    staminaEmpty = false; // set stamina not empty

                yield return new WaitForSeconds(0.1f);

            }

        }

        #endregion

        #region rotation

// set player rotation based on mouse movement

        private void SetRotation()

        {

            if (isSliding) // if sliding

            {

                if (currentCameraLean < slideCameraLean)

                    currentCameraLean++; // lean left

            }

            else if (touchingWallRight && !touchingGround) // if touching a wall to the right of the player and not on the ground

            {

                if (currentCameraLean < wallRunCameraLean)

                    currentCameraLean++; // lean left

            }

            else if (touchingWallLeft && !touchingGround) // if touching a wall to the left of the player and not on the ground

            {

                if (-currentCameraLean < wallRunCameraLean)

                    currentCameraLean--; // lean right

            }

            else // not touching a wall and not sliding

            {

// reset camera lean

                if (currentCameraLean > 0)

                    currentCameraLean--;

                else if (currentCameraLean < 0)

                    currentCameraLean++;

            }

            currRotationX -= Input.GetAxis("Mouse Y") * lookSensitivity;
            currRotationX = Mathf.Clamp(currRotationX, -lookXLimit, lookXLimit);

            if (smoothLook)
            {
                float smoothTime = Mathf.Max(0.0001f, lookSmoothTime);

                currentCameraRotationX = Mathf.SmoothDampAngle(
                    currentCameraRotationX,
                    currRotationX,
                    ref cameraRotationXVelocity,
                    smoothTime);

                currentCameraLeanSmooth = Mathf.SmoothDamp(
                    currentCameraLeanSmooth,
                    currentCameraLean,
                    ref cameraLeanVelocity,
                    smoothTime);
            }
            else
            {
                currentCameraRotationX = currRotationX;
                currentCameraLeanSmooth = currentCameraLean;
                cameraRotationXVelocity = 0f;
                cameraLeanVelocity = 0f;
            }

            cameraTransform.localRotation = Quaternion.Euler(
                currentCameraRotationX,
                0f,
                currentCameraLeanSmooth);

            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSensitivity, 0);

        }

        #endregion

        #region momentum

// check if the player is slowing down to reset momentum

        private IEnumerator CheckMomentumReset()

        {

            while (true)

            {

// if player slowed down

                if (Mathf.Abs(playerRigidbody.linearVelocity.x) < (momentumResetThreshold * momentum)

                        && Mathf.Abs(playerRigidbody.linearVelocity.z) < (momentumResetThreshold * momentum)

                        && !touchingWallLeft && !touchingWallRight)

                    momentum = 1f; // reset momentum

                yield return new WaitForFixedUpdate(); // wait for next frame

            }

        }

// continuously reduce the player's momentum over time

        private IEnumerator ReduceMomentum()

        {

            while (true)

            {

                yield return new WaitForSeconds(1); // wait 1 second

// decrease momentum over time

                if ((momentum - momentumDecreaseRate) >= 1)

                    momentum -= momentumDecreaseRate;

            }

        }

        #endregion

        #region ground detection

        private bool touchingGround = false; // if the player is touching the floor

        private bool TouchingGround() => Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); // check if the player is touching the floor using collision sphere

        private bool touchingWallRight = false; // if the player is touching a wall to the right

        private bool TouchingWallRight() => Physics.CheckSphere(wallCheckRight.position, groundDistance * 2f, groundMask); // check if the player is touching a wall to the right using collision sphere

        private float rightWallTouchTime = 0; // the time when last touched a wall to the right

        private bool touchingWallLeft = false; // if the player is touching a wall to the left

        private bool TouchingWallLeft() => Physics.CheckSphere(wallCheckLeft.position, groundDistance * 2f, groundMask); // check if the player is touching a wall to the left using collision sphere

        private float leftWallTouchTime = 0; // the time when last touched a wall to the left

// turn off grounded status after a buffer time

        private IEnumerator DisableGrounded()

        {

            yield return new WaitForSeconds(jumpBufferTime); // delay

            isGrounded = false; // set grounded false

        }

// check if the player is touching a ground object

        private IEnumerator CheckGrounded()

        {

            while (true)

            {

                if (touchingGround || (wallJumpingEnabled && (touchingWallRight || touchingWallLeft))) // touching a ground surface

                {

                    isGrounded = true; // set grounded true

// refresh double jump when player is grounded

                    if (doubleJumpingEnabled)

                        hasDoubleJump = true;

                    if (Time.time <= (jumpTryTime + jumpBufferTime) && jumpTryTime != 0) // if player tried to jump lately before hitting the ground

                        Jump(retry: true); // try to jump

                }

                else // not touching the ground

                {

                    StartCoroutine(DisableGrounded()); // turn off grounded

                }

                yield return new WaitForFixedUpdate();

            }

        }

        #endregion

    }

}