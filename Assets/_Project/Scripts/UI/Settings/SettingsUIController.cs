using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PewPewArena.UI;

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
        [SerializeField] private Color activeTabBgColor = new Color(1f, 0.80f, 0.12f, 1f); // Cyber Gold #FFCC1F
        [Tooltip("Background color of inactive sidebar buttons")]
        [SerializeField] private Color inactiveTabBgColor = new Color(0.086f, 0.098f, 0.145f, 0.94f); // Tactical Slate #161925
        [Tooltip("Background color of hovered inactive sidebar buttons")]
        [SerializeField] private Color hoverTabBgColor = new Color(0.137f, 0.157f, 0.231f, 1f); // Elevated Slate #23283B
        [Tooltip("Text color of the currently active sidebar button")]
        [SerializeField] private Color activeTabTextColor = new Color(0.06f, 0.06f, 0.08f, 1f); // Crisp Deep Black #0F0F14
        [Tooltip("Text color of inactive sidebar buttons")]
        [SerializeField] private Color inactiveTabTextColor = new Color(0.78f, 0.81f, 0.89f, 1f); // Muted Ice White #C7CFE3
        [Tooltip("Text color of hovered inactive sidebar buttons")]
        [SerializeField] private Color hoverTabTextColor = new Color(1f, 0.90f, 0.40f, 1f); // Warm Gold #FFE666
        [Tooltip("Horizontal micro-nudge distance for the active tab towards the panel")]
        [SerializeField] private float tabNudgeDistance = 8f;

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
        private Vector2[] _panelBasePositions;
        private Button[] _tabButtons;
        private int _currentTabIndex = 0;
        private bool _isUpdatingUI = false;
        private Coroutine _fadeRoutine;
        private Coroutine _keybindsFadeRoutine;
        private Coroutine _entranceRoutine;
        private Coroutine _exitRoutine;
        private Coroutine _subtitleRoutine;

        private RectTransform _headerDividerRt;
        private TextMeshProUGUI _categorySubtitleText;
        private Vector2 _keybindsBasePos;
        private bool _keybindsPosCaptured;

        private static readonly string[] CategorySubtitles = new string[]
        {
            "GENERAL  //  DISPLAY & PERFORMANCE",
            "GAME  //  HUD & COMBAT FEEDBACK",
            "GRAPHICS  //  RENDERING & FIELD OF VIEW",
            "AUDIO  //  VOLUME MIXER & CHANNELS",
            "CONTROLS  //  AIMING & KEYBINDS"
        };

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

            SanitizeSerializedTabColors();
            AutoResolveReferences();
            RegisterUIEvents();
            ApplyVisualTheme();
        }

        private void Start()
        {
            ApplyVisualTheme();
            ShowTabInstant(_currentTabIndex);
            RefreshAllUI();
        }

        private void OnEnable()
        {
            SanitizeSerializedTabColors();
            AutoResolveReferences();
            RegisterUIEvents();
            ApplyVisualTheme();
            ShowTabInstant(_currentTabIndex);
            RefreshAllUI();
            ResetAllButtonEffects();

            if (_exitRoutine != null)
            {
                StopCoroutine(_exitRoutine);
                _exitRoutine = null;
            }
            if (_entranceRoutine != null)
            {
                StopCoroutine(_entranceRoutine);
            }
            _entranceRoutine = StartCoroutine(PlayEntranceAnimationRoutine());
        }

        private void SanitizeSerializedTabColors()
        {
            // Upgrade legacy pure-white serialized tab colors to Dark Tactical Slate
            if (inactiveTabBgColor.r > 0.85f && inactiveTabBgColor.g > 0.85f && inactiveTabBgColor.b > 0.85f)
            {
                inactiveTabBgColor = new Color(0.086f, 0.098f, 0.145f, 0.94f);
            }
            if (inactiveTabTextColor.r < 0.35f && inactiveTabTextColor.g < 0.35f && inactiveTabTextColor.b < 0.35f)
            {
                inactiveTabTextColor = new Color(0.78f, 0.81f, 0.89f, 1f);
            }
            if (tabNudgeDistance < 7f)
            {
                tabNudgeDistance = 8f;
            }
        }

        #region Auto-Resolution
        private void AutoResolveReferences()
        {
            Transform settingsPanelTr = transform;

            // 0. Clean up stray red image in BackImageImprovement if present
            var backImprove = FindChildRelaxed(settingsPanelTr, "BackImageImprovement");
            if (backImprove != null)
            {
                for (int i = 0; i < backImprove.transform.childCount; i++)
                {
                    var child = backImprove.transform.GetChild(i);
                    if (child.name == "Image")
                    {
                        var redImg = child.GetComponent<Image>();
                        if (redImg != null && redImg.color.r > 0.8f && redImg.color.g < 0.2f && redImg.color.b < 0.2f)
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // 1. Sidebar Buttons
            if (generalTabBtn == null) generalTabBtn = FindButton(settingsPanelTr, "General");
            if (gameTabBtn == null) gameTabBtn = FindButton(settingsPanelTr, "Game");
            if (graphicsTabBtn == null) graphicsTabBtn = FindButton(settingsPanelTr, "Graphics");
            if (audioTabBtn == null) audioTabBtn = FindButton(settingsPanelTr, "Audio");
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
                    hoverItem.ConfigurePalette(
                        activeTabBgColor,
                        inactiveTabBgColor,
                        hoverTabBgColor,
                        activeTabTextColor,
                        inactiveTabTextColor,
                        hoverTabTextColor,
                        tabNudgeDistance);
                }
            }

            // 2. Panels
            if (generalPanel == null) generalPanel = FindChildRelaxed(settingsPanelTr, "General Panel");
            if (gamePanel == null) gamePanel = FindChildRelaxed(settingsPanelTr, "Game Panel");
            if (graphicsPanel == null) graphicsPanel = FindChildRelaxed(settingsPanelTr, "Graphics Panel");
            if (audioPanel == null) audioPanel = FindChildRelaxed(settingsPanelTr, "Audio Panel");
            if (controlsPanel == null) controlsPanel = FindChildRelaxed(settingsPanelTr, "Controls Panel");

            _panels = new GameObject[] { generalPanel, gamePanel, graphicsPanel, audioPanel, controlsPanel };
            if (_panelBasePositions == null || _panelBasePositions.Length != _panels.Length)
            {
                _panelBasePositions = new Vector2[_panels.Length];
                for (int i = 0; i < _panels.Length; i++)
                {
                    if (_panels[i] != null)
                    {
                        var rt = _panels[i].GetComponent<RectTransform>();
                        if (rt != null) _panelBasePositions[i] = rt.anchoredPosition;
                    }
                }
            }

            // Ensure panel containers never block clicks
            foreach (var p in _panels)
            {
                if (p != null)
                {
                    var img = p.GetComponent<Image>();
                    if (img != null) img.raycastTarget = false;
                }
            }

            // 3. Settings Back Button
            var backBtnGo = FindChildRelaxed(settingsPanelTr, "SettingsBackButton");
            if (backBtnGo != null)
            {
                backBtnGo.transform.SetAsLastSibling();
                var btn = backBtnGo.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnSettingsBackButtonClicked);
                }
            }

            // 4. Keybinds side panel & buttons
            if (keybindsSidePanel == null)
            {
                keybindsSidePanel = FindChildRelaxed(settingsPanelTr, "KeybindsSidePanel");
                if (keybindsSidePanel == null && controlsPanel != null)
                    keybindsSidePanel = FindChildRelaxed(controlsPanel.transform, "KeybindsSidePanel");
            }

            if (keybindsSidePanel != null && !_keybindsPosCaptured)
            {
                var kbRt = keybindsSidePanel.GetComponent<RectTransform>();
                if (kbRt != null)
                {
                    _keybindsBasePos = kbRt.anchoredPosition;
                    _keybindsPosCaptured = true;
                }
            }

            if (keybindsOpenBtn == null)
            {
                keybindsOpenBtn = FindControlByName<Button>(controlsPanel != null ? controlsPanel.transform : settingsPanelTr, "KeybindsButton");
                if (keybindsOpenBtn == null && controlsPanel != null)
                    keybindsOpenBtn = controlsPanel.GetComponentInChildren<Button>(true);
            }

            if (keybindsCloseBtn == null && keybindsSidePanel != null)
            {
                keybindsCloseBtn = FindControlByName<Button>(keybindsSidePanel.transform, "KeybindsCloseButton");
                if (keybindsCloseBtn == null)
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
            }

            // 5. General Elements
            if (generalPanel != null)
            {
                var dropdowns = generalPanel.GetComponentsInChildren<TMP_Dropdown>(true);
                foreach (var dd in dropdowns)
                {
                    if (dd.name.IndexOf("fps", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (dd.options.Count > 0 && dd.options[0].text.IndexOf("fps", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        fpsLimitDropdown = dd;
                    }
                    else if ((dd.options.Count > 0 && (dd.options[0].text.Contains("720") || dd.options[0].text.Contains("1080"))) ||
                             dd.name.IndexOf("resolution", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        resolutionDropdown = dd;
                    }
                    else if ((dd.options.Count > 0 && dd.options[0].text.IndexOf("fullscreen", System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                             dd.name.IndexOf("displaymode", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        displayModeDropdown = dd;
                    }
                }

                showFpsToggle = FindControlByName<Toggle>(generalPanel.transform, "ShowFPSToggle");
                resetSettingsBtn = FindControlByName<Button>(generalPanel.transform, "ResetSettingsButton");
            }

            // 6. Game Elements
            if (gamePanel != null)
            {
                crosshairToggle = FindControlByName<Toggle>(gamePanel.transform, "CrosshairToggle");
                hitmarkerToggle = FindControlByName<Toggle>(gamePanel.transform, "HitmarkerToggle");
                damageNumbersToggle = FindControlByName<Toggle>(gamePanel.transform, "DamageNumbersToggle");
            }

            // 7. Graphics Elements
            if (graphicsPanel != null)
            {
                qualityDropdown = FindControlByName<TMP_Dropdown>(graphicsPanel.transform, "GraphicsQualityDropdown");
                if (qualityDropdown == null) qualityDropdown = graphicsPanel.GetComponentInChildren<TMP_Dropdown>(true);

                vsyncToggle = FindControlByName<Toggle>(graphicsPanel.transform, "VSyncToggle");
                shadowsToggle = FindControlByName<Toggle>(graphicsPanel.transform, "ShadowsToggle");
                antiAliasingToggle = FindControlByName<Toggle>(graphicsPanel.transform, "AntiAliasingToggle");

                fovSlider = FindControlByName<Slider>(graphicsPanel.transform, "FOVSlider");
                viewDistanceSlider = FindControlByName<Slider>(graphicsPanel.transform, "ViewDistanceSlider");

                fovValueText = FindControlByName<TextMeshProUGUI>(graphicsPanel.transform, "FOVValueText");
                viewDistanceValueText = FindControlByName<TextMeshProUGUI>(graphicsPanel.transform, "ViewDistanceValueText");
            }

            // 8. Audio Elements
            if (audioPanel != null)
            {
                masterVolSlider = FindControlByName<Slider>(audioPanel.transform, "MasterVolumeSlider");
                sfxVolSlider = FindControlByName<Slider>(audioPanel.transform, "SFXVolumeSlider");
                musicVolSlider = FindControlByName<Slider>(audioPanel.transform, "MusicVolumeSlider");
                voiceVolSlider = FindControlByName<Slider>(audioPanel.transform, "VoiceVolumeSlider");

                masterVolValueText = FindControlByName<TextMeshProUGUI>(audioPanel.transform, "MasterVolumeValueText");
                sfxVolValueText = FindControlByName<TextMeshProUGUI>(audioPanel.transform, "SFXVolumeValueText");
                musicVolValueText = FindControlByName<TextMeshProUGUI>(audioPanel.transform, "MusicVolumeValueText");
                voiceVolValueText = FindControlByName<TextMeshProUGUI>(audioPanel.transform, "VoiceVolumeValueText");
            }

            // 9. Controls Elements
            if (controlsPanel != null)
            {
                mouseSensSlider = FindControlByName<Slider>(controlsPanel.transform, "MouseSensitivitySlider");
                adsSensSlider = FindControlByName<Slider>(controlsPanel.transform, "ADSSensitivitySlider");
                invertYToggle = FindControlByName<Toggle>(controlsPanel.transform, "InvertYToggle");

                mouseSensValueText = FindControlByName<TextMeshProUGUI>(controlsPanel.transform, "MouseSensitivityValueText");
                adsSensValueText = FindControlByName<TextMeshProUGUI>(controlsPanel.transform, "ADSSensitivityValueText");
            }

            EnsureHeaderAccents();
            SetupKeybindRows();
        }

        private void EnsureHeaderAccents()
        {
            // Find the main SETTINGS header text
            TextMeshProUGUI headerText = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Text (TMP)")
                {
                    var tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp != null && tmp.text.IndexOf("SETTINGS", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        headerText = tmp;
                        break;
                    }
                }
            }

            if (headerText == null) return;
            headerText.color = new Color(0.96f, 0.97f, 1f, 1f);

            var headerRt = headerText.GetComponent<RectTransform>();
            if (headerRt == null) return;

            // Create or find Header Divider Line
            var divTr = transform.Find("SettingsHeaderDivider");
            if (divTr == null)
            {
                var divGo = new GameObject("SettingsHeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                divGo.transform.SetParent(transform, false);
                divTr = divGo.transform;
                var divRt = divGo.GetComponent<RectTransform>();
                divRt.anchorMin = headerRt.anchorMin;
                divRt.anchorMax = headerRt.anchorMax;
                divRt.pivot = new Vector2(0.5f, 0.5f);
                divRt.anchoredPosition = new Vector2(headerRt.anchoredPosition.x + 95f, headerRt.anchoredPosition.y - 26f);
                divRt.sizeDelta = new Vector2(460f, 2f);

                var divImg = divGo.GetComponent<Image>();
                divImg.color = new Color(1f, 0.80f, 0.12f, 0.65f);
                divImg.raycastTarget = false;
            }
            _headerDividerRt = divTr.GetComponent<RectTransform>();

            // Create or find Category Subtitle
            var subTr = transform.Find("SettingsCategorySubtitle");
            if (subTr == null)
            {
                var subGo = new GameObject("SettingsCategorySubtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                subGo.transform.SetParent(transform, false);
                subTr = subGo.transform;
                var subRt = subGo.GetComponent<RectTransform>();
                subRt.anchorMin = headerRt.anchorMin;
                subRt.anchorMax = headerRt.anchorMax;
                subRt.pivot = new Vector2(0f, 0.5f);
                subRt.anchoredPosition = new Vector2(headerRt.anchoredPosition.x - 100f, headerRt.anchoredPosition.y - 40f);
                subRt.sizeDelta = new Vector2(450f, 22f);

                var subTmp = subGo.GetComponent<TextMeshProUGUI>();
                subTmp.font = headerText.font;
                subTmp.fontSharedMaterial = headerText.fontSharedMaterial;
                subTmp.fontSize = 12.5f;
                subTmp.fontStyle = FontStyles.Bold;
                subTmp.characterSpacing = 3.5f;
                subTmp.color = new Color(1f, 0.82f, 0.18f, 0.88f);
                subTmp.alignment = TextAlignmentOptions.MidlineLeft;
                subTmp.raycastTarget = false;
                subTmp.text = CategorySubtitles[Mathf.Clamp(_currentTabIndex, 0, CategorySubtitles.Length - 1)];
            }
            _categorySubtitleText = subTr.GetComponent<TextMeshProUGUI>();
        }

        private T FindControlByName<T>(Transform parent, string partialName) where T : Component
        {
            if (parent == null) return null;
            string target = partialName.Replace(" ", "").ToLowerInvariant();
            T[] components = parent.GetComponentsInChildren<T>(true);
            foreach (var c in components)
            {
                // Never match dropdown template items
                if (c.name.Equals("Item", System.StringComparison.OrdinalIgnoreCase) ||
                    (c.transform.parent != null && c.transform.parent.name.Equals("Content", System.StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string cName = c.name.Replace(" ", "").ToLowerInvariant();
                if (cName.Contains(target))
                {
                    return c;
                }
            }
            return null;
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
            string sliderRootName = slider.name.Replace("Slider", "").Replace(" ", "").ToLowerInvariant();
            var texts = slider.transform.parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                string tName = t.name.Replace(" ", "").ToLowerInvariant();
                if (tName.Contains(sliderRootName) && tName.Contains("value"))
                {
                    return t;
                }
            }
            foreach (var t in texts)
            {
                if (t.name.ToLowerInvariant().Contains("value") || t.name.ToLowerInvariant().Contains("text"))
                {
                    if (Mathf.Abs(t.rectTransform.anchoredPosition.y - slider.GetComponent<RectTransform>().anchoredPosition.y) < 25f)
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
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(PlayResetConfirmationFlashRoutine());
                    }
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
                    PulseSliderValueText(fovSlider);
                });
            }

            if (viewDistanceSlider != null)
            {
                viewDistanceSlider.onValueChanged.RemoveAllListeners();
                viewDistanceSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetViewDistance(val);
                    if (viewDistanceValueText != null) viewDistanceValueText.text = $"{Mathf.RoundToInt(val)}m";
                    PulseSliderValueText(viewDistanceSlider);
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
                    PulseSliderValueText(masterVolSlider, playAudioTick: !_isUpdatingUI);
                });
            }

            if (sfxVolSlider != null)
            {
                sfxVolSlider.onValueChanged.RemoveAllListeners();
                sfxVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetSfxVolume(val);
                    if (sfxVolValueText != null) sfxVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                    PulseSliderValueText(sfxVolSlider, playAudioTick: !_isUpdatingUI);
                });
            }

            if (musicVolSlider != null)
            {
                musicVolSlider.onValueChanged.RemoveAllListeners();
                musicVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetMusicVolume(val);
                    if (musicVolValueText != null) musicVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                    PulseSliderValueText(musicVolSlider);
                });
            }

            if (voiceVolSlider != null)
            {
                voiceVolSlider.onValueChanged.RemoveAllListeners();
                voiceVolSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetVoiceVolume(val);
                    if (voiceVolValueText != null) voiceVolValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
                    PulseSliderValueText(voiceVolSlider);
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
                    PulseSliderValueText(mouseSensSlider);
                });
            }

            if (adsSensSlider != null)
            {
                adsSensSlider.onValueChanged.RemoveAllListeners();
                adsSensSlider.onValueChanged.AddListener((val) =>
                {
                    if (!_isUpdatingUI) SettingsManager.SetADSSensitivity(val);
                    if (adsSensValueText != null) adsSensValueText.text = $"{val:F2}x";
                    PulseSliderValueText(adsSensSlider);
                });
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.RemoveAllListeners();
                invertYToggle.onValueChanged.AddListener((val) => { if (!_isUpdatingUI) SettingsManager.SetInvertY(val); });
            }
        }

        private void PulseSliderValueText(Slider s, bool playAudioTick = false)
        {
            if (_isUpdatingUI || s == null) return;
            var hover = s.GetComponent<SettingsSliderHoverItem>();
            if (hover != null)
            {
                hover.TriggerValuePop(playAudioTick);
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

                var kbHover = row.GetComponent<KeybindRowHoverItem>();
                if (kbHover == null) kbHover = row.AddComponent<KeybindRowHoverItem>();
                kbHover.Init(item.action != KeyAction.Move);
            }
        }
        #endregion

        #region Panel Entrance, Exit & Tab Navigation
        private IEnumerator PlayEntranceAnimationRoutine()
        {
            var rootCg = GetComponent<CanvasGroup>();
            if (rootCg == null) rootCg = gameObject.AddComponent<CanvasGroup>();

            var rootRt = GetComponent<RectTransform>();
            rootCg.alpha = 0f;
            if (rootRt != null) rootRt.localScale = new Vector3(0.97f, 0.97f, 1f);

            if (_headerDividerRt != null)
            {
                _headerDividerRt.localScale = new Vector3(0f, 1f, 1f);
            }

            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                rootCg.alpha = easeOut;
                if (rootRt != null)
                {
                    float s = Mathf.Lerp(0.97f, 1.0f, easeOut);
                    rootRt.localScale = new Vector3(s, s, 1f);
                }
                if (_headerDividerRt != null)
                {
                    _headerDividerRt.localScale = new Vector3(easeOut, 1f, 1f);
                }

                yield return null;
            }

            rootCg.alpha = 1f;
            if (rootRt != null) rootRt.localScale = Vector3.one;
            if (_headerDividerRt != null) _headerDividerRt.localScale = Vector3.one;
            _entranceRoutine = null;
        }

        private void OnSettingsBackButtonClicked()
        {
            if (!gameObject.activeInHierarchy)
            {
                ExecuteCloseNavigation();
                return;
            }

            if (_exitRoutine != null) StopCoroutine(_exitRoutine);
            _exitRoutine = StartCoroutine(CloseSettingsWithFadeRoutine());
        }

        private IEnumerator CloseSettingsWithFadeRoutine()
        {
            var rootCg = GetComponent<CanvasGroup>();
            if (rootCg == null) rootCg = gameObject.AddComponent<CanvasGroup>();
            var rootRt = GetComponent<RectTransform>();

            float duration = 0.12f;
            float elapsed = 0f;
            float startAlpha = rootCg.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeIn = t * t;

                rootCg.alpha = Mathf.Lerp(startAlpha, 0f, easeIn);
                if (rootRt != null)
                {
                    float s = Mathf.Lerp(1.0f, 0.975f, easeIn);
                    rootRt.localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }

            rootCg.alpha = 1f;
            if (rootRt != null) rootRt.localScale = Vector3.one;
            _exitRoutine = null;

            ExecuteCloseNavigation();
        }

        private void ExecuteCloseNavigation()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.gameObject.activeInHierarchy)
            {
                PauseMenu.Instance.BackToMainPause();
            }
            else
            {
                var relayUI = FindAnyObjectByType<RelayNetworkUI>();
                if (relayUI != null)
                {
                    relayUI.OnSettingsBackClicked();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator PlayResetConfirmationFlashRoutine()
        {
            if (_panels == null || _currentTabIndex < 0 || _currentTabIndex >= _panels.Length) yield break;
            var activePanel = _panels[_currentTabIndex];
            if (activePanel == null) yield break;

            var img = activePanel.GetComponent<Image>();
            if (img == null) yield break;

            Color baseCol = new Color(0.063f, 0.071f, 0.106f, 0.85f);
            Color flashCol = new Color(0.22f, 0.19f, 0.08f, 0.92f);

            float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                img.color = Color.Lerp(flashCol, baseCol, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }
            img.color = baseCol;
        }

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

                    var rt = _panels[i].GetComponent<RectTransform>();
                    if (rt != null && _panelBasePositions != null && i < _panelBasePositions.Length)
                    {
                        rt.anchoredPosition = _panelBasePositions[i];
                    }

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
            UpdateCategorySubtitle(_currentTabIndex, instant: true);

            var backBtnGo = FindChildRelaxed(transform, "SettingsBackButton");
            if (backBtnGo != null) backBtnGo.transform.SetAsLastSibling();

            if (_currentTabIndex != 4 && keybindsSidePanel != null)
            {
                keybindsSidePanel.SetActive(false);
            }

            ResetAllButtonEffects();
        }

        private IEnumerator CrossFadePanelRoutine(int targetIndex)
        {
            if (_panels == null || targetIndex < 0 || targetIndex >= _panels.Length) yield break;

            int prevIndex = _currentTabIndex;
            float slideDir = (targetIndex >= prevIndex) ? 1f : -1f;
            _currentTabIndex = targetIndex;

            UpdateTabButtonVisuals();
            UpdateCategorySubtitle(_currentTabIndex, instant: false);

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
            var inRt = incomingPanel.GetComponent<RectTransform>();
            Vector2 inBasePos = (_panelBasePositions != null && targetIndex < _panelBasePositions.Length)
                ? _panelBasePositions[targetIndex]
                : (inRt != null ? inRt.anchoredPosition : Vector2.zero);

            CanvasGroup outCg = null;
            RectTransform outRt = null;
            Vector2 outBasePos = Vector2.zero;
            if (outgoingPanel != null)
            {
                outCg = outgoingPanel.GetComponent<CanvasGroup>();
                if (outCg == null) outCg = outgoingPanel.AddComponent<CanvasGroup>();
                outRt = outgoingPanel.GetComponent<RectTransform>();
                outBasePos = (_panelBasePositions != null && prevIndex < _panelBasePositions.Length)
                    ? _panelBasePositions[prevIndex]
                    : (outRt != null ? outRt.anchoredPosition : Vector2.zero);
                outCg.interactable = false;
                outCg.blocksRaycasts = false;
            }

            incomingPanel.SetActive(true);
            var inImg = incomingPanel.GetComponent<Image>();
            if (inImg != null) inImg.raycastTarget = false;

            inCg.alpha = 0f;
            inCg.interactable = false;
            inCg.blocksRaycasts = false;

            float slideOffset = 14f;
            if (inRt != null)
            {
                inRt.anchoredPosition = inBasePos + new Vector2(0f, -slideDir * slideOffset);
            }

            float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOut = 1f - Mathf.Pow(1f - t, 3f);

                inCg.alpha = easeOut;
                if (inRt != null)
                {
                    inRt.anchoredPosition = Vector2.Lerp(inBasePos + new Vector2(0f, -slideDir * slideOffset), inBasePos, easeOut);
                }

                if (outCg != null)
                {
                    outCg.alpha = 1f - easeOut;
                }
                if (outRt != null)
                {
                    outRt.anchoredPosition = Vector2.Lerp(outBasePos, outBasePos + new Vector2(0f, slideDir * slideOffset), easeOut);
                }

                yield return null;
            }

            inCg.alpha = 1f;
            inCg.interactable = true;
            inCg.blocksRaycasts = true;
            if (inRt != null) inRt.anchoredPosition = inBasePos;

            // Deactivate other panels and restore their base positions
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
                    var rt = _panels[i].GetComponent<RectTransform>();
                    if (rt != null && _panelBasePositions != null && i < _panelBasePositions.Length)
                    {
                        rt.anchoredPosition = _panelBasePositions[i];
                    }
                }
            }

            _fadeRoutine = null;
        }

        private void UpdateCategorySubtitle(int tabIndex, bool instant)
        {
            if (_categorySubtitleText == null) return;
            string targetText = CategorySubtitles[Mathf.Clamp(tabIndex, 0, CategorySubtitles.Length - 1)];

            if (instant || !gameObject.activeInHierarchy)
            {
                _categorySubtitleText.text = targetText;
                _categorySubtitleText.alpha = 0.88f;
                return;
            }

            if (_subtitleRoutine != null) StopCoroutine(_subtitleRoutine);
            _subtitleRoutine = StartCoroutine(AnimateSubtitleRoutine(targetText));
        }

        private IEnumerator AnimateSubtitleRoutine(string targetText)
        {
            if (_categorySubtitleText == null) yield break;
            _categorySubtitleText.text = targetText;
            _categorySubtitleText.alpha = 0f;

            float duration = 0.16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _categorySubtitleText.alpha = Mathf.Lerp(0f, 0.88f, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }
            _categorySubtitleText.alpha = 0.88f;
            _subtitleRoutine = null;
        }

        private void UpdateTabButtonVisuals()
        {
            if (_tabButtons == null) return;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;

                bool isActive = (i == _currentTabIndex);

                // Keep Button transition disabled or neutral so TabButtonHoverItem smoothly drives colors
                _tabButtons[i].transition = Selectable.Transition.None;

                var hoverItem = _tabButtons[i].GetComponent<TabButtonHoverItem>();
                if (hoverItem == null)
                {
                    hoverItem = _tabButtons[i].gameObject.AddComponent<TabButtonHoverItem>();
                    hoverItem.Init();
                }

                hoverItem.ConfigurePalette(
                    activeTabBgColor,
                    inactiveTabBgColor,
                    hoverTabBgColor,
                    activeTabTextColor,
                    inactiveTabTextColor,
                    hoverTabTextColor,
                    tabNudgeDistance);

                hoverItem.SetActiveTab(isActive, instant: !gameObject.activeInHierarchy);
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
            var rt = keybindsSidePanel.GetComponent<RectTransform>();

            if (!_keybindsPosCaptured && rt != null)
            {
                _keybindsBasePos = rt.anchoredPosition;
                _keybindsPosCaptured = true;
            }

            Vector2 closedPos = _keybindsBasePos + new Vector2(-24f, 0f);
            Vector2 openPos = _keybindsBasePos;

            if (show)
            {
                keybindsSidePanel.SetActive(true);
                SetupKeybindRows();
                cg.alpha = 0f;
                if (rt != null) rt.anchoredPosition = closedPos;
            }

            float duration = 0.16f;
            float elapsed = 0f;
            float startAlpha = cg.alpha;
            float targetAlpha = show ? 1f : 0f;
            Vector2 startPos = rt != null ? rt.anchoredPosition : openPos;
            Vector2 targetPos = show ? openPos : closedPos;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, ease);
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                }
                yield return null;
            }

            cg.alpha = targetAlpha;
            cg.interactable = show;
            cg.blocksRaycasts = show;
            if (rt != null) rt.anchoredPosition = openPos;

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
            if (displayModeDropdown != null) { displayModeDropdown.value = SettingsManager.DisplayModeIndex; displayModeDropdown.RefreshShownValue(); }
            if (resolutionDropdown != null) { resolutionDropdown.value = SettingsManager.ResolutionIndex; resolutionDropdown.RefreshShownValue(); }
            if (fpsLimitDropdown != null) { fpsLimitDropdown.value = SettingsManager.FpsLimitIndex; fpsLimitDropdown.RefreshShownValue(); }
            if (showFpsToggle != null) showFpsToggle.isOn = SettingsManager.ShowFps;

            // Game
            if (crosshairToggle != null) crosshairToggle.isOn = SettingsManager.ShowCrosshair;
            if (hitmarkerToggle != null) hitmarkerToggle.isOn = SettingsManager.ShowHitmarkers;
            if (damageNumbersToggle != null) damageNumbersToggle.isOn = SettingsManager.ShowDamageNumbers;

            // Graphics
            if (qualityDropdown != null) { qualityDropdown.value = SettingsManager.QualityIndex; qualityDropdown.RefreshShownValue(); }
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
            Color rowTitleIdle = new Color(0.81f, 0.84f, 0.90f, 1f);          // #CFD6E6

            // 1. Panels Backgrounds & Setting Row Highlighters
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
                    SetupPanelRowHighlights(p.transform, rowTitleIdle, cyberGoldHover, cyberGoldAccent);
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

            // 6. Polish ResetSettingsButton
            StyleResetSettingsButton(cyberGoldAccent, textPrimary);

            // 7. Ensure SettingsBackButton and KeybindsButton are initialized with standard button styling
            var backBtnGo = FindChildRelaxed(transform, "SettingsBackButton");
            if (backBtnGo != null)
            {
                var bEffect = backBtnGo.GetComponent<MainMenuButtonEffect>();
                if (bEffect != null) bEffect.InitializeComponents();
            }
            if (keybindsOpenBtn != null)
            {
                var kEffect = keybindsOpenBtn.GetComponent<MainMenuButtonEffect>();
                if (kEffect != null) kEffect.InitializeComponents();
            }
        }

        private void SetupPanelRowHighlights(Transform panelTr, Color idleTitleCol, Color hoverTitleCol, Color goldAccent)
        {
            if (panelTr == null) return;

            // Collect candidate row title labels (direct children of panel that are TextMeshProUGUI and not ValueText)
            var titleTexts = new List<TextMeshProUGUI>();
            for (int i = 0; i < panelTr.childCount; i++)
            {
                var child = panelTr.GetChild(i);
                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null) continue;
                string lowerName = child.name.ToLowerInvariant();
                if (lowerName.Contains("value")) continue;
                titleTexts.Add(tmp);
            }

            // Collect interactive controls that are direct children of panel
            for (int i = 0; i < panelTr.childCount; i++)
            {
                var child = panelTr.GetChild(i);
                if (child.name == "KeybindsSidePanel" || child.name.StartsWith("RowHighlight_")) continue;

                Selectable sel = child.GetComponent<Selectable>();
                if (sel == null) continue;
                if (child.name == "ResetSettingsButton" || child.name == "KeybindsButton") continue;

                var ctrlRt = child.GetComponent<RectTransform>();
                if (ctrlRt == null) continue;

                // Find matching title text on the same horizontal row
                TextMeshProUGUI matchedTitle = null;
                float bestDiff = 32f;
                foreach (var t in titleTexts)
                {
                    float diff = Mathf.Abs(t.rectTransform.anchoredPosition.y - ctrlRt.anchoredPosition.y);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        matchedTitle = t;
                    }
                }

                if (matchedTitle != null)
                {
                    matchedTitle.color = idleTitleCol;
                    var rowItem = child.GetComponent<SettingsRowHighlightItem>();
                    if (rowItem == null) rowItem = child.gameObject.AddComponent<SettingsRowHighlightItem>();
                    rowItem.Init(panelTr, matchedTitle, ctrlRt, idleTitleCol, hoverTitleCol, goldAccent);
                }
            }
        }

        private void StyleResetSettingsButton(Color goldAccent, Color textPrimary)
        {
            if (resetSettingsBtn == null) return;

            var rt = resetSettingsBtn.GetComponent<RectTransform>();
            if (rt != null && rt.sizeDelta.x < 280f)
            {
                rt.sizeDelta = new Vector2(280f, 50f);
            }

            resetSettingsBtn.transition = Selectable.Transition.None;

            var tmp = resetSettingsBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                if (tmp.text.IndexOf("Reset", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    tmp.text = "RESET DEFAULTS";
                }
                tmp.color = textPrimary;
                tmp.fontStyle = FontStyles.Bold;
                tmp.characterSpacing = 3f;
                if (tmp.fontSize > 24f || tmp.fontSize < 18f) tmp.fontSize = 21f;
            }

            var effect = resetSettingsBtn.GetComponent<MainMenuButtonEffect>();
            if (effect == null)
            {
                effect = resetSettingsBtn.gameObject.AddComponent<MainMenuButtonEffect>();
            }
            effect.InitializeComponents();
        }

        private void ResetAllButtonEffects()
        {
            var btnEffects = GetComponentsInChildren<MainMenuButtonEffect>(true);
            foreach (var effect in btnEffects)
            {
                effect.ApplyInstantIdleState();
            }
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

                var ddHover = dd.GetComponent<SettingsDropdownHoverItem>();
                if (ddHover == null) ddHover = dd.gameObject.AddComponent<SettingsDropdownHoverItem>();
                ddHover.Init(arrow != null ? arrow.rectTransform : null);

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
                Image fillImg = null;
                if (s.fillRect != null)
                {
                    fillImg = s.fillRect.GetComponent<Image>();
                    if (fillImg != null) fillImg.color = goldAccent;
                }

                // Handle Area
                Image handleImg = null;
                if (s.handleRect != null)
                {
                    handleImg = s.handleRect.GetComponent<Image>();
                    if (handleImg != null) handleImg.color = Color.white;
                }

                // Value Text next to slider
                var valText = FindValueTextNextTo(s);
                if (valText != null)
                {
                    valText.color = goldAccent;
                    valText.fontStyle = FontStyles.Bold;
                }

                var sHover = s.GetComponent<SettingsSliderHoverItem>();
                if (sHover == null) sHover = s.gameObject.AddComponent<SettingsSliderHoverItem>();
                sHover.Init(s.handleRect, handleImg, fillImg, valText, goldAccent, goldHover);
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

                Image checkImg = tog.graphic as Image;
                if (checkImg != null)
                {
                    checkImg.color = goldAccent;
                }

                var label = tog.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.color = textCol;
                }

                var tHover = tog.GetComponent<SettingsToggleHoverItem>();
                if (tHover == null) tHover = tog.gameObject.AddComponent<SettingsToggleHoverItem>();
                tHover.Init(tog, bg, checkImg, boxBg, boxHover, goldAccent);
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
    /// Smoothly animates sidebar tab buttons with Dark Tactical Slate idle state,
    /// hover elevation, active Cyber Gold highlight, left accent bar, and unscaled interpolation.
    /// </summary>
    public class TabButtonHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rect;
        private Image _bgImage;
        private TextMeshProUGUI _label;
        private RectTransform _accentBarRt;
        private Image _accentBarImg;

        private Vector2 _basePos;
        private bool _isInitialized;
        private bool _isActive;
        private bool _isHovered;
        private bool _isPressed;

        private Color _activeBg = new Color(1f, 0.80f, 0.12f, 1f);
        private Color _inactiveBg = new Color(0.086f, 0.098f, 0.145f, 0.94f);
        private Color _hoverBg = new Color(0.137f, 0.157f, 0.231f, 1f);
        private Color _activeText = new Color(0.06f, 0.06f, 0.08f, 1f);
        private Color _inactiveText = new Color(0.78f, 0.81f, 0.89f, 1f);
        private Color _hoverText = new Color(1f, 0.90f, 0.40f, 1f);
        private float _nudgeDistance = 8f;

        public void Init()
        {
            if (_isInitialized) return;
            _rect = GetComponent<RectTransform>();
            _bgImage = GetComponent<Image>();
            _label = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_rect != null)
            {
                _basePos = _rect.anchoredPosition;
                _isInitialized = true;

                // Ensure left accent bar exists
                var barTr = transform.Find("TabAccentBar");
                if (barTr == null)
                {
                    var barGo = new GameObject("TabAccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    barGo.transform.SetParent(transform, false);
                    barTr = barGo.transform;
                    var rt = barGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(4.5f, 0f);
                    rt.localScale = new Vector3(1f, 0f, 1f);

                    var img = barGo.GetComponent<Image>();
                    img.color = _activeBg;
                    img.raycastTarget = false;
                }
                _accentBarRt = barTr.GetComponent<RectTransform>();
                _accentBarImg = barTr.GetComponent<Image>();
            }
        }

        public void ConfigurePalette(
            Color activeBg,
            Color inactiveBg,
            Color hoverBg,
            Color activeText,
            Color inactiveText,
            Color hoverText,
            float nudgeDistance)
        {
            _activeBg = activeBg;
            _inactiveBg = inactiveBg;
            _hoverBg = hoverBg;
            _activeText = activeText;
            _inactiveText = inactiveText;
            _hoverText = hoverText;
            _nudgeDistance = nudgeDistance;
        }

        public void SetActiveTab(bool active, float nudgeDistance)
        {
            _nudgeDistance = nudgeDistance;
            SetActiveTab(active, instant: false);
        }

        public void SetActiveTab(bool active, bool instant)
        {
            Init();
            _isActive = active;
            if (_label != null)
            {
                _label.fontStyle = FontStyles.Bold;
            }
            if (instant)
            {
                ApplyStateImmediate();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Init();
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Init();
            _isHovered = false;
            _isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }

        private void Update()
        {
            if (!_isInitialized || _rect == null) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 16f;

            float targetOffset = _isActive ? _nudgeDistance : (_isHovered ? 5f : 0f);
            float targetScale = _isPressed ? 0.97f : (_isActive ? 1.035f : (_isHovered ? 1.02f : 1.0f));
            Color targetBg = _isActive ? _activeBg : (_isHovered ? _hoverBg : _inactiveBg);
            Color targetText = _isActive ? _activeText : (_isHovered ? _hoverText : _inactiveText);
            float targetSpacing = _isActive ? 3.5f : (_isHovered ? 3.0f : 2.0f);
            float targetBarScaleY = _isActive ? 1.0f : (_isHovered ? 0.68f : 0f);
            Color targetBarCol = _isActive ? new Color(0.08f, 0.08f, 0.10f, 0.85f) : _activeBg;

            // Position X
            var pos = _rect.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, _basePos.x + targetOffset, dt * speed);
            _rect.anchoredPosition = pos;

            // Scale
            float curScale = _rect.localScale.x;
            float nextScale = Mathf.Lerp(curScale, targetScale, dt * speed);
            _rect.localScale = new Vector3(nextScale, nextScale, 1f);

            // Background color
            if (_bgImage != null)
            {
                _bgImage.color = Color.Lerp(_bgImage.color, targetBg, dt * speed);
            }

            // Label color & spacing
            if (_label != null)
            {
                _label.color = Color.Lerp(_label.color, targetText, dt * speed);
                _label.characterSpacing = Mathf.Lerp(_label.characterSpacing, targetSpacing, dt * speed);
            }

            // Left Accent Bar
            if (_accentBarRt != null)
            {
                var bs = _accentBarRt.localScale;
                bs.y = Mathf.Lerp(bs.y, targetBarScaleY, dt * speed);
                _accentBarRt.localScale = bs;
            }
            if (_accentBarImg != null)
            {
                _accentBarImg.color = Color.Lerp(_accentBarImg.color, targetBarCol, dt * speed);
            }
        }

        private void ApplyStateImmediate()
        {
            if (!_isInitialized || _rect == null) return;

            float targetOffset = _isActive ? _nudgeDistance : 0f;
            float targetScale = _isActive ? 1.035f : 1.0f;
            Color targetBg = _isActive ? _activeBg : _inactiveBg;
            Color targetText = _isActive ? _activeText : _inactiveText;
            float targetSpacing = _isActive ? 3.5f : 2.0f;
            float targetBarScaleY = _isActive ? 1.0f : 0f;
            Color targetBarCol = _isActive ? new Color(0.08f, 0.08f, 0.10f, 0.85f) : _activeBg;

            _rect.anchoredPosition = new Vector2(_basePos.x + targetOffset, _basePos.y);
            _rect.localScale = new Vector3(targetScale, targetScale, 1f);

            if (_bgImage != null) _bgImage.color = targetBg;
            if (_label != null)
            {
                _label.color = targetText;
                _label.characterSpacing = targetSpacing;
            }
            if (_accentBarRt != null) _accentBarRt.localScale = new Vector3(1f, targetBarScaleY, 1f);
            if (_accentBarImg != null) _accentBarImg.color = targetBarCol;
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            ApplyStateImmediate();
        }
    }

    /// <summary>
    /// Highlights both the setting row backdrop, left gold bar, and paired title TextMeshProUGUI
    /// whenever the user hovers or interacts with a setting control or its title label.
    /// </summary>
    public class SettingsRowHighlightItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private TextMeshProUGUI _titleText;
        private RectTransform _titleRt;
        private float _baseTitleX;
        private bool _titleXCaptured;

        private Image _rowBackdropImg;
        private RectTransform _rowAccentBarRt;
        private Color _idleTitleCol;
        private Color _hoverTitleCol;
        private bool _isHovered;
        private bool _isSelected;
        private bool _isInitialized;

        public void Init(
            Transform panelTr,
            TextMeshProUGUI titleText,
            RectTransform controlRt,
            Color idleTitleCol,
            Color hoverTitleCol,
            Color goldAccent)
        {
            _titleText = titleText;
            _idleTitleCol = idleTitleCol;
            _hoverTitleCol = hoverTitleCol;

            if (_titleText != null)
            {
                _titleRt = _titleText.rectTransform;
                if (!_titleXCaptured && _titleRt != null)
                {
                    _baseTitleX = _titleRt.anchoredPosition.x;
                    _titleXCaptured = true;
                }

                // Allow hovering the label itself to highlight the row
                _titleText.raycastTarget = true;
                var relay = _titleText.GetComponent<SettingsRowLabelRelay>();
                if (relay == null) relay = _titleText.gameObject.AddComponent<SettingsRowLabelRelay>();
                relay.Bind(this);
            }

            if (panelTr != null && _rowBackdropImg == null)
            {
                string bgName = $"RowHighlight_{gameObject.name}";
                var existing = panelTr.Find(bgName);
                if (existing == null)
                {
                    var bgGo = new GameObject(bgName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    bgGo.transform.SetParent(panelTr, false);
                    bgGo.transform.SetAsFirstSibling();

                    var bgRt = bgGo.GetComponent<RectTransform>();
                    float rowY = _titleRt != null ? (_titleRt.anchoredPosition.y + controlRt.anchoredPosition.y) * 0.5f : controlRt.anchoredPosition.y;
                    bgRt.anchorMin = new Vector2(0.5f, 0.5f);
                    bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                    bgRt.pivot = new Vector2(0.5f, 0.5f);
                    bgRt.anchoredPosition = new Vector2(0f, rowY);
                    bgRt.sizeDelta = new Vector2(496f, 38f);

                    var bgImg = bgGo.GetComponent<Image>();
                    bgImg.color = new Color(1f, 0.82f, 0.18f, 0f);
                    bgImg.raycastTarget = false;

                    // Left 3px gold accent bar on the row
                    var barGo = new GameObject("RowAccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    barGo.transform.SetParent(bgGo.transform, false);
                    var barRt = barGo.GetComponent<RectTransform>();
                    barRt.anchorMin = new Vector2(0f, 0f);
                    barRt.anchorMax = new Vector2(0f, 1f);
                    barRt.pivot = new Vector2(0f, 0.5f);
                    barRt.anchoredPosition = Vector2.zero;
                    barRt.sizeDelta = new Vector2(3f, 0f);
                    barRt.localScale = new Vector3(1f, 0f, 1f);

                    var barImg = barGo.GetComponent<Image>();
                    barImg.color = goldAccent;
                    barImg.raycastTarget = false;

                    _rowBackdropImg = bgImg;
                    _rowAccentBarRt = barRt;
                }
                else
                {
                    _rowBackdropImg = existing.GetComponent<Image>();
                    var bar = existing.Find("RowAccentBar");
                    if (bar != null) _rowAccentBarRt = bar.GetComponent<RectTransform>();
                }
            }

            _isInitialized = true;
        }

        public void SetLabelHovered(bool hovered)
        {
            _isHovered = hovered;
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;
        public void OnSelect(BaseEventData eventData) => _isSelected = true;
        public void OnDeselect(BaseEventData eventData) => _isSelected = false;

        private void Update()
        {
            if (!_isInitialized) return;

            bool active = _isHovered || _isSelected;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 15f;

            if (_titleText != null)
            {
                _titleText.color = Color.Lerp(_titleText.color, active ? _hoverTitleCol : _idleTitleCol, dt * speed);
            }

            if (_titleRt != null && _titleXCaptured)
            {
                var pos = _titleRt.anchoredPosition;
                pos.x = Mathf.Lerp(pos.x, _baseTitleX + (active ? 4.5f : 0f), dt * speed);
                _titleRt.anchoredPosition = pos;
            }

            if (_rowBackdropImg != null)
            {
                Color targetBg = active ? new Color(1f, 0.82f, 0.18f, 0.075f) : new Color(1f, 0.82f, 0.18f, 0f);
                _rowBackdropImg.color = Color.Lerp(_rowBackdropImg.color, targetBg, dt * speed);
            }

            if (_rowAccentBarRt != null)
            {
                var s = _rowAccentBarRt.localScale;
                s.y = Mathf.Lerp(s.y, active ? 1f : 0f, dt * speed);
                _rowAccentBarRt.localScale = s;
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isSelected = false;
            if (_titleText != null) _titleText.color = _idleTitleCol;
            if (_titleRt != null && _titleXCaptured)
            {
                _titleRt.anchoredPosition = new Vector2(_baseTitleX, _titleRt.anchoredPosition.y);
            }
            if (_rowBackdropImg != null) _rowBackdropImg.color = new Color(1f, 0.82f, 0.18f, 0f);
            if (_rowAccentBarRt != null) _rowAccentBarRt.localScale = new Vector3(1f, 0f, 1f);
        }
    }

    public class SettingsRowLabelRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SettingsRowHighlightItem _owner;
        public void Bind(SettingsRowHighlightItem owner) => _owner = owner;
        public void OnPointerEnter(PointerEventData eventData) => _owner?.SetLabelHovered(true);
        public void OnPointerExit(PointerEventData eventData) => _owner?.SetLabelHovered(false);
    }

    /// <summary>
    /// Tactile micro-animations for Sliders: handle scale lift, fill bar brightening,
    /// value readout pop on change, and subtle audio preview tick on volume sliders.
    /// </summary>
    public class SettingsSliderHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _handleRt;
        private Image _handleImg;
        private Image _fillImg;
        private TextMeshProUGUI _valueText;
        private RectTransform _valueRt;
        private Color _goldAccent;
        private Color _goldHover;

        private bool _isHovered;
        private bool _isDragging;
        private float _valuePopTimer;
        private float _lastTickTime;
        private bool _isInitialized;

        public void Init(
            RectTransform handleRt,
            Image handleImg,
            Image fillImg,
            TextMeshProUGUI valueText,
            Color goldAccent,
            Color goldHover)
        {
            _handleRt = handleRt;
            _handleImg = handleImg;
            _fillImg = fillImg;
            _valueText = valueText;
            _valueRt = valueText != null ? valueText.rectTransform : null;
            _goldAccent = goldAccent;
            _goldHover = goldHover;
            _isInitialized = true;
        }

        public void TriggerValuePop(bool playAudioTick)
        {
            _valuePopTimer = 1f;

            if (playAudioTick && Time.unscaledTime - _lastTickTime > 0.085f)
            {
                _lastTickTime = Time.unscaledTime;
                if (SniperGame.Audio.AudioManager.Instance != null)
                {
                    SniperGame.Audio.AudioManager.Instance.PlayHitmarker(false);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;
        public void OnPointerDown(PointerEventData eventData) => _isDragging = true;
        public void OnPointerUp(PointerEventData eventData) => _isDragging = false;

        private void Update()
        {
            if (!_isInitialized) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 16f;

            if (_handleRt != null)
            {
                float targetScale = _isDragging ? 1.32f : (_isHovered ? 1.20f : 1.0f);
                float next = Mathf.Lerp(_handleRt.localScale.x, targetScale, dt * speed);
                _handleRt.localScale = new Vector3(next, next, 1f);
            }

            if (_handleImg != null)
            {
                Color targetCol = (_isDragging || _isHovered) ? _goldHover : Color.white;
                _handleImg.color = Color.Lerp(_handleImg.color, targetCol, dt * speed);
            }

            if (_fillImg != null)
            {
                Color targetFill = (_isDragging || _isHovered) ? _goldHover : _goldAccent;
                _fillImg.color = Color.Lerp(_fillImg.color, targetFill, dt * speed);
            }

            if (_valuePopTimer > 0f)
            {
                _valuePopTimer = Mathf.Max(0f, _valuePopTimer - dt * 9f);
            }

            if (_valueRt != null)
            {
                float popScale = 1f + 0.14f * Mathf.Sin(_valuePopTimer * Mathf.PI);
                _valueRt.localScale = new Vector3(popScale, popScale, 1f);
            }

            if (_valueText != null)
            {
                Color popCol = Color.Lerp(_goldAccent, new Color(1f, 0.96f, 0.65f, 1f), _valuePopTimer);
                _valueText.color = popCol;
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isDragging = false;
            _valuePopTimer = 0f;
            if (_handleRt != null) _handleRt.localScale = Vector3.one;
            if (_valueRt != null) _valueRt.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Tactile micro-animations for Toggles: hover scale lift and checkmark spring bounce on toggle.
    /// </summary>
    public class SettingsToggleHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Toggle _toggle;
        private RectTransform _bgRt;
        private Image _bgImg;
        private RectTransform _checkRt;
        private Color _boxBg;
        private Color _boxHover;
        private Color _goldAccent;

        private bool _isHovered;
        private bool _lastIsOn;
        private float _checkBounceTimer;
        private bool _isInitialized;

        public void Init(Toggle toggle, Image bgImg, Image checkImg, Color boxBg, Color boxHover, Color goldAccent)
        {
            _toggle = toggle;
            _bgImg = bgImg;
            _bgRt = bgImg != null ? bgImg.rectTransform : null;
            _checkRt = checkImg != null ? checkImg.rectTransform : null;
            _boxBg = boxBg;
            _boxHover = boxHover;
            _goldAccent = goldAccent;

            if (_toggle != null)
            {
                _lastIsOn = _toggle.isOn;
            }
            _isInitialized = true;
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

        private void Update()
        {
            if (!_isInitialized || _toggle == null) return;

            if (_toggle.isOn != _lastIsOn)
            {
                _lastIsOn = _toggle.isOn;
                if (_lastIsOn)
                {
                    _checkBounceTimer = 1f;
                }
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 16f;

            if (_bgRt != null)
            {
                float targetScale = _isHovered ? 1.09f : 1.0f;
                float s = Mathf.Lerp(_bgRt.localScale.x, targetScale, dt * speed);
                _bgRt.localScale = new Vector3(s, s, 1f);
            }

            if (_bgImg != null)
            {
                Color activeBox = _toggle.isOn ? new Color(0.16f, 0.15f, 0.10f, 1f) : _boxBg;
                Color targetCol = _isHovered ? _boxHover : activeBox;
                _bgImg.color = Color.Lerp(_bgImg.color, targetCol, dt * speed);
            }

            if (_checkBounceTimer > 0f)
            {
                _checkBounceTimer = Mathf.Max(0f, _checkBounceTimer - dt * 7.5f);
                if (_checkRt != null)
                {
                    float s = 1f + 0.24f * Mathf.Sin(_checkBounceTimer * Mathf.PI);
                    _checkRt.localScale = new Vector3(s, s, 1f);
                }
            }
            else if (_checkRt != null)
            {
                _checkRt.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            _checkBounceTimer = 0f;
            if (_bgRt != null) _bgRt.localScale = Vector3.one;
            if (_checkRt != null) _checkRt.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Tactile micro-animations for Dropdowns: subtle hover scale lift and 180-degree arrow rotation when expanded.
    /// </summary>
    public class SettingsDropdownHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform _rt;
        private RectTransform _arrowRt;
        private bool _isHovered;
        private bool _isInitialized;

        public void Init(RectTransform arrowRt)
        {
            _rt = GetComponent<RectTransform>();
            _arrowRt = arrowRt;
            _isInitialized = true;
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

        private void Update()
        {
            if (!_isInitialized) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 15f;

            bool isExpanded = transform.Find("Dropdown List") != null;

            if (_rt != null)
            {
                float targetScale = (_isHovered || isExpanded) ? 1.022f : 1.0f;
                float s = Mathf.Lerp(_rt.localScale.x, targetScale, dt * speed);
                _rt.localScale = new Vector3(s, s, 1f);
            }

            if (_arrowRt != null)
            {
                float targetZ = isExpanded ? 180f : 0f;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZ);
                _arrowRt.localRotation = Quaternion.Slerp(_arrowRt.localRotation, targetRot, dt * speed);
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            if (_rt != null) _rt.localScale = Vector3.one;
            if (_arrowRt != null) _arrowRt.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Highlights individual keybind rows inside KeybindsSidePanel on pointer hover.
    /// </summary>
    public class KeybindRowHoverItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI _actionLabel;
        private RectTransform _labelRt;
        private RectTransform _btnRt;
        private float _baseLabelX;
        private bool _isRebindable;
        private bool _isHovered;
        private bool _isInitialized;

        public void Init(bool isRebindable)
        {
            _isRebindable = isRebindable;
            if (_isInitialized) return;

            _actionLabel = transform.Find("Action_Label")?.GetComponent<TextMeshProUGUI>();
            if (_actionLabel != null)
            {
                _labelRt = _actionLabel.rectTransform;
                _baseLabelX = _labelRt.anchoredPosition.x;
            }

            var btn = GetComponentInChildren<Button>(true);
            if (btn != null)
            {
                _btnRt = btn.GetComponent<RectTransform>();
            }

            _isInitialized = true;
        }

        public void OnPointerEnter(PointerEventData eventData) => _isHovered = true;
        public void OnPointerExit(PointerEventData eventData) => _isHovered = false;

        private void Update()
        {
            if (!_isInitialized) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = 15f;

            if (_actionLabel != null)
            {
                Color idleCol = new Color(0.90f, 0.92f, 0.96f, 1f);
                Color hoverCol = new Color(1f, 0.88f, 0.35f, 1f);
                _actionLabel.color = Color.Lerp(_actionLabel.color, _isHovered ? hoverCol : idleCol, dt * speed);
            }

            if (_labelRt != null)
            {
                var p = _labelRt.anchoredPosition;
                p.x = Mathf.Lerp(p.x, _baseLabelX + (_isHovered ? 4f : 0f), dt * speed);
                _labelRt.anchoredPosition = p;
            }

            if (_btnRt != null && _isRebindable && !KeybindManager.IsRebinding)
            {
                float targetScale = _isHovered ? 1.04f : 1.0f;
                float s = Mathf.Lerp(_btnRt.localScale.x, targetScale, dt * speed);
                _btnRt.localScale = new Vector3(s, s, 1f);
            }
        }

        private void OnDisable()
        {
            _isHovered = false;
            if (_labelRt != null) _labelRt.anchoredPosition = new Vector2(_baseLabelX, _labelRt.anchoredPosition.y);
            if (_btnRt != null && !KeybindManager.IsRebinding) _btnRt.localScale = Vector3.one;
        }
    }
}
