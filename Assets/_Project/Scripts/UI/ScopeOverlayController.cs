using UnityEngine;

namespace SniperGame.UI
{
    [RequireComponent(typeof(Animator))]
    public class ScopeOverlayController : SniperScopeOverlay
    {
    }

    [RequireComponent(typeof(Animator))]
    public class SniperScopeOverlay : MonoBehaviour
    {
        [Header("Animator & Core")]
        [SerializeField] private Animator scopeAnimator;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float tiltSmoothTime = 0.10f;
        [SerializeField] private float returnSmoothTime = 0.14f;

        [Header("Aim Look Sway (Mouse Inertia)")]
        [SerializeField] private bool enableMouseSway = true;
        [SerializeField] private float mouseSwayAmount = 5.5f;
        [SerializeField] private float maxMouseSway = 18f;
        [SerializeField] private float mouseRollAmount = 0.35f;
        [SerializeField] private float maxMouseRoll = 1.2f;
        [SerializeField] private float mouseSwaySmoothTime = 0.065f;

        [Header("Optical Depth (Eyebox Parallax)")]
        [SerializeField] private bool enableParallax = true;
        [SerializeField] private float housingParallax = 0.28f;
        [SerializeField] private float reticleParallax = -0.10f;

        [Header("Movement Walk Bob")]
        [SerializeField] private bool enableWalkBob = true;
        [SerializeField] private float walkBobAmountY = 3.0f;
        [SerializeField] private float walkBobAmountX = 1.8f;
        [SerializeField] private float walkBobSpeed = 8.5f;

        [Header("Scope Fire Recoil Kick")]
        [SerializeField] private bool enableRecoilKick = true;
        [SerializeField] private float recoilKickY = 16f;
        [SerializeField] private float recoilKickX = 4f;
        [SerializeField] private float recoilKickRoll = 0.75f;
        [SerializeField] private float recoilRecoverTime = 0.14f;

        [Header("Transforms (Auto-Resolved)")]
        [SerializeField] private RectTransform scopePivot;
        [SerializeField] private RectTransform scopeHousing;
        [SerializeField] private RectTransform reticleLines;

        private float _currentStrafe = 0f;
        private float _strafeVelocity = 0f;
        private bool _isScoped = false;
        private float _adsWeight = 0f;

        // Mouse look sway state
        private Vector2 _currentMouseSway = Vector2.zero;
        private Vector2 _mouseSwayVelocity = Vector2.zero;
        private float _currentMouseRoll = 0f;
        private float _mouseRollVelocity = 0f;

        // Walk bob state
        private float _walkBobTimer = 0f;
        private Vector2 _currentWalkBob = Vector2.zero;
        private Vector2 _walkBobVelocity = Vector2.zero;

        // Recoil kick state
        private Vector2 _currentRecoil = Vector2.zero;
        private Vector2 _recoilVelocity = Vector2.zero;
        private float _currentRecoilRoll = 0f;
        private float _recoilRollVelocity = 0f;

        // Base transform positions
        private Vector2 _initialPivotPos = Vector2.zero;
        private Vector2 _initialHousingPos = Vector2.zero;
        private Vector2 _initialReticlePos = Vector2.zero;

        private static readonly int IsScopedHash = Animator.StringToHash("IsScoped");
        private static readonly int StrafeHash = Animator.StringToHash("Strafe");

