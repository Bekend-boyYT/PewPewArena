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
        private Vector3 _baseScale = Vector3.one;

        public CanvasGroup ArrowCanvasGroup => arrowCanvasGroup;
        public bool IsHovered => _isHovered;
        public bool IsPressed => _isPressed;

        private void Awake()
        {
            InitializeComponents();
            CalculateArrowPositions();
            ApplyInstantIdleState();
        }

        private void OnEnable()
        {
            _isHovered = false;
            _isPressed = false;
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

            // Look for existing arrow child or create one
            if (arrowTransform == null)
            {
                Transform existingArrow = transform.Find("Arrow") ?? transform.Find("HoverArrow");
                if (existingArrow == null)
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (child.name.IndexOf("Arrow", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
        /// Computes the exact idle and hovered positions of the arrow so it sits cleanly
        /// to the left of the button text without any overlap.
        /// </summary>
        public void CalculateArrowPositions()
        {
            if (buttonText == null || arrowTransform == null) return;

            buttonText.ForceMeshUpdate();
            float textWidth = buttonText.preferredWidth;
            float textCenterX = buttonText.rectTransform.anchoredPosition.x;
            float textCenterY = buttonText.rectTransform.anchoredPosition.y;

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
                    duration: transitionDuration
                );
            }
        }

        private void StartTransition(float targetScale, Color targetTextColor, float targetArrowAlpha, Vector2 targetArrowPos, float duration)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }
            _activeTransition = StartCoroutine(AnimateTransition(targetScale, targetTextColor, targetArrowAlpha, targetArrowPos, duration));
        }

        private IEnumerator AnimateTransition(float targetScale, Color targetTextColor, float targetArrowAlpha, Vector2 targetArrowPos, float duration)
        {
            float elapsed = 0f;

            Vector3 startScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;
            Vector3 endScale = _baseScale * targetScale;

            Color startTextColor = buttonText != null ? buttonText.color : idleTextColor;
            float startArrowAlpha = arrowCanvasGroup != null ? arrowCanvasGroup.alpha : 0f;
            Vector2 startArrowPos = arrowTransform != null ? arrowTransform.anchoredPosition : _arrowRestPos;

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

            _activeTransition = null;
        }
    }
}
