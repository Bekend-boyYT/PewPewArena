using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Delivers an elegant, cinematic entrance animation when MainMenuPanel opens:
    /// - Smooth panel canvas fade-in (0.45s).
    /// - Subtle staggered upward glide for the menu buttons.
    /// - Ensures all hover arrows remain hidden until actively hovered.
    /// - Cleans up legacy dark background boxes automatically.
    /// - Fully resilient to panel re-opening and rapid switching.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuPanelEntrance : MonoBehaviour
    {
        [Header("Animation Timings")]
        [SerializeField] private float duration = 0.32f;
        [SerializeField] private float buttonStaggerDelay = 0.04f;
        [SerializeField] private float buttonSlideDistance = 14f;

        [Header("Title & Divider Polish")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private float titleSlideDistance = 8f;
        [SerializeField] private RectTransform dividerTransform;
        [SerializeField] private float dividerSlideDistance = 6f;

        private CanvasGroup _canvasGroup;
        private Coroutine _entranceCoroutine;

        private readonly List<RectTransform> _buttons = new List<RectTransform>();
        private readonly List<Vector2> _originalPositions = new List<Vector2>();
        private Vector2 _originalTitlePos;
        private bool _hasOriginalTitlePos = false;
        private Vector2 _originalDividerPos;
        private bool _hasOriginalDividerPos = false;

        private void Awake()
        {
            if (duration > 0.38f) duration = 0.32f;
            if (buttonStaggerDelay > 0.05f) buttonStaggerDelay = 0.04f;

            EnsureCanvasGroup();
            CacheElements();
            HideObsoleteElements();
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void HideObsoleteElements()
        {
            // Ensure legacy black boxes behind buttons remain disabled
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("BackImagesButtons", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void CacheElements()
        {
            _buttons.Clear();
            _originalPositions.Clear();

            // Find buttons with MainMenuButtonEffect or standard buttons
            MainMenuButtonEffect[] effects = GetComponentsInChildren<MainMenuButtonEffect>(true);
            foreach (var effect in effects)
            {
                RectTransform rt = effect.GetComponent<RectTransform>();
                if (rt != null && !_buttons.Contains(rt))
                {
                    _buttons.Add(rt);
                    _originalPositions.Add(rt.anchoredPosition);
                }
            }

            if (titleText == null)
            {
                Transform titleTransform = transform.Find("TitleText") ?? transform.Find("PlayTitle") ?? transform.Find("JoinTitle") ?? transform.Find("CreateTitle");
                if (titleTransform != null)
                {
                    titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (titleText != null && !_hasOriginalTitlePos)
            {
                _originalTitlePos = titleText.rectTransform.anchoredPosition;
                _hasOriginalTitlePos = true;
            }

            if (dividerTransform == null)
            {
                Transform divTransform = transform.Find("PlayDivider") ?? transform.Find("Divider");
                if (divTransform != null)
                {
                    dividerTransform = divTransform.GetComponent<RectTransform>();
                }
            }

            if (dividerTransform != null && !_hasOriginalDividerPos)
            {
                _originalDividerPos = dividerTransform.anchoredPosition;
                _hasOriginalDividerPos = true;
            }
        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
            HideObsoleteElements();

            if (_buttons.Count == 0)
            {
                CacheElements();
            }

            // Immediately force all arrows to be hidden
            MainMenuButtonEffect[] effects = GetComponentsInChildren<MainMenuButtonEffect>(true);
            foreach (var effect in effects)
            {
                effect.ApplyInstantIdleState();
            }

            if (_entranceCoroutine != null)
            {
                StopCoroutine(_entranceCoroutine);
            }
            _entranceCoroutine = StartCoroutine(PlayEntrance());
        }

        private void OnDisable()
        {
            if (_entranceCoroutine != null)
            {
                StopCoroutine(_entranceCoroutine);
                _entranceCoroutine = null;
            }
            ResetToFinalState();
        }

        private void ResetToFinalState()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null && i < _originalPositions.Count)
                {
                    _buttons[i].anchoredPosition = _originalPositions[i];
                }
            }

            if (titleText != null && _hasOriginalTitlePos)
            {
                titleText.rectTransform.anchoredPosition = _originalTitlePos;
            }

            if (dividerTransform != null && _hasOriginalDividerPos)
            {
                dividerTransform.anchoredPosition = _originalDividerPos;
            }
        }

        private IEnumerator PlayEntrance()
        {
            _canvasGroup.alpha = 0f;

            // Offset buttons down slightly for upward glide
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null && i < _originalPositions.Count)
                {
                    _buttons[i].anchoredPosition = _originalPositions[i] - new Vector2(0f, buttonSlideDistance);
                }
            }

            // Offset title down slightly
            if (titleText != null && _hasOriginalTitlePos)
            {
                titleText.rectTransform.anchoredPosition = _originalTitlePos - new Vector2(0f, titleSlideDistance);
            }

            // Offset divider down slightly
            if (dividerTransform != null && _hasOriginalDividerPos)
            {
                dividerTransform.anchoredPosition = _originalDividerPos - new Vector2(0f, dividerSlideDistance);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Smooth panel fade using responsive ease-out so elements appear immediately without hesitation
                float easeAlpha = 1f - Mathf.Pow(1f - progress, 2.2f);
                _canvasGroup.alpha = easeAlpha;

                // Staggered buttons upward drift with silky physical deceleration
                for (int i = 0; i < _buttons.Count; i++)
                {
                    if (_buttons[i] != null && i < _originalPositions.Count)
                    {
                        float buttonDelay = i * buttonStaggerDelay;
                        float buttonT = Mathf.Clamp01((elapsed - buttonDelay) / (duration * 0.82f));
                        float ease = 1f - Mathf.Pow(1f - buttonT, 2.5f);
                        Vector2 startPos = _originalPositions[i] - new Vector2(0f, buttonSlideDistance);
                        _buttons[i].anchoredPosition = Vector2.LerpUnclamped(startPos, _originalPositions[i], ease);
                    }
                }

                // Title glide
                if (titleText != null && _hasOriginalTitlePos)
                {
                    float titleT = Mathf.Clamp01(elapsed / (duration * 0.85f));
                    float titleEase = 1f - Mathf.Pow(1f - titleT, 2.5f);
                    Vector2 titleStart = _originalTitlePos - new Vector2(0f, titleSlideDistance);
                    titleText.rectTransform.anchoredPosition = Vector2.LerpUnclamped(titleStart, _originalTitlePos, titleEase);
                }

                // Divider glide
                if (dividerTransform != null && _hasOriginalDividerPos)
                {
                    float divT = Mathf.Clamp01(elapsed / (duration * 0.85f));
                    float divEase = 1f - Mathf.Pow(1f - divT, 2.5f);
                    Vector2 divStart = _originalDividerPos - new Vector2(0f, dividerSlideDistance);
                    dividerTransform.anchoredPosition = Vector2.LerpUnclamped(divStart, _originalDividerPos, divEase);
                }

                yield return null;
            }

            ResetToFinalState();
            _entranceCoroutine = null;
        }
    }
}
