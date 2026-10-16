using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Controls the ultra-premium tactical weapon bay card in the Loadout screen:
    /// - Continuous idle zero-G magnetic floating of the weapon silhouette.
    /// - Multi-layer 3D hover parallax: card elevates, weapon cutout lifts forward/upward, radial aura expands.
    /// - Tactile haptic click punch recoil and mechanical locking sound.
    /// - Dynamic stat comparison bar sweep from 0% to target fill on entrance.
    /// - Dynamic equipped badge state morphing (✓ EQUIPPED neon pill vs. EQUIP WEAPON ghost pill).
    /// - Fully unscaled time execution with robust null-safety and instant idle recovery.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadoutCardEffect : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Weapon Configuration")]
        [SerializeField] private int cardIndex = 0;
        [SerializeField] private Color themeColor = new Color(0f, 0.90f, 1f, 1f); // Electric Cyan (Sniper 1) or Solar Amber (Sniper 2)
        [SerializeField] private Color idleOutlineColor = new Color(0.20f, 0.28f, 0.38f, 0.50f);
        [SerializeField] private Color hoverOutlineColor = new Color(0.60f, 0.80f, 1f, 0.85f);

        [Header("Transforms & Holographic Bay")]
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private RectTransform weaponImageTransform;
        [SerializeField] private CanvasGroup radialGlowCanvasGroup;
        [SerializeField] private Outline cardOutline;

        [Header("Equip Status Badge")]
        [SerializeField] private TextMeshProUGUI equipBadgeText;
        [SerializeField] private Image equipBadgeBg;
        [SerializeField] private Outline equipBadgeOutline;

        [Header("Stat Dashboard Fill Bars")]
        [SerializeField] private Image[] statFillBars;
        [SerializeField] private float[] targetFillAmounts = new float[] { 0.95f, 0.35f, 0.50f, 0.40f };

        [Header("Zero-G Idle Float Settings")]
        [SerializeField] private float floatAmplitude = 3.5f;
        [SerializeField] private float floatSpeed = 2.4f;
        [SerializeField] private float floatPhase = 0f;

        [Header("3D Hover & Parallax Settings")]
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float clickScale = 0.97f;
        [SerializeField] private float parallaxWeaponOffset = 8f;
        [SerializeField] private float parallaxWeaponScale = 1.05f;
        [SerializeField] private float hoverTransitionDuration = 0.16f;
        [SerializeField] private float clickPunchDuration = 0.06f;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hoverTickClip;
        [SerializeField] private AudioClip equipConfirmClip;

        private bool _isEquipped = false;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private float _baseWeaponY = 0f;
        private bool _hasBaseWeaponY = false;

        private Coroutine _cardTransitionCoroutine;
        private Coroutine _statSweepCoroutine;

        // Themed visuals (bound at runtime by LoadoutPanelTheme). All optional / null-safe.
        private Image _cardFill;
        private Image _cardFrame;
        private Image[] _cornerAccents;
        private Image _bayFill;
        private Image _bayBrackets;
        private Image _badgeFrame;
        private Image _badgeIcon;
        private bool _hasThemeVisuals;

        public bool IsEquipped => _isEquipped;
        public bool IsHovered => _isHovered;
        public bool IsPressed => _isPressed;
        public int CardIndex => cardIndex;

        /// <summary>
        /// Binds the redesigned card visuals so equip / hover states can drive their colours.
        /// </summary>
        public void BindThemeVisuals(Color theme, Image cardFill, Image cardFrame, Image[] cornerAccents,
            Image bayFill, Image bayBrackets, Image badgeBg, Image badgeFrame, Image badgeIcon,
            TextMeshProUGUI badgeText, CanvasGroup auraGlow, Image[] fills, float[] fillTargets)
        {
            themeColor = theme;
            _cardFill = cardFill;
            _cardFrame = cardFrame;
            _cornerAccents = cornerAccents;
            _bayFill = bayFill;
            _bayBrackets = bayBrackets;
            _badgeFrame = badgeFrame;
            _badgeIcon = badgeIcon;
            if (badgeBg != null) equipBadgeBg = badgeBg;
            if (badgeText != null) equipBadgeText = badgeText;
            if (auraGlow != null) radialGlowCanvasGroup = auraGlow;
            if (fills != null && fills.Length > 0) statFillBars = fills;
            if (fillTargets != null && fillTargets.Length > 0) targetFillAmounts = fillTargets;
            _hasThemeVisuals = true;

            ApplyEquipVisuals(false);
        }


        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
        }

        private void OnEnable()
        {
            _isHovered = false;
            _isPressed = false;
            if (cardRoot == null) cardRoot = GetComponent<RectTransform>();
            if (cardRoot != null) cardRoot.localScale = Vector3.one;

            ApplyEquipVisuals(false);
        }

        private void OnDisable()
        {
            if (_cardTransitionCoroutine != null)
            {
                StopCoroutine(_cardTransitionCoroutine);
                _cardTransitionCoroutine = null;
            }
            if (_statSweepCoroutine != null)
            {
                StopCoroutine(_statSweepCoroutine);
                _statSweepCoroutine = null;
            }
            _isHovered = false;
            _isPressed = false;
            if (cardRoot != null) cardRoot.localScale = Vector3.one;
        }

        private void Update()
        {
            // Continuous Idle Zero-G Float for weapon cutout
            if (weaponImageTransform != null && gameObject.activeInHierarchy)
            {
                if (!_hasBaseWeaponY)
                {
                    _baseWeaponY = weaponImageTransform.anchoredPosition.y;
                    _hasBaseWeaponY = true;
                }

                float sineWave = Mathf.Sin(Time.unscaledTime * floatSpeed + floatPhase) * floatAmplitude;
                float targetY = _baseWeaponY + sineWave + (_isHovered ? parallaxWeaponOffset : 0f);
                Vector2 pos = weaponImageTransform.anchoredPosition;
                pos.y = Mathf.Lerp(pos.y, targetY, Time.unscaledDeltaTime * 12f);
                weaponImageTransform.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Auto-discovers and caches child components if not assigned in Inspector.
        /// </summary>
        public void InitializeComponents()
        {
            if (cardRoot == null)
                cardRoot = GetComponent<RectTransform>();

            if (cardOutline == null)
                cardOutline = GetComponent<Outline>();

            if (weaponImageTransform == null)
            {
                Transform wpnChild = transform.Find("WeaponCutout") ?? transform.Find("WeaponImage") ?? transform.Find("Image");
                if (wpnChild != null)
                    weaponImageTransform = wpnChild.GetComponent<RectTransform>();
            }

            if (weaponImageTransform != null && !_hasBaseWeaponY)
            {
                _baseWeaponY = weaponImageTransform.anchoredPosition.y;
                _hasBaseWeaponY = true;
            }

            if (radialGlowCanvasGroup == null)
            {
                Transform glowChild = transform.Find("RadialGlow") ?? transform.Find("AuraGlow");
                if (glowChild != null)
                    radialGlowCanvasGroup = glowChild.GetComponent<CanvasGroup>();
            }

            if (equipBadgeText == null)
            {
                Transform badgeText = transform.Find("EquipBadge/BadgeText") ?? transform.Find("BadgeText");
                if (badgeText != null)
                    equipBadgeText = badgeText.GetComponent<TextMeshProUGUI>();
            }

            if (equipBadgeBg == null)
            {
                Transform badge = transform.Find("EquipBadge");
                if (badge != null)
                {
                    equipBadgeBg = badge.GetComponent<Image>();
                    equipBadgeOutline = badge.GetComponent<Outline>();
                }
            }
        }

        /// <summary>
        /// Sets whether this card is currently selected / equipped.
        /// </summary>
        public void SetEquipped(bool equipped, bool playFeedback = false)
        {
            _isEquipped = equipped;
            ApplyEquipVisuals(playFeedback);

            if (equipped && playFeedback && audioSource != null && equipConfirmClip != null)
            {
                audioSource.PlayOneShot(equipConfirmClip, 0.85f);
            }
        }

        private void ApplyEquipVisuals(bool animatePunch)
        {
            if (_hasThemeVisuals)
            {
                ApplyThemeColors();
            }
            else
            {
                if (equipBadgeText != null)
                {
                    equipBadgeText.text = _isEquipped ? "✓  EQUIPPED" : "EQUIP WEAPON";
                    equipBadgeText.color = _isEquipped ? Color.white : new Color(0.85f, 0.90f, 0.96f, 0.85f);
                }

                if (equipBadgeBg != null)
                {
                    equipBadgeBg.color = _isEquipped
                        ? new Color(themeColor.r, themeColor.g, themeColor.b, 0.88f)
                        : new Color(0.08f, 0.13f, 0.19f, 0.65f);
                }

                if (equipBadgeOutline != null)
                {
                    equipBadgeOutline.effectColor = _isEquipped
                        ? new Color(themeColor.r, themeColor.g, themeColor.b, 1f)
                        : new Color(0.25f, 0.35f, 0.45f, 0.50f);
                }

                if (cardOutline != null)
                {
                    cardOutline.effectColor = _isEquipped ? themeColor : (_isHovered ? hoverOutlineColor : idleOutlineColor);
                    cardOutline.effectDistance = _isEquipped ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }

                if (radialGlowCanvasGroup != null)
                {
                    radialGlowCanvasGroup.alpha = _isEquipped ? 0.35f : (_isHovered ? 0.25f : 0.12f);
                }
            }

            if (animatePunch && cardRoot != null && gameObject.activeInHierarchy)
            {
                if (_cardTransitionCoroutine != null) StopCoroutine(_cardTransitionCoroutine);
                _cardTransitionCoroutine = StartCoroutine(AnimateEquipPunch());
            }
        }

        /// <summary>
        /// Redesigned card styling: equipped card gets a theme-tinted body and a solid theme button
        /// ("EQUIPPED"), others keep a dark body with theme border and a ghost "EQUIP WEAPON" button.
        /// </summary>
        private void ApplyThemeColors()
        {
            Color t = themeColor;
            Color darkBase = new Color(0.03f, 0.06f, 0.09f, 1f);
            Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

            if (_cardFill != null)
            {
                Color body = Color.Lerp(darkBase, t, _isEquipped ? 0.36f : (_isHovered ? 0.15f : 0.10f));
                _cardFill.color = Alpha(body, _isEquipped ? 0.84f : 0.86f);
            }

            if (_cardFrame != null)
                _cardFrame.color = Alpha(t, _isEquipped || _isHovered ? 1f : 0.80f);

            if (_cornerAccents != null)
            {
                foreach (var accent in _cornerAccents)
                    if (accent != null) accent.color = Alpha(Color.Lerp(t, Color.white, 0.15f), 1f);
            }

            if (_bayFill != null)
                _bayFill.color = Alpha(t, _isEquipped ? 0.30f : (_isHovered ? 0.28f : 0.22f));

            if (_bayBrackets != null)
                _bayBrackets.color = Alpha(t, _isEquipped || _isHovered ? 1f : 0.85f);

            if (radialGlowCanvasGroup != null)
                radialGlowCanvasGroup.alpha = _isEquipped ? 0.45f : (_isHovered ? 0.40f : 0.15f);

            Color textColor;
            if (_isEquipped)
            {
                textColor = new Color(t.r * 0.20f, t.g * 0.16f, t.b * 0.10f + 0.02f, 1f);
                if (equipBadgeBg != null) equipBadgeBg.color = Alpha(t, 1f);
                if (_badgeFrame != null) _badgeFrame.color = Alpha(Color.Lerp(t, Color.white, 0.30f), 1f);
            }
            else
            {
                textColor = new Color(0.88f, 0.94f, 0.98f, 1f);
                if (equipBadgeBg != null)
                    equipBadgeBg.color = _isHovered ? new Color(0.14f, 0.27f, 0.35f, 0.92f) : new Color(0.10f, 0.20f, 0.27f, 0.88f);
                if (_badgeFrame != null)
                    _badgeFrame.color = Alpha(Color.Lerp(t, Color.white, 0.35f), _isHovered ? 1f : 0.75f);
            }

            if (equipBadgeText != null)
            {
                equipBadgeText.text = _isEquipped ? "EQUIPPED" : "EQUIP WEAPON";
                equipBadgeText.color = textColor;
            }

            if (_badgeIcon != null)
            {
                _badgeIcon.sprite = _isEquipped ? LoadoutUISprites.IconSquare : LoadoutUISprites.IconCrosshair;
                _badgeIcon.color = textColor;

                // Keep icon + label visually centred as one group
                if (equipBadgeText != null)
                {
                    float textW = equipBadgeText.GetPreferredValues(equipBadgeText.text).x;
                    float iconW = _badgeIcon.rectTransform.sizeDelta.x;
                    const float gap = 12f;
                    float groupW = iconW + gap + textW;
                    _badgeIcon.rectTransform.anchoredPosition = new Vector2(-groupW * 0.5f + iconW * 0.5f, 0f);
                    equipBadgeText.rectTransform.anchoredPosition = new Vector2((iconW + gap) * 0.5f, 0f);
                }
            }
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (audioSource != null && hoverTickClip != null)
            {
                audioSource.PlayOneShot(hoverTickClip, 0.35f);
            }

            StartCardTransition(hoverScale, hoverTransitionDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            StartCardTransition(1.0f, hoverTransitionDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _isPressed = true;
            StartCardTransition(clickScale, clickPunchDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            _isPressed = false;
            StartCardTransition(_isHovered ? hoverScale : 1.0f, hoverTransitionDuration);
        }

        private void StartCardTransition(float targetScale, float duration)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_cardTransitionCoroutine != null)
                StopCoroutine(_cardTransitionCoroutine);

            _cardTransitionCoroutine = StartCoroutine(AnimateScale(targetScale, duration));

            // Outline & radial glow feedback
            if (_hasThemeVisuals)
            {
                ApplyThemeColors();
                return;
            }

            if (cardOutline != null && !_isEquipped)
            {
                cardOutline.effectColor = _isHovered ? hoverOutlineColor : idleOutlineColor;
            }

            if (radialGlowCanvasGroup != null && !_isEquipped)
            {
                radialGlowCanvasGroup.alpha = _isHovered ? 0.28f : 0.12f;
            }
        }

        private IEnumerator AnimateScale(float targetScale, float duration)
        {
            float elapsed = 0f;
            Vector3 startScale = cardRoot != null ? cardRoot.localScale : Vector3.one;
            Vector3 endScale = Vector3.one * targetScale;

            Vector3 startWeaponScale = weaponImageTransform != null ? weaponImageTransform.localScale : Vector3.one;
            Vector3 endWeaponScale = Vector3.one * (_isHovered ? parallaxWeaponScale : 1.0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);

                if (cardRoot != null)
                    cardRoot.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);

                if (weaponImageTransform != null)
                    weaponImageTransform.localScale = Vector3.LerpUnclamped(startWeaponScale, endWeaponScale, ease);

                yield return null;
            }

            if (cardRoot != null) cardRoot.localScale = endScale;
            if (weaponImageTransform != null) weaponImageTransform.localScale = endWeaponScale;
            _cardTransitionCoroutine = null;
        }

        private IEnumerator AnimateEquipPunch()
        {
            // Rapid energetic haptic punch (compression -> spring rebound)
            float elapsed = 0f;
            float punchDuration = 0.05f;
            Vector3 startScale = cardRoot.localScale;
            Vector3 punchScale = Vector3.one * 0.96f;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                cardRoot.localScale = Vector3.Lerp(startScale, punchScale, elapsed / punchDuration);
                yield return null;
            }

            elapsed = 0f;
            float reboundDuration = 0.12f;
            Vector3 reboundScale = Vector3.one * (_isHovered ? hoverScale : 1.0f);

            while (elapsed < reboundDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float ease = Mathf.SmoothStep(0f, 1f, elapsed / reboundDuration);
                cardRoot.localScale = Vector3.Lerp(punchScale, reboundScale, ease);
                yield return null;
            }

            cardRoot.localScale = reboundScale;
            _cardTransitionCoroutine = null;
        }

        /// <summary>
        /// Sweeps stat comparison fill bars dynamically from 0% to their target amounts.
        /// </summary>
        public void PlayStatFillSweep(float duration = 0.40f)
        {
            if (statFillBars == null || statFillBars.Length == 0 || !gameObject.activeInHierarchy) return;

            if (_statSweepCoroutine != null)
                StopCoroutine(_statSweepCoroutine);

            _statSweepCoroutine = StartCoroutine(AnimateStatSweep(duration));
        }

        private IEnumerator AnimateStatSweep(float duration)
        {
            // Reset all to 0
            for (int i = 0; i < statFillBars.Length; i++)
            {
                if (statFillBars[i] != null) statFillBars[i].fillAmount = 0f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Cubic ease-out
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                for (int i = 0; i < statFillBars.Length; i++)
                {
                    if (statFillBars[i] != null && targetFillAmounts != null && i < targetFillAmounts.Length)
                    {
                        statFillBars[i].fillAmount = targetFillAmounts[i] * ease;
                    }
                }

                yield return null;
            }

            for (int i = 0; i < statFillBars.Length; i++)
            {
                if (statFillBars[i] != null && targetFillAmounts != null && i < targetFillAmounts.Length)
                {
                    statFillBars[i].fillAmount = targetFillAmounts[i];
                }
            }

            _statSweepCoroutine = null;
        }
    }
}
