using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Delivers an ultra-cinematic, staggered entrance for the Loadout Arsenal screen:
    /// - Smooth panel canvas fade-in (0.45s).
    /// - Title and subtitle drop with smooth deceleration.
    /// - Laser divider beam horizontal expansion from center.
    /// - Staggered wing-glide for Sniper 1 (left) and Sniper 2 (right) hero weapon bays.
    /// - Dynamically triggers stat comparison bar sweeps upon arrival.
    /// - Resilient 100% unscaled time execution with instant rapid-switch recovery.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadoutPanelEntrance : MonoBehaviour
    {
        [Header("Animation Timings")]
        [SerializeField] private float duration = 0.46f;
        [SerializeField] private float cardStaggerDelay = 0.08f;
        [SerializeField] private float cardSlideDistance = 45f;

        [Header("Header Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private float titleSlideDistance = 14f;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private float subtitleSlideDistance = 10f;
        [SerializeField] private RectTransform dividerTransform;

        [Header("Weapon Cards")]
        [SerializeField] private RectTransform sniper1Card;
        [SerializeField] private RectTransform sniper2Card;

        private CanvasGroup _canvasGroup;
        private Coroutine _entranceCoroutine;

        private Vector2 _originalTitlePos;
        private bool _hasTitlePos = false;
        private Vector2 _originalSubtitlePos;
        private bool _hasSubtitlePos = false;
        private Vector2 _originalCard1Pos;
        private bool _hasCard1Pos = false;
        private Vector2 _originalCard2Pos;
        private bool _hasCard2Pos = false;
        private Vector3 _originalDividerScale = Vector3.one;

        private void Awake()
        {
            // Apply the redesigned layout first so the cached "original" positions match the new layout
            var theme = GetComponent<LoadoutPanelTheme>();
            if (theme == null) theme = gameObject.AddComponent<LoadoutPanelTheme>();
            theme.Apply();

            EnsureCanvasGroup();
            CacheElements();
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

        private void CacheElements()
        {
            if (titleText == null)
            {
                Transform t = transform.Find("LoadoutTitle");
                if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
            }
            if (titleText != null && !_hasTitlePos)
            {
                _originalTitlePos = titleText.rectTransform.anchoredPosition;
                _hasTitlePos = true;
            }

            if (subtitleText == null)
            {
                Transform s = transform.Find("LoadoutSubtitle");
                if (s != null) subtitleText = s.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleText != null && !_hasSubtitlePos)
            {
                _originalSubtitlePos = subtitleText.rectTransform.anchoredPosition;
                _hasSubtitlePos = true;
            }

            if (dividerTransform == null)
            {
                Transform d = transform.Find("LaserDivider") ?? transform.Find("Divider");
                if (d != null) dividerTransform = d.GetComponent<RectTransform>();
            }
            if (dividerTransform != null)
            {
                _originalDividerScale = dividerTransform.localScale;
            }

            if (sniper1Card == null)
            {
                Transform c1 = transform.Find("WeaponCardsContainer/Sniper1Card");
                if (c1 != null) sniper1Card = c1.GetComponent<RectTransform>();
            }
            if (sniper1Card != null && !_hasCard1Pos)
            {
                _originalCard1Pos = sniper1Card.anchoredPosition;
                _hasCard1Pos = true;
            }

            if (sniper2Card == null)
            {
                Transform c2 = transform.Find("WeaponCardsContainer/Sniper2Card");
                if (c2 != null) sniper2Card = c2.GetComponent<RectTransform>();
            }
            if (sniper2Card != null && !_hasCard2Pos)
            {
                _originalCard2Pos = sniper2Card.anchoredPosition;
                _hasCard2Pos = true;
            }
        }

        private void OnEnable()
        {
            EnsureCanvasGroup();
            CacheElements();

            // Force all back button arrows inside LoadoutPanel to hide instantly
            MainMenuButtonEffect[] btnEffects = GetComponentsInChildren<MainMenuButtonEffect>(true);
            foreach (var btn in btnEffects)
            {
                btn.ApplyInstantIdleState();
            }

            if (_entranceCoroutine != null)
            {
                StopCoroutine(_entranceCoroutine);
            }
            _entranceCoroutine = StartCoroutine(PlayEntranceSequence());
        }

        private void OnDisable()
        {
            if (_entranceCoroutine != null)
            {
                StopCoroutine(_entranceCoroutine);
                _entranceCoroutine = null;
            }
            ResetToFinalPositions();
        }

        private void ResetToFinalPositions()
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;

            if (titleText != null && _hasTitlePos)
                titleText.rectTransform.anchoredPosition = _originalTitlePos;

            if (subtitleText != null && _hasSubtitlePos)
                subtitleText.rectTransform.anchoredPosition = _originalSubtitlePos;

            if (dividerTransform != null)
                dividerTransform.localScale = _originalDividerScale;

            if (sniper1Card != null && _hasCard1Pos)
                sniper1Card.anchoredPosition = _originalCard1Pos;

            if (sniper2Card != null && _hasCard2Pos)
                sniper2Card.anchoredPosition = _originalCard2Pos;
        }

        private IEnumerator PlayEntranceSequence()
        {
            _canvasGroup.alpha = 0f;

            // Offset elements initially
            if (titleText != null && _hasTitlePos)
                titleText.rectTransform.anchoredPosition = _originalTitlePos + new Vector2(0f, titleSlideDistance);

            if (subtitleText != null && _hasSubtitlePos)
                subtitleText.rectTransform.anchoredPosition = _originalSubtitlePos + new Vector2(0f, subtitleSlideDistance);

            if (dividerTransform != null)
                dividerTransform.localScale = new Vector3(0f, _originalDividerScale.y, _originalDividerScale.z);

            if (sniper1Card != null && _hasCard1Pos)
                sniper1Card.anchoredPosition = _originalCard1Pos - new Vector2(cardSlideDistance, 0f);

            if (sniper2Card != null && _hasCard2Pos)
                sniper2Card.anchoredPosition = _originalCard2Pos + new Vector2(cardSlideDistance, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Smooth panel fade
                _canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress * 1.3f);

                // Title and subtitle drop
                if (titleText != null && _hasTitlePos)
                {
                    float t = Mathf.Clamp01(elapsed / (duration * 0.75f));
                    float ease = Mathf.SmoothStep(0f, 1f, t);
                    titleText.rectTransform.anchoredPosition = Vector2.LerpUnclamped(
                        _originalTitlePos + new Vector2(0f, titleSlideDistance),
                        _originalTitlePos,
                        ease
                    );
                }

                if (subtitleText != null && _hasSubtitlePos)
                {
                    float t = Mathf.Clamp01(elapsed / (duration * 0.8f));
                    float ease = Mathf.SmoothStep(0f, 1f, t);
                    subtitleText.rectTransform.anchoredPosition = Vector2.LerpUnclamped(
                        _originalSubtitlePos + new Vector2(0f, subtitleSlideDistance),
                        _originalSubtitlePos,
                        ease
                    );
                }

                // Divider horizontal expansion
                if (dividerTransform != null)
                {
                    float t = Mathf.Clamp01(elapsed / (duration * 0.85f));
                    float ease = Mathf.SmoothStep(0f, 1f, t);
                    dividerTransform.localScale = new Vector3(
                        Mathf.LerpUnclamped(0f, _originalDividerScale.x, ease),
                        _originalDividerScale.y,
                        _originalDividerScale.z
                    );
                }

                // Card 1 slide from left
                if (sniper1Card != null && _hasCard1Pos)
                {
                    float t1 = Mathf.Clamp01(elapsed / (duration * 0.85f));
                    float ease1 = Mathf.SmoothStep(0f, 1f, t1);
                    sniper1Card.anchoredPosition = Vector2.LerpUnclamped(
                        _originalCard1Pos - new Vector2(cardSlideDistance, 0f),
                        _originalCard1Pos,
                        ease1
                    );
                }

                // Card 2 slide from right with stagger
                if (sniper2Card != null && _hasCard2Pos)
                {
                    float t2 = Mathf.Clamp01((elapsed - cardStaggerDelay) / (duration * 0.85f));
                    float ease2 = Mathf.SmoothStep(0f, 1f, t2);
                    sniper2Card.anchoredPosition = Vector2.LerpUnclamped(
                        _originalCard2Pos + new Vector2(cardSlideDistance, 0f),
                        _originalCard2Pos,
                        ease2
                    );
                }

                yield return null;
            }

            ResetToFinalPositions();

            // Trigger dynamic stat bar sweeps on both cards!
            if (sniper1Card != null)
            {
                var card1Effect = sniper1Card.GetComponent<LoadoutCardEffect>();
                if (card1Effect != null) card1Effect.PlayStatFillSweep(0.38f);
            }

            if (sniper2Card != null)
            {
                var card2Effect = sniper2Card.GetComponent<LoadoutCardEffect>();
                if (card2Effect != null) card2Effect.PlayStatFillSweep(0.38f);
            }

            _entranceCoroutine = null;
        }
    }
}
