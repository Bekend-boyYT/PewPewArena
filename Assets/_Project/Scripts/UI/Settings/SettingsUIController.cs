using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SniperGame.UI.Settings
{
    public class SettingsUIController : MonoBehaviour
    {
        public static SettingsUIController Instance { get; private set; }
        public int CurrentTabIndex => _currentTabIndex;

        [Header("Sidebar Buttons")]
        [SerializeField] private Button generalTabBtn;
        [SerializeField] private Button gameTabBtn;
        [SerializeField] private Button graphicsTabBtn;
        [SerializeField] private Button audioTabBtn;
        [SerializeField] private Button controlsTabBtn;

        [Header("Tab Button Highlighting")]
        [Tooltip("Background color of the currently active sidebar button")]
        [SerializeField] private Color activeTabBgColor = new Color(1f, 0.82f, 0.15f, 1f); // Vibrant Gold
        [Tooltip("Background color of inactive sidebar buttons")]
        [SerializeField] private Color inactiveTabBgColor = Color.white;
        [Tooltip("Text color of the currently active sidebar button")]
        [SerializeField] private Color activeTabTextColor = new Color(0.08f, 0.08f, 0.08f, 1f); // Crisp Dark/Black
        [Tooltip("Text color of inactive sidebar buttons")]
        [SerializeField] private Color inactiveTabTextColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Charcoal
        [Tooltip("Horizontal micro-nudge distance for the active tab towards the panel")]
        [SerializeField] private float tabNudgeDistance = 6f;

        [Header("Panels")]
        [SerializeField] private GameObject generalPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject controlsPanel;

        [Header("Keybinds Side Panel")]
        [SerializeField] private Button keybindsOpenBtn;
        [SerializeField] private GameObject keybindsSidePanel;
        [SerializeField] private Button keybindsCloseBtn;

        [Header("General UI Elements")]
        [SerializeField] private TMP_Dropdown displayModeDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private TMP_Dropdown fpsLimitDropdown;
        [SerializeField] private Toggle showFpsToggle;
        [SerializeField] private Button resetSettingsBtn;

        [Header("Game UI Elements")]
        [SerializeField] private Toggle crosshairToggle;
        [SerializeField] private Toggle hitmarkerToggle;
        [SerializeField] private Toggle damageNumbersToggle;

        [Header("Graphics UI Elements")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private Toggle shadowsToggle;
        [SerializeField] private Toggle antiAliasingToggle;
        [SerializeField] private Slider fovSlider;
        [SerializeField] private TextMeshProUGUI fovValueText;
        [SerializeField] private Slider viewDistanceSlider;
        [SerializeField] private TextMeshProUGUI viewDistanceValueText;

        [Header("Audio UI Elements")]
        [SerializeField] private Slider masterVolSlider;
        [SerializeField] private TextMeshProUGUI masterVolValueText;
        [SerializeField] private Slider sfxVolSlider;
        [SerializeField] private TextMeshProUGUI sfxVolValueText;
        [SerializeField] private Slider musicVolSlider;
        [SerializeField] private TextMeshProUGUI musicVolValueText;
        [SerializeField] private Slider voiceVolSlider;
        [SerializeField] private TextMeshProUGUI voiceVolValueText;

        [Header("Controls UI Elements")]
        [SerializeField] private Slider mouseSensSlider;
        [SerializeField] private TextMeshProUGUI mouseSensValueText;
        [SerializeField] private Slider adsSensSlider;
        [SerializeField] private TextMeshProUGUI adsSensValueText;
        [SerializeField] private Toggle invertYToggle;

        private GameObject[] _panels;
        private Button[] _tabButtons;
        private int _currentTabIndex = 0;
        private bool _isUpdatingUI = false;
        private Coroutine _fadeRoutine;
        private Coroutine _keybindsFadeRoutine;

        private void Awake()
        {
            Instance = this;

            // Ensure managers exist
            if (SettingsManager.Instance == null)
            {
                var mgrGo = new GameObject("SettingsManager");
                mgrGo.AddComponent<SettingsManager>();
            }

            if (KeybindManager.Instance == null)
            {
                var kbGo = new GameObject("KeybindManager");
                kbGo.AddComponent<KeybindManager>();
            }

            if (FPSDisplay.Instance == null)
            {
                var fpsGo = new GameObject("FPSDisplay");
                fpsGo.AddComponent<FPSDisplay>();
            }

            AutoResolveReferences();
            RegisterUIEvents();
            ApplyVisualTheme();
        }

        private void Start()
        {
            ApplyVisualTheme();
            ShowTabInstant(0); // General tab by default
            RefreshAllUI();
        }

        private void OnEnable()
        {
            AutoResolveReferences();
            RegisterUIEvents();
            ApplyVisualTheme();
            ShowTabInstant(_currentTabIndex);
            RefreshAllUI();
        }

        #region Auto-Resolution
        private void AutoResolveReferences()
        {
            // Find Sidebar Buttons if unassigned
            Transform settingsPanelTr = transform;
            if (generalTabBtn == null) generalTabBtn = FindButton(settingsPanelTr, "General");
            if (gameTabBtn == null) gameTabBtn = FindButton(settingsPanelTr, "Game");
            if (graphicsTabBtn == null) graphicsTabBtn = FindButton(settingsPanelTr, "Graphics");
            if (audioTabBtn == null) audioTabBtn = FindButton(settingsPanelTr, "Audio");
            if (controlsTabBtn == null) controlsTabBtn = FindButton(settingsPanelTr, "Controls");
            if (controlsTabBtn == null) controlsTabBtn = FindButton(settingsPanelTr, "Control");

            _tabButtons = new Button[] { generalTabBtn, gameTabBtn, graphicsTabBtn, audioTabBtn, controlsTabBtn };

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] != null)
                {
                    var hoverItem = _tabButtons[i].GetComponent<TabButtonHoverItem>();
                    if (hoverItem == null)
                    {
                        hoverItem = _tabButtons[i].gameObject.AddComponent<TabButtonHoverItem>();
                    }
                    hoverItem.Init();
                }
            }

            // Find Panels if unassigned
            if (generalPanel == null) generalPanel = FindChildRelaxed(settingsPanelTr, "General Panel");
            if (gamePanel == null) gamePanel = FindChildRelaxed(settingsPanelTr, "Game Panel");
            if (graphicsPanel == null) graphicsPanel = FindChildRelaxed(settingsPanelTr, "Graphics Panel");
            if (audioPanel == null) audioPanel = FindChildRelaxed(settingsPanelTr, "Audio Panel");
            if (controlsPanel == null) controlsPanel = FindChildRelaxed(settingsPanelTr, "Controls Panel");

            _panels = new GameObject[] { generalPanel, gamePanel, graphicsPanel, audioPanel, controlsPanel };

            // Ensure panel containers never block clicks
            foreach (var p in _panels)
            {
                if (p != null)
                {
                    var img = p.GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                }
            }

            // Ensure Back button is on top and wired
            var backBtnGo = FindChildRelaxed(settingsPanelTr, "SettingsBackButton");
            if (backBtnGo != null)
            {
                backBtnGo.transform.SetAsLastSibling();
                var btn = backBtnGo.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        if (PauseMenu.Instance != null)
                        {
                            PauseMenu.Instance.BackToMainPause();
                        }
                    });
                }
            }

            // Find Keybinds side panel & buttons
            if (keybindsSidePanel == null)
            {
                keybindsSidePanel = FindChildRelaxed(settingsPanelTr, "KeybindsSidePanel");
                if (keybindsSidePanel == null && controlsPanel != null)
                    keybindsSidePanel = FindChildRelaxed(controlsPanel.transform, "KeybindsSidePanel");
            }

            if (keybindsOpenBtn == null)
            {
                if (controlsPanel != null)
                    keybindsOpenBtn = controlsPanel.GetComponentInChildren<Button>(true);
            }

            if (keybindsCloseBtn == null && keybindsSidePanel != null)
            {
                var btns = keybindsSidePanel.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.Contains("Close") || b.GetComponentInChildren<TextMeshProUGUI>()?.text == "X")
                    {
                        keybindsCloseBtn = b;
                        break;
                    }
                }
            }

            // General Elements
            if (generalPanel != null)
            {
                var dropdowns = generalPanel.GetComponentsInChildren<TMP_Dropdown>(true);
                if (displayModeDropdown == null && dropdowns.Length > 0) displayModeDropdown = dropdowns[0];
                if (resolutionDropdown == null && dropdowns.Length > 1) resolutionDropdown = dropdowns[1];
                if (fpsLimitDropdown == null && dropdowns.Length > 2) fpsLimitDropdown = dropdowns[2];

                if (showFpsToggle == null) showFpsToggle = generalPanel.GetComponentInChildren<Toggle>(true);
                if (resetSettingsBtn == null) resetSettingsBtn = generalPanel.GetComponentInChildren<Button>(true);
            }

            // Game Elements
            if (gamePanel != null)
            {
                var toggles = gamePanel.GetComponentsInChildren<Toggle>(true);
                if (crosshairToggle == null && toggles.Length > 0) crosshairToggle = toggles[0];
                if (hitmarkerToggle == null && toggles.Length > 1) hitmarkerToggle = toggles[1];
                if (damageNumbersToggle == null && toggles.Length > 2) damageNumbersToggle = toggles[2];
            }

            // Graphics Elements
            if (graphicsPanel != null)
            {
                if (qualityDropdown == null) qualityDropdown = graphicsPanel.GetComponentInChildren<TMP_Dropdown>(true);

                var toggles = graphicsPanel.GetComponentsInChildren<Toggle>(true);
                if (vsyncToggle == null && toggles.Length > 0) vsyncToggle = toggles[0];
                if (shadowsToggle == null && toggles.Length > 1) shadowsToggle = toggles[1];
                if (antiAliasingToggle == null && toggles.Length > 2) antiAliasingToggle = toggles[2];

                var sliders = graphicsPanel.GetComponentsInChildren<Slider>(true);
                if (fovSlider == null && sliders.Length > 0) fovSlider = sliders[0];
                if (viewDistanceSlider == null && sliders.Length > 1) viewDistanceSlider = sliders[1];

                if (fovSlider != null) fovValueText = FindValueTextNextTo(fovSlider);
                if (viewDistanceSlider != null) viewDistanceValueText = FindValueTextNextTo(viewDistanceSlider);
            }

            // Audio Elements
            if (audioPanel != null)
            {
                var sliders = audioPanel.GetComponentsInChildren<Slider>(true);
                if (masterVolSlider == null && sliders.Length > 0) masterVolSlider = sliders[0];
                if (sfxVolSlider == null && sliders.Length > 1) sfxVolSlider = sliders[1];
                if (musicVolSlider == null && sliders.Length > 2) musicVolSlider = sliders[2];
                if (voiceVolSlider == null && sliders.Length > 3) voiceVolSlider = sliders[3];

                if (masterVolSlider != null) masterVolValueText = FindValueTextNextTo(masterVolSlider);
                if (sfxVolSlider != null) sfxVolValueText = FindValueTextNextTo(sfxVolSlider);
                if (musicVolSlider != null) musicVolValueText = FindValueTextNextTo(musicVolSlider);
                if (voiceVolSlider != null) voiceVolValueText = FindValueTextNextTo(voiceVolSlider);
            }

            // Controls Elements
            if (controlsPanel != null)
            {
                var sliders = controlsPanel.GetComponentsInChildren<Slider>(true);
                if (mouseSensSlider == null && sliders.Length > 0) mouseSensSlider = sliders[0];
                if (adsSensSlider == null && sliders.Length > 1) adsSensSlider = sliders[1];

                if (invertYToggle == null) invertYToggle = controlsPanel.GetComponentInChildren<Toggle>(true);

                if (mouseSensSlider != null) mouseSensValueText = FindValueTextNextTo(mouseSensSlider);
                if (adsSensSlider != null) adsSensValueText = FindValueTextNextTo(adsSensSlider);
            }

            SetupKeybindRows();
        }

        private GameObject FindChildRelaxed(Transform parent, string targetName)
        {
            string cleanTarget = targetName.Trim().ToLowerInvariant();
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name.Trim().ToLowerInvariant().StartsWith(cleanTarget))
                {
                    return child.gameObject;
                }
            }
            return null;
        }

        private Button FindButton(Transform parent, string targetName)
        {
            var go = FindChildRelaxed(parent, targetName);
            if (go == null) return null;
            var btn = go.GetComponent<Button>();
            if (btn != null) return btn;
            return go.GetComponentInChildren<Button>(true);
        }

        private TextMeshProUGUI FindValueTextNextTo(Slider slider)
        {
            if (slider == null || slider.transform.parent == null) return null;
            var texts = slider.transform.parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name.ToLowerInvariant().Contains("value") || t.name.ToLowerInvariant().Contains("text"))
                {
                    // Check if aligned vertically
                    if (Mathf.Abs(t.rectTransform.anchoredPosition.y - slider.GetComponent<RectTransform>().anchoredPosition.y) < 15f)
                    {
                        return t;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Event Wiring
        private void RegisterUIEvents()
        {
            // Tab Buttons
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int index = i;
                if (_tabButtons[i] != null)
                {
                    _tabButtons[i].onClick.RemoveAllListeners();
                    _tabButtons[i].onClick.AddListener(() => SwitchTab(index));
                }
            }

            // Keybinds side panel buttons
            if (keybindsOpenBtn != null)
            {
                keybindsOpenBtn.onClick.RemoveAllListeners();
                keybindsOpenBtn.onClick.AddListener(ToggleKeybindsPanel);
            }

            if (keybindsCloseBtn != null)
            {
                keybindsCloseBtn.onClick.RemoveAllListeners();
                keybindsCloseBtn.onClick.AddListener(() =>
                {
                    if (keybindsSidePanel != null)
                    {
                        if (gameObject.activeInHierarchy)
                        {
                            if (_keybindsFadeRoutine != null) StopCoroutine(_keybindsFadeRoutine);
                            _keybindsFadeRoutine = StartCoroutine(FadeKeybindsRoutine(false));
                        }
                        else
                        {
                            keybindsSidePanel.SetActive(false);
                        }
                    }
                });
            }

            // General
            if (displayModeDropdown != null)
            {
                displayModeDropdown.onValueChanged.RemoveAllListeners();
                displayModeDropdown.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetDisplayMode(val); });
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.RemoveAllListeners();
                resolutionDropdown.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetResolution(val); });
            }

            if (fpsLimitDropdown != null)
            {
                fpsLimitDropdown.onValueChanged.RemoveAllListeners();
                fpsLimitDropdown.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetFpsLimit(val); });
            }

            if (showFpsToggle != null)
            {
                showFpsToggle.onValueChanged.RemoveAllListeners();
                showFpsToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetShowFps(val); });
            }

            if (resetSettingsBtn != null)
            {
                resetSettingsBtn.onClick.RemoveAllListeners();
                resetSettingsBtn.onClick.AddListener(() =>
                {
                    SettingsManager.ResetToDefaults();
                    RefreshAllUI();
                });
            }

            // Game
            if (crosshairToggle != null)
            {
                crosshairToggle.onValueChanged.RemoveAllListeners();
                crosshairToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetCrosshair(val); });
            }

            if (hitmarkerToggle != null)
            {
                hitmarkerToggle.onValueChanged.RemoveAllListeners();
                hitmarkerToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetHitmarkers(val); });
            }

            if (damageNumbersToggle != null)
            {
                damageNumbersToggle.onValueChanged.RemoveAllListeners();
                damageNumbersToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetDamageNumbers(val); });
            }

            // Graphics
            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.RemoveAllListeners();
                qualityDropdown.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetQuality(val); });
            }

            if (vsyncToggle != null)
            {
                vsyncToggle.onValueChanged.RemoveAllListeners();
                vsyncToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetVSync(val); });
            }

            if (shadowsToggle != null)
            {
                shadowsToggle.onValueChanged.RemoveAllListeners();
                shadowsToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetShadows(val); });
            }

            if (antiAliasingToggle != null)
            {
                antiAliasingToggle.onValueChanged.RemoveAllListeners();
                antiAliasingToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetAntiAliasing(val); });
            }

            if (fovSlider != null)
            {
                fovSlider.onValueChanged.RemoveAllListeners();
                fovSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetFOV(val);
                    if (fovValueText != null) fovValueText.text = $"{Mathf.RoundToInt(val)}°";
                });
            }

            if (viewDistanceSlider != null)
            {
                viewDistanceSlider.onValueChanged.RemoveAllListeners();
                viewDistanceSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetViewDistance(val);
                    if (viewDistanceValueText != null) viewDistanceValueText.text = $"{Mathf.RoundToInt(val)}m";
                });
            }

            // Audio
            if (masterVolSlider != null)
            {
                masterVolSlider.onValueChanged.RemoveAllListeners();
                masterVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetMasterVolume(val);
                    if (masterVolValueText != null) masterVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                });
            }

            if (sfxVolSlider != null)
            {
                sfxVolSlider.onValueChanged.RemoveAllListeners();
                sfxVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetSfxVolume(val);
                    if (sfxVolValueText != null) sfxVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                });
            }

            if (musicVolSlider != null)
            {
                musicVolSlider.onValueChanged.RemoveAllListeners();
                musicVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetMusicVolume(val);
                    if (musicVolValueText != null) musicVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                });
            }

            if (voiceVolSlider != null)
            {
                voiceVolSlider.onValueChanged.RemoveAllListeners();
                voiceVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetVoiceVolume(val);
                    if (voiceVolValueText != null) voiceVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                });
            }

            // Controls
            if (mouseSensSlider != null)
            {
                mouseSensSlider.onValueChanged.RemoveAllListeners();
                mouseSensSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetMouseSensitivity(val);
                    if (mouseSensValueText != null) mouseSensValueText.text = $"{val:F2}x";
                });
            }

            if (adsSensSlider != null)
            {
                adsSensSlider.onValueChanged.RemoveAllListeners();
                adsSensSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetADSSensitivity(val);
                    if (adsSensValueText != null) adsSensValueText.text = $"{val:F2}x";
                });
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.RemoveAllListeners();
                invertYToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetInvertY(val); });
            }
        }

        private void SetupKeybindRows()
        {
            if (keybindsSidePanel == null) return;

            var actionMap = new (string rowName, KeyAction action)[]
            {
                ("Row_Move", KeyAction.Move),
                ("Row_Jump", KeyAction.Jump),
                ("Row_Crouch", KeyAction.Crouch),
                ("Row_Slide", KeyAction.Slide),
                ("Row_WallJump", KeyAction.WallJump),
                ("Row_Shoot", KeyAction.Shoot),
                ("Row_Aim", KeyAction.Aim),
                ("Row_Reload", KeyAction.Reload)
            };

            foreach (var item in actionMap)
            {
                var row = FindChildRelaxed(keybindsSidePanel.transform, item.rowName);
                if (row == null) continue;

                var btn = row.GetComponentInChildren<Button>(true);
                var tmp = btn != null ? btn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                if (tmp != null)
                {
                    tmp.text = KeybindManager.GetKeyDisplayName(item.action);
                    tmp.color = new Color(1f, 0.85f, 0.2f, 1f); // Cyber Gold
                    tmp.fontStyle = FontStyles.Bold;
                }

                if (btn != null)
                {
                    var btnImg = btn.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color(0.106f, 0.118f, 0.169f, 1f);
                }

                if (btn != null && item.action != KeyAction.Move)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        KeybindManager.Instance.StartRebinding(item.action, tmp);
                    });
                }
            }
        }
        #endregion

        #region Tab Navigation
        public void SwitchTab(int tabIndex)
        {
            if (_panels == null || _panels.Length == 0) return;
            tabIndex = Mathf.Clamp(tabIndex, 0, _panels.Length - 1);

            if (tabIndex == _currentTabIndex && _panels[tabIndex] != null && _panels[tabIndex].activeSelf)
            {
                UpdateTabButtonVisuals();
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                _fadeRoutine = StartCoroutine(CrossFadePanelRoutine(tabIndex));
            }
            else
            {
                ShowTabInstant(tabIndex);
            }
        }

        private void ShowTabInstant(int tabIndex)
        {
            if (_panels == null || _panels.Length == 0) return;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _currentTabIndex = Mathf.Clamp(tabIndex, 0, _panels.Length - 1);

            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                {
                    bool isTarget = (i == _currentTabIndex);
                    _panels[i].SetActive(isTarget);

                    var cg = _panels[i].GetComponent<CanvasGroup>();
                    if (cg == null) cg = _panels[i].AddComponent<CanvasGroup>();
                    cg.alpha = isTarget ? 1f : 0f;
                    cg.interactable = isTarget;
                    cg.blocksRaycasts = isTarget;

                    var img = _panels[i].GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                }
            }

            UpdateTabButtonVisuals();

            var backBtnGo = FindChildRelaxed(transform, "SettingsBackButton");
            if (backBtnGo != null) backBtnGo.transform.SetAsLastSibling();

            if (_currentTabIndex != 4 && keybindsSidePanel != null)
            {
                keybindsSidePanel.SetActive(false);
            }
        }

        private IEnumerator CrossFadePanelRoutine(int targetIndex)
        {
            if (_panels == null || targetIndex < 0 || targetIndex >= _panels.Length) yield break;

            int prevIndex = _currentTabIndex;
            _currentTabIndex = targetIndex;
            UpdateTabButtonVisuals();

            // Close Keybinds side panel when leaving Controls tab
            if (_currentTabIndex != 4 && keybindsSidePanel != null && keybindsSidePanel.activeSelf)
            {
                if (_keybindsFadeRoutine != null) StopCoroutine(_keybindsFadeRoutine);
                _keybindsFadeRoutine = StartCoroutine(FadeKeybindsRoutine(false));
            }

            // Ensure Back button is on top of active panel
            var backBtnGo = FindChildRelaxed(transform, "SettingsBackButton");
            if (backBtnGo != null) backBtnGo.transform.SetAsLastSibling();

            GameObject incomingPanel = _panels[targetIndex];
            GameObject outgoingPanel = (prevIndex >= 0 && prevIndex < _panels.Length && prevIndex != targetIndex) ? _panels[prevIndex] : null;

            if (incomingPanel == null) yield break;

            var inCg = incomingPanel.GetComponent<CanvasGroup>();
            if (inCg == null) inCg = incomingPanel.AddComponent<CanvasGroup>();

            CanvasGroup outCg = null;
            if (outgoingPanel != null)
            {
                outCg = outgoingPanel.GetComponent<CanvasGroup>();
                if (outCg == null) outCg = outgoingPanel.AddComponent<CanvasGroup>();
            }

            incomingPanel.SetActive(true);
            var inImg = incomingPanel.GetComponent<Image>();
            if (inImg != null) inImg.raycastTarget = false;

            inCg.alpha = 0f;
            inCg.interactable = false;
            inCg.blocksRaycasts = false;

            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                inCg.alpha = t;
                if (outCg != null)
                {
                    outCg.alpha = 1f - t;
                }

                yield return null;
            }

            inCg.alpha = 1f;
            inCg.interactable = true;
            inCg.blocksRaycasts = true;

            // Deactivate other panels
            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null && i != _currentTabIndex)
                {
                    _panels[i].SetActive(false);
                    var cg = _panels[i].GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 0f;
                        cg.interactable = false;
                        cg.blocksRaycasts = false;
                    }
                }
            }

            _fadeRoutine = null;
        }

        private void UpdateTabButtonVisuals()
        {
            if (_tabButtons == null) return;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;

                bool isActive = (i == _currentTabIndex);
                Color targetBg = isActive ? activeTabBgColor : inactiveTabBgColor;
                Color targetText = isActive ? activeTabTextColor : inactiveTabTextColor;

                // 1. Direct Image background color for instant visual update
                var btnImg = _tabButtons[i].targetGraphic as Image ?? _tabButtons[i].GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = targetBg;
                }

                // 2. Button ColorBlock transition colors (ensures persistent highlight during select/hover)
                var colors = _tabButtons[i].colors;
                colors.normalColor = targetBg;
                colors.selectedColor = targetBg;
                colors.highlightedColor = isActive ? targetBg : new Color(0.92f, 0.92f, 0.92f, 1f);
                colors.pressedColor = isActive ? targetBg : new Color(0.85f, 0.85f, 0.85f, 1f);
                _tabButtons[i].colors = colors;

                // 3. Child TextMeshProUGUI styling (color + bold state)
                var tmp = _tabButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.color = targetText;
                    tmp.fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
                }

                // 4. Smooth micro-nudge
                var hoverItem = _tabButtons[i].GetComponent<TabButtonHoverItem>();
                if (hoverItem != null)
                {
                    hoverItem.SetActiveTab(isActive, tabNudgeDistance);
                }
            }
        }

        public void ToggleKeybindsPanel()
        {
            if (keybindsSidePanel != null)
            {
                bool newState = !keybindsSidePanel.activeSelf;
                if (_keybindsFadeRoutine != null)
                {
                    StopCoroutine(_keybindsFadeRoutine);
                    _keybindsFadeRoutine = null;
                }

                if (gameObject.activeInHierarchy)
                {
                    _keybindsFadeRoutine = StartCoroutine(FadeKeybindsRoutine(newState));
                }
                else
                {
                    keybindsSidePanel.SetActive(newState);
                    if (newState)
                    {
                        SetupKeybindRows();
                    }
                }
            }
        }

        private IEnumerator FadeKeybindsRoutine(bool show)
        {
            if (keybindsSidePanel == null) yield break;

            var cg = keybindsSidePanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = keybindsSidePanel.AddComponent<CanvasGroup>();

            if (show)
            {
                keybindsSidePanel.SetActive(true);
                SetupKeybindRows();
                cg.alpha = 0f;
            }

            float duration = 0.12f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            float targetAlpha = show ? 1f : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            cg.alpha = targetAlpha;
            cg.interactable = show;
            cg.blocksRaycasts = show;

            if (!show)
            {
                keybindsSidePanel.SetActive(false);
            }

            _keybindsFadeRoutine = null;
        }
        #endregion

        #region Refresh UI Values
        public void RefreshAllUI()
        {
            _isUpdatingUI = true;

            // General
            if (displayModeDropdown != null) displayModeDropdown.value = SettingsManager.DisplayModeIndex;
            if (resolutionDropdown != null) resolutionDropdown.value = SettingsManager.ResolutionIndex;
            if (fpsLimitDropdown != null) fpsLimitDropdown.value = SettingsManager.FpsLimitIndex;
            if (showFpsToggle != null) showFpsToggle.isOn = SettingsManager.ShowFps;

            // Game
            if (crosshairToggle != null) crosshairToggle.isOn = SettingsManager.ShowCrosshair;
            if (hitmarkerToggle != null) hitmarkerToggle.isOn = SettingsManager.ShowHitmarkers;
            if (damageNumbersToggle != null) damageNumbersToggle.isOn = SettingsManager.ShowDamageNumbers;

            // Graphics
            if (qualityDropdown != null) qualityDropdown.value = SettingsManager.QualityIndex;
            if (vsyncToggle != null) vsyncToggle.isOn = SettingsManager.VSyncEnabled;
            if (shadowsToggle != null) shadowsToggle.isOn = SettingsManager.ShadowsEnabled;
            if (antiAliasingToggle != null) antiAliasingToggle.isOn = SettingsManager.AntiAliasingEnabled;
            if (fovSlider != null) fovSlider.value = SettingsManager.FOV;
            if (fovValueText != null) fovValueText.text = $"{Mathf.RoundToInt(SettingsManager.FOV)}°";
            if (viewDistanceSlider != null) viewDistanceSlider.value = SettingsManager.ViewDistance;
            if (viewDistanceValueText != null) viewDistanceValueText.text = $"{Mathf.RoundToInt(SettingsManager.ViewDistance)}m";

            // Audio
            if (masterVolSlider != null) masterVolSlider.value = SettingsManager.MasterVolume;
            if (masterVolValueText != null) masterVolValueText.text = $"{Mathf.RoundToInt(SettingsManager.MasterVolume * 100)}%";
            if (sfxVolSlider != null) sfxVolSlider.value = SettingsManager.SfxVolume;
            if (sfxVolValueText != null) sfxVolValueText.text = $"{Mathf.RoundToInt(SettingsManager.SfxVolume * 100)}%";
            if (musicVolSlider != null) musicVolSlider.value = SettingsManager.MusicVolume;
            if (musicVolValueText != null) musicVolValueText.text = $"{Mathf.RoundToInt(SettingsManager.MusicVolume * 100)}%";
            if (voiceVolSlider != null) voiceVolSlider.value = SettingsManager.VoiceVolume;
            if (voiceVolValueText != null) voiceVolValueText.text = $"{Mathf.RoundToInt(SettingsManager.VoiceVolume * 100)}%";

            // Controls
            if (mouseSensSlider != null) mouseSensSlider.value = SettingsManager.MouseSensitivity;
            if (mouseSensValueText != null) mouseSensValueText.text = $"{SettingsManager.MouseSensitivity:F2}x";
            if (adsSensSlider != null) adsSensSlider.value = SettingsManager.ADSSensitivity;
            if (adsSensValueText != null) adsSensValueText.text = $"{SettingsManager.ADSSensitivity:F2}x";
            if (invertYToggle != null) invertYToggle.isOn = SettingsManager.InvertY;

            SetupKeybindRows();

            _isUpdatingUI = false;
        }
        #endregion

        #region Visual Theme & Scaling Polish
        [ContextMenu("Apply Visual Theme")]
        public void ApplyVisualTheme()
        {
            Color darkCharcoalBg = new Color(0.078f, 0.086f, 0.125f, 0.96f); // #141620
            Color panelMatteBg = new Color(0.063f, 0.071f, 0.106f, 0.85f);   // #10121B
            Color controlBoxBg = new Color(0.106f, 0.118f, 0.169f, 1f);      // #1B1E2B
            Color controlBoxHover = new Color(0.153f, 0.169f, 0.235f, 1f);   // #272B3C
            Color cyberGoldAccent = new Color(1f, 0.80f, 0.12f, 1f);          // #FFCC1F
            Color cyberGoldHover = new Color(1f, 0.88f, 0.35f, 1f);           // #FFE059
            Color textPrimary = new Color(0.94f, 0.95f, 0.98f, 1f);           // #F0F2FA

            // 1. Panels Backgrounds
            if (_panels != null)
            {
                foreach (var p in _panels)
                {
                    if (p == null) continue;
                    var img = p.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = panelMatteBg;
                        img.raycastTarget = false;
                    }
                }
            }

            // 2. Dropdown Fields Scaling & Colors
            StyleAllDropdowns(controlBoxBg, controlBoxHover, darkCharcoalBg, cyberGoldAccent, textPrimary);

            // 3. Sliders Styling
            StyleAllSliders(controlBoxBg, cyberGoldAccent, cyberGoldHover);

            // 4. Toggles Styling
            StyleAllToggles(controlBoxBg, controlBoxHover, cyberGoldAccent, textPrimary);

            // 5. Keybinds Side Panel & Rows
            StyleKeybindsPanel(darkCharcoalBg, controlBoxBg, controlBoxHover, cyberGoldAccent, textPrimary);
        }

        private void StyleAllDropdowns(Color boxBg, Color boxHover, Color templateBg, Color goldAccent, Color textCol)
        {
            var dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
            foreach (var dd in dropdowns)
            {
                if (dd == null) continue;

                // Scale Main Dropdown Box: 190w x 34h
                var rt = dd.GetComponent<RectTransform>();
                if (rt != null)
                {
                    var size = rt.sizeDelta;
                    if (size.y < 32f) size.y = 34f;
                    if (size.x < 185f)
                    {
                        // Shift X slightly right if at original narrow center so it expands rightward without overlapping titles
                        if (Mathf.Abs(rt.anchoredPosition.x - 81.28f) < 2f)
                        {
                            var pos = rt.anchoredPosition;
                            pos.x = 101.28f;
                            rt.anchoredPosition = pos;
                        }
                        else if (Mathf.Abs(rt.anchoredPosition.x - 20.2f) < 2f)
                        {
                            var pos = rt.anchoredPosition;
                            pos.x = 40.2f;
                            rt.anchoredPosition = pos;
                        }
                        size.x = 190f;
                    }
                    rt.sizeDelta = size;
                }

                // Main Dropdown Box Colors
                var ddImg = dd.targetGraphic as Image ?? dd.GetComponent<Image>();
                if (ddImg != null) ddImg.color = boxBg;

                var cb = dd.colors;
                cb.normalColor = boxBg;
                cb.highlightedColor = boxHover;
                cb.selectedColor = boxBg;
                cb.pressedColor = new Color(0.08f, 0.09f, 0.13f, 1f);
                dd.colors = cb;

                // Caption Text
                if (dd.captionText != null)
                {
                    dd.captionText.color = textCol;
                    dd.captionText.fontSize = 13.5f;
                    dd.captionText.alignment = TextAlignmentOptions.MidlineLeft;
                    var cRt = dd.captionText.rectTransform;
                    if (cRt != null)
                    {
                        cRt.offsetMin = new Vector2(10f, 0f);
                        cRt.offsetMax = new Vector2(-28f, 0f);
                    }
                }

                // Arrow Image
                var arrow = dd.transform.Find("Arrow")?.GetComponent<Image>();
                if (arrow == null)
                {
                    var images = dd.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                    {
                        if (img != ddImg && img.name.ToLowerInvariant().Contains("arrow"))
                        {
                            arrow = img;
                            break;
                        }
                    }
                }
                if (arrow != null)
                {
                    arrow.color = goldAccent;
                }

                // Template (Popup Menu)
                if (dd.template != null)
                {
                    var tRt = dd.template;
                    tRt.sizeDelta = new Vector2(0f, 160f);
                    tRt.pivot = new Vector2(0.5f, 1f);
                    tRt.anchoredPosition = new Vector2(0f, -2f);

                    var tImg = tRt.GetComponent<Image>();
                    if (tImg != null)
                    {
                        tImg.color = templateBg;
                    }

                    // Item in Template
                    var item = tRt.Find("Viewport/Content/Item");
                    if (item == null)
                    {
                        var tog = tRt.GetComponentInChildren<Toggle>(true);
                        if (tog != null) item = tog.transform;
                    }

                    if (item != null)
                    {
                        var itemRt = item.GetComponent<RectTransform>();
                        if (itemRt != null)
                        {
                            itemRt.sizeDelta = new Vector2(0f, 34f);
                        }

                        var itemToggle = item.GetComponent<Toggle>();
                        if (itemToggle != null)
                        {
                            var tcb = itemToggle.colors;
                            tcb.normalColor = boxBg;
                            tcb.highlightedColor = goldAccent;
                            tcb.selectedColor = goldAccent;
                            tcb.pressedColor = new Color(0.85f, 0.68f, 0.1f, 1f);
                            itemToggle.colors = tcb;
                        }

                        var itemBg = item.Find("Item Background")?.GetComponent<Image>();
                        if (itemBg != null) itemBg.color = boxBg;

                        var itemText = item.Find("Item Text")?.GetComponent<TextMeshProUGUI>();
                        if (itemText != null)
                        {
                            itemText.color = textCol;
                            itemText.fontSize = 13f;
                            itemText.alignment = TextAlignmentOptions.MidlineLeft;
                            var itRt = itemText.rectTransform;
                            if (itRt != null)
                            {
                                itRt.offsetMin = new Vector2(12f, 0f);
                                itRt.offsetMax = new Vector2(-28f, 0f);
                            }
                        }

                        var itemCheck = item.Find("Item Checkmark")?.GetComponent<Image>();
                        if (itemCheck != null)
                        {
                            itemCheck.color = goldAccent;
                        }
                    }

                    // Scrollbar in Template
                    var sb = tRt.GetComponentInChildren<Scrollbar>(true);
                    if (sb != null)
                    {
                        var sbImg = sb.GetComponent<Image>();
                        if (sbImg != null) sbImg.color = new Color(0.06f, 0.07f, 0.10f, 1f);
                        var handleImg = sb.targetGraphic as Image ?? sb.handleRect?.GetComponent<Image>();
                        if (handleImg != null) handleImg.color = new Color(0.3f, 0.33f, 0.44f, 1f);
                    }
                }
            }
        }

        private void StyleAllSliders(Color trackBg, Color goldAccent, Color goldHover)
        {
            var sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s == null) continue;

                // Track Background
                var bg = s.transform.Find("Background")?.GetComponent<Image>();
                if (bg != null) bg.color = trackBg;

                // Fill Area
                if (s.fillRect != null)
                {
                    var fillImg = s.fillRect.GetComponent<Image>();
                    if (fillImg != null) fillImg.color = goldAccent;
                }

                // Handle Area
                if (s.handleRect != null)
                {
                    var handleImg = s.handleRect.GetComponent<Image>();
                    if (handleImg != null) handleImg.color = Color.white;
                }

                // Value Text next to slider
                var valText = FindValueTextNextTo(s);
                if (valText != null)
                {
                    valText.color = goldAccent;
                    valText.fontStyle = FontStyles.Bold;
                }
            }
        }

        private void StyleAllToggles(Color boxBg, Color boxHover, Color goldAccent, Color textCol)
        {
            var toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var tog in toggles)
            {
                if (tog == null) continue;
                // Skip dropdown template item toggles
                if (tog.transform.parent != null && tog.transform.parent.name == "Content") continue;

                var bg = tog.transform.Find("Background")?.GetComponent<Image>() ?? tog.targetGraphic as Image;
                if (bg != null) bg.color = boxBg;

                var cb = tog.colors;
                cb.normalColor = boxBg;
                cb.highlightedColor = boxHover;
                cb.pressedColor = boxBg;
                cb.selectedColor = boxBg;
                tog.colors = cb;

                if (tog.graphic is Image checkImg)
                {
                    checkImg.color = goldAccent;
                }

                var label = tog.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.color = textCol;
                }
            }
        }

        private void StyleKeybindsPanel(Color panelBg, Color boxBg, Color boxHover, Color goldAccent, Color textCol)
        {
            if (keybindsSidePanel == null) return;

            var pImg = keybindsSidePanel.GetComponent<Image>();
            if (pImg != null) pImg.color = panelBg;

            // Style Title
            var title = keybindsSidePanel.transform.Find("Title_Keybinds")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.color = goldAccent;
                title.fontStyle = FontStyles.Bold;
            }

            // Style Close Button
            if (keybindsCloseBtn != null)
            {
                var cbImg = keybindsCloseBtn.GetComponent<Image>();
                if (cbImg != null) cbImg.color = new Color(0.22f, 0.10f, 0.10f, 1f);
                var cbText = keybindsCloseBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (cbText != null) cbText.color = new Color(1f, 0.45f, 0.45f, 1f);
            }

            // Style Rows
            var actionMap = new string[] { "Row_Move", "Row_Jump", "Row_Crouch", "Row_Slide", "Row_WallJump", "Row_Shoot", "Row_Aim", "Row_Reload" };
            foreach (var rowName in actionMap)
            {
                var row = FindChildRelaxed(keybindsSidePanel.transform, rowName);
                if (row == null) continue;

                var label = row.transform.Find("Action_Label")?.GetComponent<TextMeshProUGUI>();
                if (label == null)
                {
                    var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                    if (texts.Length > 0) label = texts[0];
                }
                if (label != null)
                {
                    label.color = textCol;
                    label.fontStyle = FontStyles.Bold;
                }

                var btn = row.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    var btnImg = btn.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = boxBg;

                    var bcb = btn.colors;
                    bcb.normalColor = boxBg;
                    bcb.highlightedColor = boxHover;
                    bcb.pressedColor = new Color(0.08f, 0.09f, 0.12f, 1f);
                    btn.colors = bcb;

                    var btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (btnText != null)
                    {
                        btnText.color = goldAccent;
                        btnText.fontStyle = FontStyles.Bold;
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides smooth micro-nudge and tactile visual feedback on pointer hover and tab selection.
    /// Uses unscaled delta time so animations remain responsive even if game is paused.
    /// </summary>
    public class TabButtonHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _rect;
        private Vector2 _basePos;
        private bool _isInitialized;
        private bool _isActive;
        private bool _isHovered;
        private Coroutine _nudgeRoutine;

        public void Init()
        {
            if (_isInitialized) return;
            _rect = GetComponent<RectTransform>();
            if (_rect != null)
            {
                _basePos = _rect.anchoredPosition;
                _isInitialized = true;
            }
        }

        public void SetActiveTab(bool active, float nudgeDistance)
        {
            Init();
            _isActive = active;
            float targetX = _basePos.x + (active ? nudgeDistance : (_isHovered ? 3f : 0f));
            MoveToX(targetX);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Init();
            _isHovered = true;
            if (!_isActive && _rect != null)
            {
                MoveToX(_basePos.x + 3f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Init();
            _isHovered = false;
            if (!_isActive && _rect != null)
            {
                MoveToX(_basePos.x);
            }
        }

        private void MoveToX(float targetX)
        {
            if (_rect == null) return;
            if (_nudgeRoutine != null)
            {
                StopCoroutine(_nudgeRoutine);
                _nudgeRoutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                _nudgeRoutine = StartCoroutine(SmoothNudge(targetX));
            }
            else
            {
                var p = _rect.anchoredPosition;
                p.x = targetX;
                _rect.anchoredPosition = p;
            }
        }

        private IEnumerator SmoothNudge(float targetX)
        {
            float startX = _rect.anchoredPosition.x;
            float elapsed = 0f;
            float duration = 0.08f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);
                var p = _rect.anchoredPosition;
                p.x = Mathf.Lerp(startX, targetX, t);
                _rect.anchoredPosition = p;
                yield return null;
            }

            var finalPos = _rect.anchoredPosition;
            finalPos.x = targetX;
            _rect.anchoredPosition = finalPos;
            _nudgeRoutine = null;
        }

        private void OnDisable()
        {
            if (_nudgeRoutine != null)
            {
                StopCoroutine(_nudgeRoutine);
                _nudgeRoutine = null;
            }
            if (_isInitialized && _rect != null && !_isActive)
            {
                var p = _rect.anchoredPosition;
                p.x = _basePos.x;
                _rect.anchoredPosition = p;
            }
        }
    }
}
