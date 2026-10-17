using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Delivers premium AAA-style hover and click animations for main menu buttons:
    /// - Transparent default state with crisp typography.
    /// - Smooth right-pointing arrow fade-in and subtle forward glide on hover.
    /// - Subtle text brightening with emerald/cyan accent glow.
    /// - Responsive 1.03x scale on hover and tactile 0.97x compression on click.
    /// - 100% unscaled time execution with robust rapid-cursor recovery.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public class MainMenuButtonEffect : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Component References")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private RectTransform arrowTransform;
        [SerializeField] private CanvasGroup arrowCanvasGroup;
        [SerializeField] private Image arrowImage;
        [Header("Arrow Styling")]
        [SerializeField] private Color arrowColor = new Color(0.92f, 0.98f, 1.0f, 1f);
        [SerializeField] private Vector2 arrowSize = new Vector2(40f, 34f);
        [SerializeField] private float arrowMargin = 12f;
        [SerializeField] private float arrowSlideDistance = 6f;

        [Header("Arrow Sprites")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite rightArrowSprite;

        [Header("Right Arrow Settings (Optional)")]
        [SerializeField] private RectTransform rightArrowTransform;
        [SerializeField] private CanvasGroup rightArrowCanvasGroup;
        [SerializeField] private Image rightArrowImage;

        [Header("Border & Fill Styling")]
        [SerializeField] private bool showBorder = true;
        [SerializeField] private Color borderIdleColor = new Color(0.25f, 0.42f, 0.52f, 0.60f);
        [SerializeField] private Color borderHoverColor = new Color(0.38f, 0.65f, 0.80f, 0.90f);
        [SerializeField] private Color fillIdleColor = new Color(0.035f, 0.07f, 0.11f, 0.70f);
        [SerializeField] private Color fillHoverColor = new Color(0.05f, 0.10f, 0.16f, 0.85f);
        [SerializeField] private Image buttonBgImage;
        [SerializeField] private Image buttonBorderImage;

        [Header("Text Styling")]
        [SerializeField] private Color idleTextColor = new Color(0.85f, 0.90f, 0.93f, 1f);
        [SerializeField] private Color hoverTextColor = new Color(1.0f, 1.0f, 1.0f, 1f);
        [SerializeField] private Color clickTextColor = new Color(0.70f, 1.0f, 0.90f, 1f);

        [Header("Animation Timings & Scales")]
        [SerializeField] private float hoverScale = 1.03f;
        [SerializeField] private float clickScale = 0.97f;
        [SerializeField] private float transitionDuration = 0.18f;
        [SerializeField] private float clickDuration = 0.08f;

        private RectTransform _rectTransform;
        private Coroutine _activeTransition;
        private bool _isHovered = false;
        private bool _isPressed = false;

        private Vector2 _arrowTargetPos;
        private Vector2 _arrowRestPos;
        private Vector2 _rightArrowTargetPos;
        private Vector2 _rightArrowRestPos;
        private Vector3 _baseScale = Vector3.one;

        private static Sprite _cachedLeftArrow;
        private static Sprite _cachedRightArrow;

        public static Sprite GetDefaultLeftArrow()
        {
            if (_cachedLeftArrow != null) return _cachedLeftArrow;
            _cachedLeftArrow = Resources.Load<Sprite>("UI/Symbol Pijl rechts");
#if UNITY_EDITOR
            if (_cachedLeftArrow == null)
            {
                _cachedLeftArrow = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Symbol Pijl rechts.png");
            }
#endif
            return _cachedLeftArrow;
        }

        public static Sprite GetDefaultRightArrow()
        {
            if (_cachedRightArrow != null) return _cachedRightArrow;
            _cachedRightArrow = Resources.Load<Sprite>("UI/Symbol Pijl links");
#if UNITY_EDITOR
            if (_cachedRightArrow == null)
            {
                _cachedRightArrow = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Symbol Pijl links.png");
            }
#endif
            return _cachedRightArrow;
        }

        public CanvasGroup ArrowCanvasGroup => arrowCanvasGroup;
        public CanvasGroup RightArrowCanvasGroup => rightArrowCanvasGroup;
        public RectTransform ArrowTransform => arrowTransform;
        public RectTransform RightArrowTransform => rightArrowTransform;
        public Image ArrowImage => arrowImage;
        public Image RightArrowImage => rightArrowImage;

        public Sprite ArrowSprite
        {
            get => arrowSprite;
            set
            {
                arrowSprite = value;
                if (arrowImage != null && arrowSprite != null)
                {
                    arrowImage.sprite = arrowSprite;
                }
            }
        }

        public Sprite RightArrowSprite
        {
            get => rightArrowSprite;
            set
            {
                rightArrowSprite = value;
                if (rightArrowImage != null && rightArrowSprite != null)
                {
                    rightArrowImage.sprite = rightArrowSprite;
                }
            }
        }

        public Image ButtonBorderImage => buttonBorderImage;
        public Image ButtonBgImage => buttonBgImage;
        public bool ShowBorder
        {
            get => showBorder;
            set
            {
                showBorder = value;
                if (buttonBorderImage != null) buttonBorderImage.gameObject.SetActive(showBorder);
            }
        }

        public bool IsHovered => _isHovered;
        public bool IsPressed => _isPressed;

        public void ConfigureColors(
            Color fillIdle, Color fillHover,
            Color borderIdle, Color borderHover,
            Color textIdle, Color textHover,
            Color? clickText = null,
            Color? arrowCol = null)
        {
            fillIdleColor = fillIdle;
            fillHoverColor = fillHover;
            borderIdleColor = borderIdle;
            borderHoverColor = borderHover;
            idleTextColor = textIdle;
            hoverTextColor = textHover;
            clickTextColor = clickText.HasValue ? clickText.Value : textHover;
            if (arrowCol.HasValue)
            {
                arrowColor = arrowCol.Value;
                if (arrowImage != null) arrowImage.color = arrowColor;
                if (rightArrowImage != null) rightArrowImage.color = arrowColor;
            }
        }

        public void ConfigureArrows(Vector2 size, float margin = 12f, float slideDistance = 6f)
        {
            arrowSize = size;
            arrowMargin = margin;
            arrowSlideDistance = slideDistance;
            if (arrowTransform != null) arrowTransform.sizeDelta = arrowSize;
            if (rightArrowTransform != null) rightArrowTransform.sizeDelta = arrowSize;
            CalculateArrowPositions();
            ApplyInstantIdleState();
        }

        private void Awake()
        {
            InitializeComponents();
            FindRightArrow();
            CalculateArrowPositions();
            ApplyInstantIdleState();
        }

        private void OnEnable()
        {
            _isHovered = false;
            _isPressed = false;
            InitializeComponents();
            FindRightArrow();
            CalculateArrowPositions();
            ApplyInstantIdleState();
        }

        private void OnDisable()
        {
            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
                _activeTransition = null;
            }
            _isHovered = false;
            _isPressed = false;
            ApplyInstantIdleState();
        }

        /// <summary>
        /// Configures the rounded button fill and steel-cyan rounded rectangle outline.
        /// </summary>
        public void SetupBorderAndFill()
        {
            if (!showBorder) return;

            if (buttonBgImage == null)
            {
                buttonBgImage = GetComponent<Image>();
            }

            if (buttonBgImage != null)
            {
                if (buttonBgImage.sprite == null)
                    buttonBgImage.sprite = JoinUISprites.ButtonFill;
                buttonBgImage.type = Image.Type.Sliced;
                buttonBgImage.color = fillIdleColor;
                buttonBgImage.raycastTarget = true;
            }

            var legacyOutline = GetComponent<Outline>();
            if (legacyOutline != null) legacyOutline.enabled = false;

            if (buttonBorderImage == null)
            {
                Transform borderTr = transform.Find("ButtonBorder") ?? transform.Find("Border") ?? transform.Find("ButtonFrame");
                if (borderTr != null)
                {
                    buttonBorderImage = borderTr.GetComponent<Image>();
                }
            }

            if (buttonBorderImage == null)
            {
                GameObject borderObj = new GameObject("ButtonBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                borderObj.layer = gameObject.layer;
                borderObj.transform.SetParent(transform, false);

                RectTransform bRt = borderObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.localScale = Vector3.one;

                buttonBorderImage = borderObj.GetComponent<Image>();
            }

            if (buttonBorderImage != null)
            {
                buttonBorderImage.gameObject.SetActive(true);
                if (buttonBorderImage.sprite == null)
                    buttonBorderImage.sprite = JoinUISprites.ButtonFrame;
                buttonBorderImage.type = Image.Type.Sliced;
                buttonBorderImage.color = borderIdleColor;
                buttonBorderImage.raycastTarget = false;

                // Keep border behind text and arrows (index 0)
                buttonBorderImage.transform.SetSiblingIndex(0);
            }
        }

        /// <summary>
        /// Automatically discovers or configures required UI components.
        /// </summary>
        public void InitializeComponents()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            // Ensure crisp, uncropped arrow proportions matching native 421x358 texture ratio (40x34)
            if (arrowSize == new Vector2(26f, 22f) || arrowSize == new Vector2(24f, 24f) || arrowSize == Vector2.zero)
            {
                arrowSize = new Vector2(40f, 34f);
            }
            if (arrowMargin <= 0f)
            {
                arrowMargin = 12f;
            }
            if (arrowSlideDistance <= 0f)
            {
                arrowSlideDistance = 6f;
            }

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                // Disable Unity's default color tint transition so it doesn't fight our custom animation
                button.transition = Selectable.Transition.None;
            }

            SetupBorderAndFill();

            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (buttonText != null)
            {
                buttonText.raycastTarget = false;
            }

            if (arrowSprite == null)
            {
                arrowSprite = GetDefaultLeftArrow();
            }

            if (rightArrowSprite == null)
            {
                rightArrowSprite = GetDefaultRightArrow();
            }

            // Look for existing left arrow child or create one
            if (arrowTransform == null)
            {
                Transform existingArrow = transform.Find("Arrow") ?? transform.Find("HoverArrow") ?? transform.Find("HoverArrowLeft") ?? transform.Find("ArrowLeft");
                if (existingArrow == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        string cName = child.name;
                        if (cName.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (cName.IndexOf("Border", System.StringComparison.OrdinalIgnoreCase) >= 0 || cName.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (cName.IndexOf("Arrow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            existingArrow = child;
                            break;
                        }
                    }
                }

                if (existingArrow != null)
                {
                    arrowTransform = existingArrow.GetComponent<RectTransform>();
                    arrowCanvasGroup = existingArrow.GetComponent<CanvasGroup>();
                    arrowImage = existingArrow.GetComponent<Image>();
                }
                else
                {
                    CreateArrowChild();
                }
            }

            if (arrowTransform != null)
            {
                arrowTransform.sizeDelta = arrowSize;
            }

            if (arrowCanvasGroup == null && arrowTransform != null)
            {
                arrowCanvasGroup = arrowTransform.GetComponent<CanvasGroup>();
                if (arrowCanvasGroup == null)
                    arrowCanvasGroup = arrowTransform.gameObject.AddComponent<CanvasGroup>();
            }

            if (arrowCanvasGroup != null)
            {
                arrowCanvasGroup.interactable = false;
                arrowCanvasGroup.blocksRaycasts = false;
            }

            if (arrowImage == null && arrowTransform != null)
            {
                arrowImage = arrowTransform.GetComponent<Image>();
            }

            if (arrowImage != null)
            {
                if (arrowSprite != null)
                {
                    arrowImage.sprite = arrowSprite;
                }
                arrowImage.color = arrowColor;
                arrowImage.raycastTarget = false;
                arrowImage.preserveAspect = true;
            }

            FindRightArrow();
        }

        /// <summary>
        /// Detects any right-side arrow child added to the button (e.g. HoverArrowRight, HoverArrow (1), RightArrow),
        /// or creates one if it doesn't exist.
        /// </summary>
        public void FindRightArrow()
        {
            if (rightArrowSprite == null)
            {
                rightArrowSprite = GetDefaultRightArrow();
            }

            if (rightArrowTransform == null)
            {
                string[] candidates = new string[]
                {
                    "HoverArrowRight", "RightArrow", "ArrowRight", "HoverArrow (1)",
                    "Arrow (1)", "Right HoverArrow", "HoverArrow_Right", "Arrow_Right",
                    "HoverArrow2", "Arrow2", "RightHoverArrow"
                };

                foreach (string candidate in candidates)
                {
                    Transform found = transform.Find(candidate);
                    if (found != null && found != arrowTransform)
                    {
                        rightArrowTransform = found.GetComponent<RectTransform>();
                        break;
                    }
                }

                if (rightArrowTransform == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (child == arrowTransform) continue;
                        if (buttonText != null && child == buttonText.transform) continue;

                        string cName = child.name;
                        if (cName.IndexOf("Line", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                        if (cName.IndexOf("Border", System.StringComparison.OrdinalIgnoreCase) >= 0 || cName.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

                        if (cName.IndexOf("Arrow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            cName.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            rightArrowTransform = child.GetComponent<RectTransform>();
                            break;
                        }
                    }
                }

                if (rightArrowTransform == null)
                {
                    CreateRightArrowChild();
                }
            }

            if (rightArrowTransform != null)
            {
                if (!rightArrowTransform.gameObject.activeSelf)
                    rightArrowTransform.gameObject.SetActive(true);

                // Ensure right arrow scale is non-mirrored so Symbol Pijl links points left
                Vector3 currentScale = rightArrowTransform.localScale;
                if (currentScale.x < 0f)
                {
                    rightArrowTransform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
                }

                rightArrowTransform.sizeDelta = arrowSize;

                if (rightArrowCanvasGroup == null)
                {
                    rightArrowCanvasGroup = rightArrowTransform.GetComponent<CanvasGroup>();
                    if (rightArrowCanvasGroup == null)
                        rightArrowCanvasGroup = rightArrowTransform.gameObject.AddComponent<CanvasGroup>();
                }

                rightArrowCanvasGroup.interactable = false;
                rightArrowCanvasGroup.blocksRaycasts = false;

                if (rightArrowImage == null)
                {
                    rightArrowImage = rightArrowTransform.GetComponent<Image>();
                }

                if (rightArrowImage != null)
                {
                    if (rightArrowSprite != null)
                    {
                        rightArrowImage.sprite = rightArrowSprite;
                    }
                    rightArrowImage.color = arrowColor;
                    rightArrowImage.raycastTarget = false;
                    rightArrowImage.preserveAspect = true;
                }
            }
        }

        private void CreateArrowChild()
        {
            GameObject arrowObj = new GameObject("HoverArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            arrowObj.layer = gameObject.layer;
            arrowObj.transform.SetParent(transform, false);

            arrowTransform = arrowObj.GetComponent<RectTransform>();
            arrowTransform.sizeDelta = arrowSize;
            arrowTransform.anchorMin = new Vector2(0.5f, 0.5f);
            arrowTransform.anchorMax = new Vector2(0.5f, 0.5f);
            arrowTransform.pivot = new Vector2(0.5f, 0.5f);
            arrowTransform.localScale = Vector3.one;

            arrowCanvasGroup = arrowObj.GetComponent<CanvasGroup>();
            arrowCanvasGroup.alpha = 0f;
            arrowCanvasGroup.interactable = false;
            arrowCanvasGroup.blocksRaycasts = false;

            arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.raycastTarget = false;
            arrowImage.color = arrowColor;
            arrowImage.preserveAspect = true;

            if (arrowSprite != null)
            {
                arrowImage.sprite = arrowSprite;
            }
            else
            {
                arrowImage.sprite = GetDefaultLeftArrow();
            }
        }

        private void CreateRightArrowChild()
        {
            GameObject rightArrowObj = new GameObject("HoverArrowRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            rightArrowObj.layer = gameObject.layer;
            rightArrowObj.transform.SetParent(transform, false);

            rightArrowTransform = rightArrowObj.GetComponent<RectTransform>();
            rightArrowTransform.sizeDelta = arrowSize;
            rightArrowTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rightArrowTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rightArrowTransform.pivot = new Vector2(0.5f, 0.5f);
            rightArrowTransform.localScale = Vector3.one;

            rightArrowCanvasGroup = rightArrowObj.GetComponent<CanvasGroup>();
            rightArrowCanvasGroup.alpha = 0f;
            rightArrowCanvasGroup.interactable = false;
            rightArrowCanvasGroup.blocksRaycasts = false;

            rightArrowImage = rightArrowObj.GetComponent<Image>();
            rightArrowImage.raycastTarget = false;
            rightArrowImage.color = arrowColor;
            rightArrowImage.preserveAspect = true;

            if (rightArrowSprite != null)
            {
                rightArrowImage.sprite = rightArrowSprite;
            }
            else
            {
                rightArrowImage.sprite = GetDefaultRightArrow();
            }
        }

        /// <summary>
        /// Computes the exact idle and hovered positions of arrows so they sit cleanly
        /// flanking the button text without any overlap, strictly clamped inside the button outline.
        /// </summary>
        public void CalculateArrowPositions()
        {
            if (buttonText == null) return;

            buttonText.ForceMeshUpdate();

            // Text rendered width in button space
            float textWidth = buttonText.preferredWidth;
            if (textWidth <= 0f && buttonText.textBounds.size.x > 0f)
            {
                textWidth = buttonText.textBounds.size.x;
            }
            float halfTextWidth = textWidth * 0.5f;

            float textCenterX = 0f;
            if (buttonText.rectTransform != null && buttonText.rectTransform.anchorMin.x == buttonText.rectTransform.anchorMax.x)
            {
                textCenterX = buttonText.rectTransform.anchoredPosition.x;
            }
            float textCenterY = 0f;
            if (buttonText.rectTransform != null && buttonText.rectTransform.anchorMin.y == buttonText.rectTransform.anchorMax.y)
            {
                textCenterY = buttonText.rectTransform.anchoredPosition.y;
            }

            float buttonWidth = (_rectTransform != null && _rectTransform.rect.width > 0f) ? _rectTransform.rect.width : 280f;
            float buttonHalfWidth = buttonWidth * 0.5f;
            const float edgePadding = 12f; // Guarantees arrows never touch or cross the rounded button outline frame

            if (arrowTransform != null)
            {
                arrowTransform.sizeDelta = arrowSize;
                float arrowWidth = arrowSize.x;
                float halfArrowWidth = arrowWidth * 0.5f;

                // Left boundary of text
                float textLeft = textCenterX - halfTextWidth;

                // Ideal target (hovered) sits arrowMargin to the left of text
                float idealTargetX = textLeft - halfArrowWidth - arrowMargin;
                // Ideal rest (idle) sits arrowSlideDistance further left so it slides forward on hover
                float idealRestX = idealTargetX - arrowSlideDistance;

                // Boundary clamping: arrow must remain strictly inside [-buttonHalfWidth + edgePadding, textLeft - 4f]
                float minX = -buttonHalfWidth + edgePadding + halfArrowWidth;
                float maxX = textLeft - 4f - halfArrowWidth;

                if (minX > maxX)
                {
                    float midX = (-buttonHalfWidth + edgePadding + textLeft - 4f) * 0.5f;
                    minX = midX;
                    maxX = midX;
                }

                float restX = Mathf.Clamp(idealRestX, minX, maxX);
                float targetX = Mathf.Clamp(idealTargetX, minX, maxX);

                // Ensure slide direction is forward (left-to-right toward the text)
                if (targetX < restX) targetX = restX;

                _arrowRestPos = new Vector2(restX, textCenterY);
                _arrowTargetPos = new Vector2(targetX, textCenterY);
            }

            if (rightArrowTransform != null)
            {
                rightArrowTransform.sizeDelta = arrowSize;
                float rightArrowWidth = arrowSize.x;
                float halfRightArrowWidth = rightArrowWidth * 0.5f;

                // Right boundary of text
                float textRight = textCenterX + halfTextWidth;

                // Ideal target (hovered) sits arrowMargin to the right of text
                float idealTargetX = textRight + halfRightArrowWidth + arrowMargin;
                // Ideal rest (idle) sits arrowSlideDistance further right so it slides forward (inward) on hover
                float idealRestX = idealTargetX + arrowSlideDistance;

                // Boundary clamping: arrow must remain strictly inside [textRight + 4f, buttonHalfWidth - edgePadding]
                float minX = textRight + 4f + halfRightArrowWidth;
                float maxX = buttonHalfWidth - edgePadding - halfRightArrowWidth;

                if (minX > maxX)
                {
                    float midX = (textRight + 4f + buttonHalfWidth - edgePadding) * 0.5f;
                    minX = midX;
                    maxX = midX;
                }

                float restX = Mathf.Clamp(idealRestX, minX, maxX);
                float targetX = Mathf.Clamp(idealTargetX, minX, maxX);

                // Ensure slide direction is forward (right-to-left toward the text)
                if (targetX > restX) targetX = restX;

                _rightArrowRestPos = new Vector2(restX, textCenterY);
                _rightArrowTargetPos = new Vector2(targetX, textCenterY);
            }
        }

        /// <summary>
        /// Immediately applies the idle appearance with no interpolation.
        /// </summary>
        public void ApplyInstantIdleState()
        {
            if (_rectTransform != null)
                _rectTransform.localScale = _baseScale;

            if (buttonText != null)
                buttonText.color = idleTextColor;

            if (showBorder)
            {
                if (buttonBorderImage == null || buttonBgImage == null)
                {
                    SetupBorderAndFill();
                }

                if (buttonBorderImage != null)
                    buttonBorderImage.color = borderIdleColor;

                if (buttonBgImage != null)
                    buttonBgImage.color = fillIdleColor;
            }

            if (arrowCanvasGroup != null)
                arrowCanvasGroup.alpha = 0f;

            if (arrowTransform != null)
                arrowTransform.anchoredPosition = _arrowRestPos;

            if (rightArrowCanvasGroup != null)
                rightArrowCanvasGroup.alpha = 0f;

            if (rightArrowTransform != null)
                rightArrowTransform.anchoredPosition = _rightArrowRestPos;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            CalculateArrowPositions();
            StartTransition(
                targetScale: hoverScale,
                targetTextColor: hoverTextColor,
                targetBorderColor: borderHoverColor,
                targetFillColor: fillHoverColor,
                targetArrowAlpha: 1f,
                targetArrowPos: _arrowTargetPos,
                targetRightArrowPos: _rightArrowTargetPos,
                duration: transitionDuration
            );
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            StartTransition(
                targetScale: 1f,
                targetTextColor: idleTextColor,
                targetBorderColor: borderIdleColor,
                targetFillColor: fillIdleColor,
                targetArrowAlpha: 0f,
                targetArrowPos: _arrowRestPos,
                targetRightArrowPos: _rightArrowRestPos,
                duration: transitionDuration
            );
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _isPressed = true;
            StartTransition(
                targetScale: clickScale,
                targetTextColor: clickTextColor,
                targetBorderColor: borderHoverColor,
                targetFillColor: fillHoverColor,
                targetArrowAlpha: 1f,
                targetArrowPos: _arrowTargetPos,
                targetRightArrowPos: _rightArrowTargetPos,
                duration: clickDuration
            );
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _isPressed = false;
            if (_isHovered)
            {
                StartTransition(
                    targetScale: hoverScale,
                    targetTextColor: hoverTextColor,
                    targetBorderColor: borderHoverColor,
                    targetFillColor: fillHoverColor,
                    targetArrowAlpha: 1f,
                    targetArrowPos: _arrowTargetPos,
                    targetRightArrowPos: _rightArrowTargetPos,
                    duration: transitionDuration
                );
            }
            else
            {
                StartTransition(
                    targetScale: 1f,
                    targetTextColor: idleTextColor,
                    targetBorderColor: borderIdleColor,
                    targetFillColor: fillIdleColor,
                    targetArrowAlpha: 0f,
                    targetArrowPos: _arrowRestPos,
                    targetRightArrowPos: _rightArrowRestPos,
                    duration: transitionDuration
                );
            }
        }

        private void StartTransition(float targetScale, Color targetTextColor, Color targetBorderColor, Color targetFillColor, float targetArrowAlpha, Vector2 targetArrowPos, Vector2 targetRightArrowPos, float duration)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }
            _activeTransition = StartCoroutine(AnimateTransition(targetScale, targetTextColor, targetBorderColor, targetFillColor, targetArrowAlpha, targetArrowPos, targetRightArrowPos, duration));
        }

        private IEnumerator AnimateTransition(float targetScale, Color targetTextColor, Color targetBorderColor, Color targetFillColor, float targetArrowAlpha, Vector2 targetArrowPos, Vector2 targetRightArrowPos, float duration)
        {
            float elapsed = 0f;

            Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
            Vector3 endScale = _baseScale * targetScale;

            Color startTextColor = buttonText != null ? buttonText.color : idleTextColor;
            Color startBorderColor = buttonBorderImage != null ? buttonBorderImage.color : borderIdleColor;
            Color startFillColor = buttonBgImage != null ? buttonBgImage.color : fillIdleColor;

            float startArrowAlpha = arrowCanvasGroup != null ? arrowCanvasGroup.alpha : 0f;
            Vector2 startArrowPos = arrowTransform != null ? arrowTransform.anchoredPosition : _arrowRestPos;

            float startRightArrowAlpha = rightArrowCanvasGroup != null ? rightArrowCanvasGroup.alpha : 0f;
            Vector2 startRightArrowPos = rightArrowTransform != null ? rightArrowTransform.anchoredPosition : _rightArrowRestPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);

                if (_rectTransform != null)
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);

                if (buttonText != null)
                    buttonText.color = Color.LerpUnclamped(startTextColor, targetTextColor, ease);

                if (buttonBorderImage != null && showBorder)
                    buttonBorderImage.color = Color.LerpUnclamped(startBorderColor, targetBorderColor, ease);

                if (buttonBgImage != null && showBorder)
                    buttonBgImage.color = Color.LerpUnclamped(startFillColor, targetFillColor, ease);

                if (arrowCanvasGroup != null)
                    arrowCanvasGroup.alpha = Mathf.LerpUnclamped(startArrowAlpha, targetArrowAlpha, ease);

                if (arrowTransform != null)
                    arrowTransform.anchoredPosition = Vector2.LerpUnclamped(startArrowPos, targetArrowPos, ease);

                if (rightArrowCanvasGroup != null)
                    rightArrowCanvasGroup.alpha = Mathf.LerpUnclamped(startRightArrowAlpha, targetArrowAlpha, ease);

                if (rightArrowTransform != null)
                    rightArrowTransform.anchoredPosition = Vector2.LerpUnclamped(startRightArrowPos, targetRightArrowPos, ease);

                yield return null;
            }

            if (_rectTransform != null)
                _rectTransform.localScale = endScale;

            if (buttonText != null)
                buttonText.color = targetTextColor;

            if (buttonBorderImage != null && showBorder)
                buttonBorderImage.color = targetBorderColor;

            if (buttonBgImage != null && showBorder)
                buttonBgImage.color = targetFillColor;

            if (arrowCanvasGroup != null)
                arrowCanvasGroup.alpha = targetArrowAlpha;

            if (arrowTransform != null)
                arrowTransform.anchoredPosition = targetArrowPos;

            if (rightArrowCanvasGroup != null)
                rightArrowCanvasGroup.alpha = targetArrowAlpha;

            if (rightArrowTransform != null)
                rightArrowTransform.anchoredPosition = targetRightArrowPos;

            _activeTransition = null;
        }
    }
}
