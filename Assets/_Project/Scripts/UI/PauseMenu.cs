using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SniperGame.Gameplay;

namespace SniperGame.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        public static bool IsPaused { get; private set; } = false;

        [Header("Panels")]
        [SerializeField] private GameObject mainPausePanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main Pause Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings UI Elements")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumeValueText;
        [SerializeField] private Button settingsBackButton;

        private const string SensitivityPrefKey = "MouseSensitivity";
        private const string VolumePrefKey = "MasterVolume";
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            IsPaused = false;

            // Zorg dat de sub-panelen standaard dicht staan
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (mainPausePanel != null) mainPausePanel.SetActive(false);
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                if (settingsPanel.GetComponent<Settings.SettingsUIController>() == null)
                {
                    settingsPanel.AddComponent<Settings.SettingsUIController>();
                }
            }

            SetupButtonListeners();
            LoadSavedSettings();
            ApplyPauseMenuTheme();
        }

        private void Start()
        {
            ApplyVolume(volumeSlider != null ? volumeSlider.value : 0.8f);
            ApplyPauseMenuTheme();
        }

        private void Update()
        {
            // Niet pauzeren als de match voorbij is
            if (RoundManager.Instance != null && RoundManager.Instance.CurrentState.Value == MatchState.MatchEnded)
            {
                if (IsPaused) CloseAllMenus();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeInput();
            }
        }

        private void SetupButtonListeners()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitToMainMenu);
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(BackToMainPause);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        private void HandleEscapeInput()
        {
            if (!IsPaused)
            {
                OpenMainPause();
            }
            else
            {
                // Als we in Settings zitten: ESC gaat terug naar Hoofdpauze
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    BackToMainPause();
                }
                else
                {
                    ResumeGame();
                }
            }
        }

        public void OpenMainPause()
        {
            IsPaused = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            if (mainPausePanel != null)
            {
                mainPausePanel.SetActive(true);
                ApplyPauseMenuTheme();

                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                if (gameObject.activeInHierarchy)
                {
                    _fadeRoutine = StartCoroutine(FadePauseMenuRoutine(true));
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenSettings()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (mainPausePanel != null) mainPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);

            var controller = settingsPanel != null ? settingsPanel.GetComponent<Settings.SettingsUIController>() : null;
            if (controller == null && settingsPanel != null)
            {
                controller = settingsPanel.AddComponent<Settings.SettingsUIController>();
            }

            if (controller != null)
            {
                controller.SwitchTab(0);
                controller.RefreshAllUI();
            }
        }

        public void BackToMainPause()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainPausePanel != null)
            {
                LoadSavedSettings();
                mainPausePanel.SetActive(true);
                ApplyPauseMenuTheme();

                if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
                if (gameObject.activeInHierarchy)
                {
                    _fadeRoutine = StartCoroutine(FadePauseMenuRoutine(true));
                }
            }
        }

        public void ResumeGame()
        {
            CloseAllMenus();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CloseAllMenus()
        {
            IsPaused = false;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (mainPausePanel != null)
            {
                var cg = mainPausePanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0f;
                mainPausePanel.SetActive(false);
            }

            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void QuitToMainMenu()
        {
            IsPaused = false;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene("01_MainMenu");
        }

        private float _lastAudioPreviewTime = 0f;

        private void LoadSavedSettings()
        {
            float savedSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, 2.0f);
            if (savedSensitivity < 0.2f) savedSensitivity = 2.0f;

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.2f;
                sensitivitySlider.maxValue = 5.0f;
                sensitivitySlider.value = savedSensitivity;
            }
            OnSensitivityChanged(savedSensitivity);

            float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
            if (savedVolume <= 0.001f && !PlayerPrefs.HasKey(VolumePrefKey)) savedVolume = 0.8f;

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = savedVolume;
            }
            OnVolumeChanged(savedVolume);
        }

        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat(SensitivityPrefKey, value);
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = $"{value:F2}";
            }
        }

        private void OnVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat(VolumePrefKey, value);
            ApplyVolume(value);

            if (volumeValueText != null)
            {
                volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }

            // Real-time audio preview while adjusting volume in settings menu
            if (Time.unscaledTime - _lastAudioPreviewTime > 0.12f && SniperGame.Audio.AudioManager.Instance != null && IsPaused)
            {
                _lastAudioPreviewTime = Time.unscaledTime;
                SniperGame.Audio.AudioManager.Instance.PlayHitmarker(false);
            }
        }

        private void ApplyVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public static float GetSensitivity()
        {
            return Settings.SettingsManager.MouseSensitivity;
        }

        public static float GetVolume()
        {
            return Settings.SettingsManager.MasterVolume;
        }

        #region Visual Theme & Animations
        private IEnumerator FadePauseMenuRoutine(bool show)
        {
            if (mainPausePanel == null) yield break;

            var cg = mainPausePanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = mainPausePanel.AddComponent<CanvasGroup>();

            var rt = mainPausePanel.GetComponent<RectTransform>();

            float duration = 0.12f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            float targetAlpha = show ? 1f : 0f;
            Vector3 startScale = show ? (Vector3.one * 0.96f) : Vector3.one;
            Vector3 targetScale = show ? Vector3.one : (Vector3.one * 0.96f);

            if (show)
            {
                cg.alpha = 0f;
                if (rt != null) rt.localScale = startScale;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                if (rt != null) rt.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            cg.alpha = targetAlpha;
            if (rt != null) rt.localScale = targetScale;
            cg.interactable = show;
            cg.blocksRaycasts = show;

            if (!show)
            {
                mainPausePanel.SetActive(false);
            }

            _fadeRoutine = null;
        }

        [ContextMenu("Apply Pause Menu Theme")]
        public void ApplyPauseMenuTheme()
        {
            if (mainPausePanel == null) return;

            // Palette colors matching the reference images
            Color darkCharcoalBg = new Color(0.067f, 0.075f, 0.106f, 0.96f); // #11131B (Deep Obsidian)
            Color goldBorderColor = new Color(0.77f, 0.63f, 0.35f, 0.50f);    // #C5A059 (Weathered Gold)
            Color crimsonRed = new Color(0.96f, 0.20f, 0.26f, 1f);            // #F53342 (Hades Crimson Title)
            Color goldAccent = new Color(0.77f, 0.65f, 0.40f, 0.90f);          // #C5A059
            Color buttonBg = new Color(0.09f, 0.10f, 0.15f, 1f);              // #171A26 (Dark Slate)
            Color buttonHover = new Color(0.16f, 0.18f, 0.26f, 1f);           // #292E42
            Color buttonBorder = new Color(0.77f, 0.63f, 0.35f, 0.30f);
            Color textSilver = new Color(0.85f, 0.88f, 0.94f, 1f);            // #D9DFEC
            Color textGlow = new Color(1f, 0.96f, 0.82f, 1f);                 // #FFF5D1

            // 1. Main Pause Card
            var cardRt = mainPausePanel.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.sizeDelta = new Vector2(360f, 480f);
            }

            var cardImg = mainPausePanel.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.color = darkCharcoalBg;
            }

            var cardOutline = mainPausePanel.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = mainPausePanel.AddComponent<Outline>();
            cardOutline.effectColor = goldBorderColor;
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Hide old BackImageImprovement2 so it doesn't clutter the card
            var oldBack = mainPausePanel.transform.Find("BackImageImprovement2");
            if (oldBack != null)
            {
                oldBack.gameObject.SetActive(false);
            }

            // 2. Title ("PAUSE" in Crimson Red)
            var titleGo = mainPausePanel.transform.Find("PauzeTitel");
            if (titleGo != null)
            {
                var titleRt = titleGo.GetComponent<RectTransform>();
                if (titleRt != null)
                {
                    titleRt.anchoredPosition = new Vector2(0f, 165f);
                    titleRt.sizeDelta = new Vector2(280f, 50f);
                }

                var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
                if (titleTmp != null)
                {
                    titleTmp.text = "PAUSE";
                    titleTmp.color = crimsonRed;
                    titleTmp.fontSize = 38f;
                    titleTmp.fontStyle = FontStyles.Bold;
                    titleTmp.alignment = TextAlignmentOptions.Center;
                }
            }

            // 3. Subtitle ("PEW PEW ARENA")
            var subtitleTmp = GetOrCreateText(mainPausePanel.transform, "PauseSubtitle", new Vector2(0f, 130f), new Vector2(240f, 24f));
            subtitleTmp.text = "PEW PEW ARENA";
            subtitleTmp.color = goldAccent;
            subtitleTmp.fontSize = 12.5f;
            subtitleTmp.fontStyle = FontStyles.Bold;
            subtitleTmp.alignment = TextAlignmentOptions.Center;

            // 4. Ornamental Divider ("───  ◆  ───")
            var dividerTmp = GetOrCreateText(mainPausePanel.transform, "PauseDivider", new Vector2(0f, 104f), new Vector2(240f, 20f));
            dividerTmp.text = "───  ◆  ───";
            dividerTmp.color = goldBorderColor;
            dividerTmp.fontSize = 15f;
            dividerTmp.fontStyle = FontStyles.Normal;
            dividerTmp.alignment = TextAlignmentOptions.Center;

            // 5. Buttons Styling
            StyleMenuButton(resumeButton, new Vector2(0f, 40f), "RESUME", buttonBg, buttonHover, buttonBorder, textSilver, textGlow);
            StyleMenuButton(settingsButton, new Vector2(0f, -22f), "SETTINGS", buttonBg, buttonHover, buttonBorder, textSilver, textGlow);
            StyleMenuButton(quitButton, new Vector2(0f, -84f), "MAIN MENU", buttonBg, buttonHover, buttonBorder, textSilver, textGlow);

            // 6. Footer Star Ornament ("✦")
            var footerTmp = GetOrCreateText(mainPausePanel.transform, "PauseFooterEmblem", new Vector2(0f, -155f), new Vector2(40f, 30f));
            footerTmp.text = "✦";
            footerTmp.color = crimsonRed;
            footerTmp.fontSize = 18f;
            footerTmp.fontStyle = FontStyles.Normal;
            footerTmp.alignment = TextAlignmentOptions.Center;
        }

        private TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tr = parent.Find(name);
            TextMeshProUGUI tmp;
            if (tr == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                tmp = go.GetComponent<TextMeshProUGUI>();

                var titleTmp = parent.Find("PauzeTitel")?.GetComponent<TextMeshProUGUI>();
                if (titleTmp != null) tmp.font = titleTmp.font;
            }
            else
            {
                tmp = tr.GetComponent<TextMeshProUGUI>();
            }

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            return tmp;
        }

        private void StyleMenuButton(Button btn, Vector2 pos, string label, Color bg, Color hover, Color border, Color textNormal, Color textHover)
        {
            if (btn == null) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(250f, 46f);
            }

            var img = btn.targetGraphic as Image ?? btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = bg;
            }

            var outline = btn.GetComponent<Outline>();
            if (outline == null) outline = btn.gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var cb = btn.colors;
            cb.normalColor = bg;
            cb.highlightedColor = hover;
            cb.selectedColor = bg;
            cb.pressedColor = new Color(0.06f, 0.07f, 0.10f, 1f);
            btn.colors = cb;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = label;
                tmp.color = textNormal;
                tmp.fontSize = 16f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            var hoverItem = btn.GetComponent<PauseButtonHoverItem>();
            if (hoverItem == null) hoverItem = btn.gameObject.AddComponent<PauseButtonHoverItem>();
            hoverItem.Init(textNormal, textHover);
        }
        #endregion
    }

    /// <summary>
    /// Provides smooth scale lift and text glow when hovering over pause menu buttons.
    /// Operates using unscaled delta time so animations remain responsive when game is paused.
    /// </summary>
    public class PauseButtonHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _rect;
        private TextMeshProUGUI _text;
        private Color _normalTextColor = new Color(0.85f, 0.88f, 0.94f, 1f);
        private Color _hoverTextColor = new Color(1f, 0.96f, 0.82f, 1f);
        private Coroutine _animateRoutine;

        public void Init(Color normalCol, Color hoverCol)
        {
            _rect = GetComponent<RectTransform>();
            _text = GetComponentInChildren<TextMeshProUGUI>(true);
            _normalTextColor = normalCol;
            _hoverTextColor = hoverCol;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Animate(1.04f, _hoverTextColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Animate(1.0f, _normalTextColor);
        }

        private void Animate(float targetScale, Color targetColor)
        {
            if (_animateRoutine != null) StopCoroutine(_animateRoutine);
            if (gameObject.activeInHierarchy)
            {
                _animateRoutine = StartCoroutine(AnimateRoutine(targetScale, targetColor));
            }
            else
            {
                if (_rect != null) _rect.localScale = Vector3.one * targetScale;
                if (_text != null) _text.color = targetColor;
            }
        }

        private IEnumerator AnimateRoutine(float targetScale, Color targetColor)
        {
            float elapsed = 0f;
            float duration = 0.08f;
            Vector3 startScale = _rect != null ? _rect.localScale : Vector3.one;
            Color startColor = _text != null ? _text.color : _normalTextColor;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                if (_rect != null) _rect.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, t);
                if (_text != null) _text.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }

            if (_rect != null) _rect.localScale = Vector3.one * targetScale;
            if (_text != null) _text.color = targetColor;
            _animateRoutine = null;
        }

        private void OnDisable()
        {
            if (_animateRoutine != null)
            {
                StopCoroutine(_animateRoutine);
                _animateRoutine = null;
            }
            if (_rect != null) _rect.localScale = Vector3.one;
            if (_text != null) _text.color = _normalTextColor;
        }
    }
}