using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SniperGame.Gameplay;
using PewPewArena.UI;

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
        [SerializeField] private Sprite hoverArrowSprite;
        [SerializeField] private Sprite hoverArrowRightSprite;

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

            var cardBg = mainPausePanel.transform.Find("PauseCardBg") as RectTransform;
            var laserDivider = mainPausePanel.transform.Find("PauseLaserDivider") as RectTransform;
            var titleTr = mainPausePanel.transform.Find("PauzeTitel") as RectTransform;
            var subtitleTr = mainPausePanel.transform.Find("PauseSubtitle") as RectTransform;

            RectTransform[] btnRects = new RectTransform[]
            {
                resumeButton != null ? resumeButton.GetComponent<RectTransform>() : null,
                settingsButton != null ? settingsButton.GetComponent<RectTransform>() : null,
                quitButton != null ? quitButton.GetComponent<RectTransform>() : null
            };
            Vector2[] btnTargetPositions = new Vector2[]
            {
                new Vector2(0f, 36f),
                new Vector2(0f, -40f),
                new Vector2(0f, -116f)
            };

            // Reset all button hover arrows immediately on open
            var effects = mainPausePanel.GetComponentsInChildren<MainMenuButtonEffect>(true);
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null) effects[i].ApplyInstantIdleState();
            }

            float duration = show ? 0.24f : 0.14f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            float targetAlpha = show ? 1f : 0f;

            Vector3 cardStartScale = show ? (Vector3.one * 0.95f) : Vector3.one;
            Vector3 cardTargetScale = show ? Vector3.one : (Vector3.one * 0.95f);

            if (show)
            {
                cg.alpha = 0f;
                if (cardBg != null) cardBg.localScale = cardStartScale;
                if (laserDivider != null) laserDivider.localScale = new Vector3(0f, 1f, 1f);
                if (titleTr != null) titleTr.anchoredPosition = new Vector2(0f, 154f);
                if (subtitleTr != null) subtitleTr.anchoredPosition = new Vector2(0f, 206f);

                for (int i = 0; i < btnRects.Length; i++)
                {
                    if (btnRects[i] != null)
                        btnRects[i].anchoredPosition = btnTargetPositions[i] - new Vector2(0f, 14f);
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, progress);

                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(progress * 1.35f));

                if (cardBg != null)
                    cardBg.localScale = Vector3.LerpUnclamped(cardStartScale, cardTargetScale, ease);

                if (show)
                {
                    if (laserDivider != null)
                    {
                        float divT = Mathf.Clamp01((elapsed - 0.03f) / (duration * 0.85f));
                        laserDivider.localScale = new Vector3(Mathf.SmoothStep(0f, 1f, divT), 1f, 1f);
                    }

                    if (titleTr != null)
                        titleTr.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 154f), new Vector2(0f, 162f), ease);

                    if (subtitleTr != null)
                        subtitleTr.anchoredPosition = Vector2.LerpUnclamped(new Vector2(0f, 206f), new Vector2(0f, 212f), ease);

                    for (int i = 0; i < btnRects.Length; i++)
                    {
                        if (btnRects[i] != null)
                        {
                            float delay = i * 0.045f;
                            float btnT = Mathf.Clamp01((elapsed - delay) / (duration * 0.80f));
                            float btnEase = Mathf.SmoothStep(0f, 1f, btnT);
                            Vector2 startPos = btnTargetPositions[i] - new Vector2(0f, 14f);
                            btnRects[i].anchoredPosition = Vector2.LerpUnclamped(startPos, btnTargetPositions[i], btnEase);
                        }
                    }
                }

                yield return null;
            }

            cg.alpha = targetAlpha;
            if (cardBg != null) cardBg.localScale = cardTargetScale;
            if (laserDivider != null) laserDivider.localScale = Vector3.one;
            if (titleTr != null) titleTr.anchoredPosition = new Vector2(0f, 162f);
            if (subtitleTr != null) subtitleTr.anchoredPosition = new Vector2(0f, 212f);

            for (int i = 0; i < btnRects.Length; i++)
            {
                if (btnRects[i] != null)
                    btnRects[i].anchoredPosition = btnTargetPositions[i];
            }

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

            // AAA Tactical Palette (matching MainMenuPanel, PlayPanel, LoadoutPanel & SettingsPanel)
            Color dimmerBg = new Color(0.024f, 0.035f, 0.059f, 0.78f);         // #06090F Full-Screen Tactical Dimmer
            Color cardSlateBg = new Color(0.043f, 0.059f, 0.090f, 0.95f);      // #0B0F17 Deep Obsidian Command Card
            Color cardBorderColor = new Color(0.149f, 0.231f, 0.329f, 0.85f);  // #263B54 Tactical Steel-Cyan Border
            Color neonCyan = new Color(0f, 0.898f, 1f, 0.90f);                 // #00E5FF Electric Cyan Accent
            Color neonCyanSubtle = new Color(0f, 0.898f, 1f, 0.55f);           // #00E5FF Laser Divider
            Color textIdleSilver = new Color(0.85f, 0.90f, 0.93f, 1f);         // #D9E6ED Signature Button Idle
            Color textHoverWhite = Color.white;                                // #FFFFFF Signature Button Hover
            Color textClickCyan = new Color(0.70f, 1.0f, 0.90f, 1f);           // #B3FFE6 Signature Button Click
            Color footerSlate = new Color(0.424f, 0.510f, 0.600f, 0.85f);      // #6C8299 Footer Hint

            // 1. Full-Screen Tactical Dimmer on MainPausePanel
            var panelRt = mainPausePanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.one;
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = Vector2.zero;
                panelRt.localScale = Vector3.one;
            }

            var panelImg = mainPausePanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.sprite = null;
                panelImg.color = dimmerBg;
                panelImg.raycastTarget = true;
            }

            var panelOutline = mainPausePanel.GetComponent<Outline>();
            if (panelOutline != null) panelOutline.enabled = false;

            // 2. Centered Tactical Command Card ("PauseCardBg" 480x560)
            var cardBgImg = GetOrCreateImage(mainPausePanel.transform, "PauseCardBg", Vector2.zero, new Vector2(480f, 560f));
            cardBgImg.color = cardSlateBg;
            cardBgImg.raycastTarget = true;
            cardBgImg.transform.SetSiblingIndex(0);

            var cardOutline = cardBgImg.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = cardBgImg.gameObject.AddComponent<Outline>();
            cardOutline.enabled = true;
            cardOutline.effectColor = cardBorderColor;
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Top & Bottom Laser Accents inside PauseCardBg
            var topAccent = GetOrCreateImage(cardBgImg.transform, "TopLaserAccent", new Vector2(0f, 278.5f), new Vector2(480f, 3f));
            topAccent.color = neonCyan;
            topAccent.raycastTarget = false;

            var bottomAccent = GetOrCreateImage(cardBgImg.transform, "BottomLaserAccent", new Vector2(0f, -279f), new Vector2(160f, 2f));
            bottomAccent.color = new Color(0f, 0.898f, 1f, 0.45f);
            bottomAccent.raycastTarget = false;

            // Hide legacy clutter elements
            var oldBack = mainPausePanel.transform.Find("BackImageImprovement2");
            if (oldBack != null) oldBack.gameObject.SetActive(false);

            var oldDivider = mainPausePanel.transform.Find("PauseDivider");
            if (oldDivider != null) oldDivider.gameObject.SetActive(false);

            var oldFooter = mainPausePanel.transform.Find("PauseFooterEmblem");
            if (oldFooter != null) oldFooter.gameObject.SetActive(false);

            // 3. Tactical Sub-Header ("Goodboy")
            var subtitleTmp = GetOrCreateText(mainPausePanel.transform, "PauseSubtitle", new Vector2(0f, 212f), new Vector2(380f, 24f));
            subtitleTmp.text = "Goodboy";
            subtitleTmp.color = new Color(0f, 0.898f, 1f, 0.85f);
            subtitleTmp.fontSize = 14f;
            subtitleTmp.characterSpacing = 4f;
            subtitleTmp.fontStyle = FontStyles.Bold;
            subtitleTmp.alignment = TextAlignmentOptions.Center;
            subtitleTmp.raycastTarget = false;

            // 4. Hero Title ("PAUSED")
            var titleGo = mainPausePanel.transform.Find("PauzeTitel");
            if (titleGo != null)
            {
                var titleRt = titleGo.GetComponent<RectTransform>();
                if (titleRt != null)
                {
                    titleRt.anchoredPosition = new Vector2(0f, 162f);
                    titleRt.sizeDelta = new Vector2(400f, 64f);
                }

                var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
                if (titleTmp != null)
                {
                    titleTmp.text = "PAUSED";
                    titleTmp.color = Color.white;
                    titleTmp.fontSize = 56f;
                    titleTmp.characterSpacing = 5f;
                    titleTmp.fontStyle = FontStyles.Bold;
                    titleTmp.alignment = TextAlignmentOptions.Center;
                    titleTmp.raycastTarget = false;
                }
            }

            // 5. Sleek Laser Divider Bar (320 x 2px)
            var laserDividerImg = GetOrCreateImage(mainPausePanel.transform, "PauseLaserDivider", new Vector2(0f, 110f), new Vector2(320f, 2f));
            laserDividerImg.color = neonCyanSubtle;
            laserDividerImg.raycastTarget = false;

            // 6. Signature AAA Borderless Buttons with HoverArrow & MainMenuButtonEffect
            StyleMenuButton(resumeButton, new Vector2(0f, 36f), "RESUME", textIdleSilver, textHoverWhite, textClickCyan);
            StyleMenuButton(settingsButton, new Vector2(0f, -40f), "SETTINGS", textIdleSilver, textHoverWhite, textClickCyan);
            StyleMenuButton(quitButton, new Vector2(0f, -116f), "MAIN MENU", textIdleSilver, textHoverWhite, textClickCyan);

            // 7. Tactical Footer Keybind Prompt ("[ ESC ]  RETURN TO MATCH")
            var footerHintTmp = GetOrCreateText(mainPausePanel.transform, "PauseFooterHint", new Vector2(0f, -215f), new Vector2(360f, 28f));
            footerHintTmp.text = "[ ESC ]  RETURN TO MATCH";
            footerHintTmp.color = footerSlate;
            footerHintTmp.fontSize = 13f;
            footerHintTmp.characterSpacing = 3f;
            footerHintTmp.fontStyle = FontStyles.Bold;
            footerHintTmp.alignment = TextAlignmentOptions.Center;
            footerHintTmp.raycastTarget = false;
        }

        private Image GetOrCreateImage(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tr = parent.Find(name);
            Image img;
            if (tr == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                tr.gameObject.SetActive(true);
                img = tr.GetComponent<Image>();
                if (img == null) img = tr.gameObject.AddComponent<Image>();
            }

            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            return img;
        }

        private TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var tr = parent.Find(name);
            TextMeshProUGUI tmp;
            if (tr == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                tmp = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                tr.gameObject.SetActive(true);
                tmp = tr.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = tr.gameObject.AddComponent<TextMeshProUGUI>();
            }

            var titleTmp = parent.Find("PauzeTitel")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null && titleTmp.font != null)
            {
                tmp.font = titleTmp.font;
            }

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;

            return tmp;
        }

        private void StyleMenuButton(Button btn, Vector2 pos, string label, Color textNormal, Color textHover, Color textClick)
        {
            if (btn == null) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                if (rt.sizeDelta == Vector2.zero)
                {
                    rt.sizeDelta = new Vector2(280f, 52f);
                }
                rt.localScale = Vector3.one;
            }

            var img = btn.targetGraphic as Image ?? btn.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }

            var outline = btn.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;

            var legacyHover = btn.GetComponent<PauseButtonHoverItem>();
            if (legacyHover != null) legacyHover.enabled = false;

            btn.transition = Selectable.Transition.None;

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                var titleTmp = mainPausePanel != null ? mainPausePanel.transform.Find("PauzeTitel")?.GetComponent<TextMeshProUGUI>() : null;
                if (titleTmp != null && titleTmp.font != null)
                {
                    tmp.font = titleTmp.font;
                }

                tmp.text = label;
                tmp.color = textNormal;
                tmp.fontSize = 28f;
                tmp.characterSpacing = 3f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }

            if (hoverArrowSprite == null)
            {
                hoverArrowSprite = MainMenuButtonEffect.GetDefaultLeftArrow();
            }
            if (hoverArrowRightSprite == null)
            {
                hoverArrowRightSprite = MainMenuButtonEffect.GetDefaultRightArrow();
            }

            // Ensure HoverArrow exists with sprite assigned
            Transform arrowTr = btn.transform.Find("HoverArrow") ?? btn.transform.Find("Arrow");
            if (arrowTr != null && hoverArrowSprite != null)
            {
                var arrowImg = arrowTr.GetComponent<Image>();
                if (arrowImg != null && arrowImg.sprite == null)
                {
                    arrowImg.sprite = hoverArrowSprite;
                }
            }

            var effect = btn.GetComponent<MainMenuButtonEffect>();
            if (effect == null) effect = btn.gameObject.AddComponent<MainMenuButtonEffect>();

            if (hoverArrowSprite != null) effect.ArrowSprite = hoverArrowSprite;
            if (hoverArrowRightSprite != null) effect.RightArrowSprite = hoverArrowRightSprite;

            effect.InitializeComponents();
            effect.CalculateArrowPositions();
            effect.ApplyInstantIdleState();
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