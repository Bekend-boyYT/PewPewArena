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
        [SerializeField] private Sprite arrowSprite;

        [Header("Right Arrow Settings (Optional)")]
        [SerializeField] private RectTransform rightArrowTransform;
        [SerializeField] private CanvasGroup rightArrowCanvasGroup;
        [SerializeField] private Image rightArrowImage;

        [Header("Text Styling")]
        [SerializeField] private Color idleTextColor = new Color(0.85f, 0.90f, 0.93f, 1f);
        [SerializeField] private Color hoverTextColor = new Color(1.0f, 1.0f, 1.0f, 1f);
        [SerializeField] private Color clickTextColor = new Color(0.70f, 1.0f, 0.90f, 1f);

        [Header("Arrow Styling")]
        [SerializeField] private Color arrowColor = new Color(0.92f, 0.98f, 1.0f, 1f);
        [SerializeField] private Vector2 arrowSize = new Vector2(40f, 34f);
        [SerializeField] private float arrowMargin = 16f;
        [SerializeField] private float arrowSlideDistance = 8f;

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

        public CanvasGroup ArrowCanvasGroup => arrowCanvasGroup;
        public CanvasGroup RightArrowCanvasGroup => rightArrowCanvasGroup;
        public RectTransform RightArrowTransform => rightArrowTransform;
        public bool IsHovered => _isHovered;
        public bool IsPressed => _isPressed;

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
        /// Automatically discovers or configures required UI components.
        /// </summary>
        public void InitializeComponents()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                // Disable Unity's default color tint transition so it doesn't fight our custom animation
                button.transition = Selectable.Transition.None;

                // Ensure the button's background image is transparent
                Image bgImage = button.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = new Color(0f, 0f, 0f, 0f);
                    bgImage.raycastTarget = true;
                }
            }

            if (buttonText == null)
            {
                buttonText = GetComponentInChildren<TextMeshProUGUI>();
            }

            if (buttonText != null)
            {
                buttonText.raycastTarget = false;
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
                if (arrowImage.sprite == null && arrowSprite != null)
                {
                    arrowImage.sprite = arrowSprite;
                }
                arrowImage.color = arrowColor;
                arrowImage.raycastTarget = false;
            }

            FindRightArrow();
        }

        /// <summary>
        /// Detects any right-side arrow child added to the button (e.g. HoverArrowRight, HoverArrow (1), RightArrow).
        /// </summary>
        public void FindRightArrow()
        {
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

                        if (cName.IndexOf("Arrow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                            cName.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            rightArrowTransform = child.GetComponent<RectTransform>();
                            break;
                        }
                    }
                }
            }

            if (rightArrowTransform != null)
            {
                if (!rightArrowTransform.gameObject.activeSelf)
                    rightArrowTransform.gameObject.SetActive(true);

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
                    if (rightArrowImage.sprite == null && arrowSprite != null)
                    {
                        rightArrowImage.sprite = arrowSprite;
                    }
                    if (rightArrowImage.sprite == null && arrowImage != null && arrowImage.sprite != null)
                    {
                        rightArrowImage.sprite = arrowImage.sprite;
                    }
                    rightArrowImage.color = arrowColor;
                    rightArrowImage.raycastTarget = false;
                }
            }
        }

        private void CreateArrowChild()
        {
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            arrowObj.transform.SetParent(transform, false);

            arrowTransform = arrowObj.GetComponent<RectTransform>();
            arrowTransform.sizeDelta = arrowSize;
            arrowTransform.anchorMin = new Vector2(0.5f, 0.5f);
            arrowTransform.anchorMax = new Vector2(0.5f, 0.5f);
            arrowTransform.pivot = new Vector2(0.5f, 0.5f);

            arrowCanvasGroup = arrowObj.GetComponent<CanvasGroup>();
            arrowCanvasGroup.alpha = 0f;
            arrowCanvasGroup.interactable = false;
            arrowCanvasGroup.blocksRaycasts = false;

            arrowImage = arrowObj.GetComponent<Image>();
            arrowImage.raycastTarget = false;
            arrowImage.color = arrowColor;

            if (arrowSprite != null)
            {
                arrowImage.sprite = arrowSprite;
            }
        }

        /// <summary>
        /// Computes the exact idle and hovered positions of arrows so they sit cleanly
        /// flanking the button text without any overlap.
        /// </summary>
        public void CalculateArrowPositions()
        {
            if (buttonText == null) return;

            buttonText.ForceMeshUpdate();
            float textWidth = buttonText.preferredWidth;
            float textCenterX = buttonText.rectTransform.anchoredPosition.x;
            float textCenterY = buttonText.rectTransform.anchoredPosition.y;

            if (arrowTransform != null)
            {
                // Compute the left boundary of the text
                float textLeft = textCenterX - (textWidth * 0.5f);
                float arrowWidth = arrowTransform.rect.width > 0 ? arrowTransform.rect.width : arrowSize.x;

                // Target position: arrow sits arrowMargin to the left of the text
                float targetX = textLeft - (arrowWidth * 0.5f) - arrowMargin;
                // Rest position: slightly further back to the left so it slides forward on hover
                float restX = targetX - arrowSlideDistance;

                _arrowTargetPos = new Vector2(targetX, textCenterY);
                _arrowRestPos = new Vector2(restX, textCenterY);
            }

            if (rightArrowTransform != null)
            {
                // Compute the right boundary of the text
                float textRight = textCenterX + (textWidth * 0.5f);
                float rightArrowWidth = rightArrowTransform.rect.width > 0 ? rightArrowTransform.rect.width : arrowSize.x;

                // Target position: right arrow sits arrowMargin to the right of the text
                float rightTargetX = textRight + (rightArrowWidth * 0.5f) + arrowMargin;
                // Rest position: slightly further back to the right so it slides forward (inward) on hover
                float rightRestX = rightTargetX + arrowSlideDistance;

                _rightArrowTargetPos = new Vector2(rightTargetX, textCenterY);
                _rightArrowRestPos = new Vector2(rightRestX, textCenterY);
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
                    targetArrowAlpha: 0f,
                    targetArrowPos: _arrowRestPos,
                    targetRightArrowPos: _rightArrowRestPos,
                    duration: transitionDuration
                );
            }
        }

        private void StartTransition(float targetScale, Color targetTextColor, float targetArrowAlpha, Vector2 targetArrowPos, Vector2 targetRightArrowPos, float duration)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }
            _activeTransition = StartCoroutine(AnimateTransition(targetScale, targetTextColor, targetArrowAlpha, targetArrowPos, targetRightArrowPos, duration));
        }

        private IEnumerator AnimateTransition(float targetScale, Color targetTextColor, float targetArrowAlpha, Vector2 targetArrowPos, Vector2 targetRightArrowPos, float duration)
        {
            float elapsed = 0f;

            Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
            Vector3 endScale = _baseScale * targetScale;

            Color startTextColor = buttonText != null ? buttonText.color : idleTextColor;
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