        private void Awake()
        {
            if (scopeAnimator == null)
            {
                scopeAnimator = GetComponent<Animator>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            ResolveTransforms();
        }

        private void ResolveTransforms()
        {
            if (scopePivot == null)
            {
                Transform pivotChild = transform.Find("ScopePivot");
                if (pivotChild != null)
                {
                    scopePivot = pivotChild as RectTransform;
                }
                else if (transform.childCount > 0)
                {
                    scopePivot = transform.GetChild(0) as RectTransform;
                }
            }

            if (scopePivot != null)
            {
                _initialPivotPos = scopePivot.anchoredPosition;

                if (scopeHousing == null)
                {
                    Transform housingChild = scopePivot.Find("Scope");
                    if (housingChild != null) scopeHousing = housingChild as RectTransform;
                }

                if (reticleLines == null)
                {
                    Transform reticleChild = scopePivot.Find("ReticleLines");
                    if (reticleChild != null) reticleLines = reticleChild as RectTransform;
                }

                if (scopeHousing != null) _initialHousingPos = scopeHousing.anchoredPosition;
                if (reticleLines != null) _initialReticlePos = reticleLines.anchoredPosition;
            }
        }

        public void SetScoped(bool scoped)
        {
            _isScoped = scoped;

            if (scopeAnimator == null)
            {
                scopeAnimator = GetComponent<Animator>();
            }

            if (scopeAnimator != null)
            {
                scopeAnimator.SetBool(IsScopedHash, scoped);
            }

            if (!scoped)
            {
                _currentStrafe = 0f;
                _strafeVelocity = 0f;
                if (scopeAnimator != null)
                {
                    scopeAnimator.SetFloat(StrafeHash, 0f);
                }
            }
        }

        public void TriggerFireRecoil(float kickMultiplier = 1f)
        {
            if (!_isScoped || !enableRecoilKick) return;

            _recoilVelocity.y += recoilKickY * kickMultiplier;
            _recoilVelocity.x += Random.Range(-recoilKickX, recoilKickX) * kickMultiplier;
            _recoilRollVelocity += Random.Range(-recoilKickRoll, recoilKickRoll) * kickMultiplier;
        }

        private void Update()
        {
            float targetWeight = _isScoped ? 1f : 0f;
            // Snappy 0.09s fade in, ultra-fast 0.04s fade out on scope release
            float fadeTime = _isScoped ? 0.09f : 0.04f;
            _adsWeight = Mathf.MoveTowards(_adsWeight, targetWeight, Time.deltaTime / fadeTime);

            UpdateStrafeBlend();
            UpdateProceduralSway();
        }

        private void LateUpdate()
        {
            // Once scope-out completes, guarantee canvasGroup is cleanly 0 with zero ghosting
            if (!_isScoped && canvasGroup != null)
            {
                if (_adsWeight <= 0.001f && canvasGroup.alpha > 0f)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        private void UpdateStrafeBlend()
        {
            if (scopeAnimator == null) return;

            float targetStrafe = _isScoped ? Input.GetAxisRaw("Horizontal") : 0f;
            float smoothTime = Mathf.Abs(targetStrafe) > 0.05f ? tiltSmoothTime : returnSmoothTime;

            _currentStrafe = Mathf.SmoothDamp(_currentStrafe, targetStrafe, ref _strafeVelocity, smoothTime);
            scopeAnimator.SetFloat(StrafeHash, _currentStrafe);
        }

        private void UpdateProceduralSway()
        {
            if (scopePivot == null) return;

            if (_adsWeight <= 0.001f)
            {
                ResetProceduralState();
                return;
            }

            // 1. Mouse Aiming Inertia / Weapon Drag
            if (enableMouseSway && _isScoped)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                Vector2 targetLag = new Vector2(-mouseX * mouseSwayAmount, -mouseY * mouseSwayAmount);
                targetLag.x = Mathf.Clamp(targetLag.x, -maxMouseSway, maxMouseSway);
                targetLag.y = Mathf.Clamp(targetLag.y, -maxMouseSway, maxMouseSway);

                float targetRoll = Mathf.Clamp(-mouseX * mouseRollAmount, -maxMouseRoll, maxMouseRoll);

                _currentMouseSway = Vector2.SmoothDamp(_currentMouseSway, targetLag, ref _mouseSwayVelocity, mouseSwaySmoothTime);
                _currentMouseRoll = Mathf.SmoothDamp(_currentMouseRoll, targetRoll, ref _mouseRollVelocity, mouseSwaySmoothTime);
            }
            else
            {
                _currentMouseSway = Vector2.SmoothDamp(_currentMouseSway, Vector2.zero, ref _mouseSwayVelocity, 0.05f);
                _currentMouseRoll = Mathf.SmoothDamp(_currentMouseRoll, 0f, ref _mouseRollVelocity, 0.05f);
            }

            // 2. Movement Step Bob
            if (enableWalkBob && _isScoped)
            {
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveY = Input.GetAxisRaw("Vertical");
                bool isMoving = (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f);

                if (isMoving)
                {
                    _walkBobTimer += Time.deltaTime * walkBobSpeed;
                    float bobY = Mathf.Sin(_walkBobTimer * 2f) * walkBobAmountY;
                    float bobX = Mathf.Cos(_walkBobTimer) * walkBobAmountX;
                    _currentWalkBob = Vector2.SmoothDamp(_currentWalkBob, new Vector2(bobX, bobY), ref _walkBobVelocity, 0.08f);
                }
                else
                {
                    _currentWalkBob = Vector2.SmoothDamp(_currentWalkBob, Vector2.zero, ref _walkBobVelocity, 0.08f);
                }
            }
            else
            {
                _currentWalkBob = Vector2.SmoothDamp(_currentWalkBob, Vector2.zero, ref _walkBobVelocity, 0.05f);
            }

            // 3. Recoil Kick Spring
            _currentRecoil = Vector2.SmoothDamp(_currentRecoil, Vector2.zero, ref _recoilVelocity, recoilRecoverTime);
            _currentRecoilRoll = Mathf.SmoothDamp(_currentRecoilRoll, 0f, ref _recoilRollVelocity, recoilRecoverTime);

            // 4. Composite Offsets & Dynamic Optical Scale
            Vector2 combinedPos = (_currentMouseSway + _currentWalkBob + _currentRecoil) * _adsWeight;
            float combinedRoll = (_currentMouseRoll + _currentRecoilRoll) * _adsWeight;

            scopePivot.anchoredPosition = _initialPivotPos + combinedPos;
            scopePivot.localEulerAngles = new Vector3(0f, 0f, combinedRoll);

            // Dynamic optical eye-rush scale (subtly expands optic as it meets eye)
            float adsScale = Mathf.Lerp(0.92f, 1.0f, _adsWeight);
            scopePivot.localScale = new Vector3(adsScale, adsScale, 1f);

            // 5. Eyebox Parallax Depth
            if (enableParallax)
            {
                if (scopeHousing != null)
                {
                    scopeHousing.anchoredPosition = _initialHousingPos + (_currentMouseSway * (housingParallax * _adsWeight));
                }

                if (reticleLines != null)
                {
                    reticleLines.anchoredPosition = _initialReticlePos + (_currentMouseSway * (reticleParallax * _adsWeight));
                }
            }
        }

        private void ResetProceduralState()
        {
            _currentMouseSway = Vector2.zero;
            _mouseSwayVelocity = Vector2.zero;
            _currentMouseRoll = 0f;
            _mouseRollVelocity = 0f;

            _currentWalkBob = Vector2.zero;
            _walkBobVelocity = Vector2.zero;

            _currentRecoil = Vector2.zero;
            _recoilVelocity = Vector2.zero;
            _currentRecoilRoll = 0f;
            _recoilRollVelocity = 0f;

            if (scopePivot != null)
            {
                scopePivot.anchoredPosition = _initialPivotPos;
                scopePivot.localEulerAngles = Vector3.zero;
                scopePivot.localScale = Vector3.one;
            }

            if (scopeHousing != null)
            {
                scopeHousing.anchoredPosition = _initialHousingPos;
            }

            if (reticleLines != null)
            {
                reticleLines.anchoredPosition = _initialReticlePos;
            }

            if (!_isScoped && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void OnDisable()
        {
            ResetProceduralState();
        }
    }
}