using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SniperGame.UI.Settings
{
    public class SettingsUIController : MonoBehaviour
    {
        public static SettingsUIController Instance { get; private set; }

        [Header("Sidebar Buttons")]
        [SerializeField] private Button generalTabBtn;
        [SerializeField] private Button gameTabBtn;
        [SerializeField] private Button graphicsTabBtn;
        [SerializeField] private Button audioTabBtn;
        [SerializeField] private Button controlsTabBtn;

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
        }

        private void Start()
        {
            SwitchTab(0); // General tab by default
            RefreshAllUI();
        }

        private void OnEnable()
        {
            AutoResolveReferences();
            RegisterUIEvents();
            SwitchTab(_currentTabIndex);
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
                    if (keybindsSidePanel != null) keybindsSidePanel.SetActive(false);
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
                    if (fovValueText != null) fovValueText.text = $"{Mathf.RoundToInt(val)}";
                });
            }

            if (viewDistanceSlider != null)
            {
                viewDistanceSlider.onValueChanged.RemoveAllListeners();
                viewDistanceSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetViewDistance(val);
                    if (viewDistanceValueText != null) viewDistanceValueText.text = $"{Mathf.RoundToInt(val)}";
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
                    if (mouseSensValueText != null) mouseSensValueText.text = $"{val:F2}";
                });
            }

            if (adsSensSlider != null)
            {
                adsSensSlider.onValueChanged.RemoveAllListeners();
                adsSensSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetADSSensitivity(val);
                    if (adsSensValueText != null) adsSensValueText.text = $"{val:F2}";
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
            _currentTabIndex = Mathf.Clamp(tabIndex, 0, _panels.Length - 1);

            for (int i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                {
                    _panels[i].SetActive(i == _currentTabIndex);
                    var img = _panels[i].GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                }

                if (_tabButtons[i] != null)
                {
                    // Subtle visual highlight on active tab button
                    var colors = _tabButtons[i].colors;
                    colors.normalColor = (i == _currentTabIndex) ? new Color(1f, 0.9f, 0.75f, 1f) : Color.white;
                    _tabButtons[i].colors = colors;
                }
            }

            // Ensure Back button is on top of active panel
            var backBtnGo = FindChildRelaxed(transform, "SettingsBackButton");
            if (backBtnGo != null) backBtnGo.transform.SetAsLastSibling();

            // Close Keybinds side panel when leaving Controls tab
            if (_currentTabIndex != 4 && keybindsSidePanel != null)
            {
                keybindsSidePanel.SetActive(false);
            }
        }

        public void ToggleKeybindsPanel()
        {
            if (keybindsSidePanel != null)
            {
                bool newState = !keybindsSidePanel.activeSelf;
                keybindsSidePanel.SetActive(newState);
                if (newState)
                {
                    SetupKeybindRows();
                }
            }
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
            if (fovValueText != null) fovValueText.text = $"{Mathf.RoundToInt(SettingsManager.FOV)}";
            if (viewDistanceSlider != null) viewDistanceSlider.value = SettingsManager.ViewDistance;
            if (viewDistanceValueText != null) viewDistanceValueText.text = $"{Mathf.RoundToInt(SettingsManager.ViewDistance)}";

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
            if (mouseSensValueText != null) mouseSensValueText.text = $"{SettingsManager.MouseSensitivity:F2}";
            if (adsSensSlider != null) adsSensSlider.value = SettingsManager.ADSSensitivity;
            if (adsSensValueText != null) adsSensValueText.text = $"{SettingsManager.ADSSensitivity:F2}";
            if (invertYToggle != null) invertYToggle.isOn = SettingsManager.InvertY;

            SetupKeybindRows();

            _isUpdatingUI = false;
        }
        #endregion
    }
}
