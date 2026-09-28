using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SniperGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HoldKeyPrompt : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The background image of the rectangle button")]
        [SerializeField] private Image buttonBackground;
        [Tooltip("The 'SPACE' text component (TextMeshPro)")]
        [SerializeField] private TMP_Text keyTextTMP;
        [Tooltip("Fallback standard UI Text (if not using TextMeshPro)")]
        [SerializeField] private Text keyTextStandard;
        [Tooltip("Optional secondary label (e.g. 'WALL RUN')")]
        [SerializeField] private TMP_Text actionLabelTMP;

        [Header("Color Transition")]
        [Tooltip("Default button background color (light keycap)")]
        [SerializeField] private Color normalBgColor = new Color(0.9f, 0.9f, 0.92f, 0.95f);
        [Tooltip("Darker button background color when fully held down")]
        [SerializeField] private Color heldBgColor = new Color(0.12f, 0.12f, 0.14f, 1.0f);

        [Header("Text Color Transition")]
        [SerializeField] private Color normalTextColor = new Color(0.12f, 0.12f, 0.14f, 1.0f);
        [SerializeField] private Color heldTextColor = new Color(0.95f, 0.95f, 0.95f, 1.0f);

        [Header("Press Effect (Optional)")]
        [SerializeField] private bool punchScaleOnHold = true;
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 pressedScale = new Vector3(0.92f, 0.92f, 1.0f);

        [Header("Prompt Visibility Fade")]
        [SerializeField] private float fadeSpeed = 10f;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private bool _isVisible;
        private float _currentProgress;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();

            if (buttonBackground == null)
            {
                buttonBackground = GetComponent<Image>();
            }

            _canvasGroup.alpha = 0f;
            ApplyVisualProgress(0f);
        }

        private void Update()
        {
            // Smoothly fade the prompt container in and out
            float targetAlpha = _isVisible ? 1f : 0f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        /// <summary>
        /// Updates visibility and normalized progress (0.0 to 1.0)
        /// </summary>
        public void SetPromptState(bool visible, float holdProgress)
        {
            _isVisible = visible;
            _currentProgress = Mathf.Clamp01(holdProgress);
            ApplyVisualProgress(_currentProgress);
        }

        private void ApplyVisualProgress(float t)
        {
            // Background color interpolation
            if (buttonBackground != null)
            {
                buttonBackground.color = Color.Lerp(normalBgColor, heldBgColor, t);
            }

            // Text color interpolation (inverts so text stays legible against the dark background)
            Color currentTextColor = Color.Lerp(normalTextColor, heldTextColor, t);
            if (keyTextTMP != null) keyTextTMP.color = currentTextColor;
            if (keyTextStandard != null) keyTextStandard.color = currentTextColor;

            // Slight physical keypress indentation
            if (punchScaleOnHold && _rectTransform != null)
            {
                _rectTransform.localScale = Vector3.Lerp(normalScale, pressedScale, t);
            }
        }
    }
}