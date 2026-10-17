using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using PewPewArena.UI;

namespace SniperGame.UI
{
    public class RelayNetworkUI : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("Exact name of the gameplay scene in Build Settings.")]
        [SerializeField] private string gameplaySceneName = "Maintestgameplay";
        [Tooltip("Exact name of the tutorial scene in Build Settings.")]
        [SerializeField] private string tutorialSceneName = "Tutorial";

        [Header("Screens / Panels")]
        [SerializeField] private GameObject persistentBackground;
        [SerializeField] private GameObject mainScreen;
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject loadoutPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject settingsContainer;
        [SerializeField] private GameObject createScreen;
        [SerializeField] private GameObject joinScreen;

        private GameObject _settingsContainer;
        private GameObject _settingsInnerPanel;
        private Sprite _menuBackgroundSprite;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Play Menu Buttons")]
        [SerializeField] private Button openCreateScreenButton;
        [SerializeField] private Button openJoinScreenButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button playBackButton;

        [Header("Loadout Menu Buttons")]
        [SerializeField] private Button sniper1Button;
        [SerializeField] private Button sniper2Button;
        [SerializeField] private Button loadoutBackButton;

        [Header("Settings Screen Elements")]
        [SerializeField] private Button settingsBackButton;

        [Header("Create Screen Elements")]
        [SerializeField] private TextMeshProUGUI gameCodeDisplay;
        [SerializeField] private Button createBackButton;
        [SerializeField] private Button createStartButton;
        [SerializeField] private TextMeshProUGUI createStatusText;
        [SerializeField] private Button mapOptionButton;
        [SerializeField] private TextMeshProUGUI mapOptionText;
        [SerializeField] private Button weaponToggleButton;
        [SerializeField] private TextMeshProUGUI weaponToggleText;

        public static bool HostSniper2Allowed = true;
        public static int HostEquippedSniper = 0;

        private const string SelectedLoadoutKey = "SelectedLoadout";
        public const string LoadoutRequiredMessage = LoadoutRequiredPopup.DefaultPopupMessage;

        private static bool _sessionLoadoutInitialized = false;
        private static bool _hasSelectedLoadout = false;

        private LoadoutRequiredPopup _loadoutRequiredPopup;
        private bool _returnToCreateScreenFromLoadout = false;
        private bool _isCreatingRelay = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionLoadoutState()
        {
            _sessionLoadoutInitialized = false;
            _hasSelectedLoadout = false;
            HostEquippedSniper = 0;
        }

        public static bool HasValidLoadoutSelected()
        {
            if (!_hasSelectedLoadout) return false;
            if (!PlayerPrefs.HasKey(SelectedLoadoutKey)) return false;
            int selected = PlayerPrefs.GetInt(SelectedLoadoutKey, -1);
            return selected == 0 || selected == 1;
        }

        public static void ClearLoadoutSelection()
        {
            _hasSelectedLoadout = false;
            HostEquippedSniper = 0;
            PlayerPrefs.DeleteKey(SelectedLoadoutKey);
            PlayerPrefs.Save();
        }

        [System.Serializable]
        public struct MapOption
        {
            public string displayName;
            public string sceneName;
        }

        [SerializeField] private MapOption[] availableMaps = new MapOption[]
        {
            new MapOption { displayName = "ARENA", sceneName = "Maintestgameplay" },
            new MapOption { displayName = "ARENA 2", sceneName = "Map2Test" }
        };

        private int _selectedMapIndex = 0;

        [Header("Join Screen Elements")]
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Button joinBackButton;
        [SerializeField] private Button joinConfirmButton;
        [SerializeField] private TextMeshProUGUI joinStatusText;

        private string _currentJoinCode = "";

        private async void Awake()
        {
            ForceUnlockCursor();

            if (!_sessionLoadoutInitialized)
            {
                _sessionLoadoutInitialized = true;
                ClearLoadoutSelection();
            }

            EnsurePersistentBackground();
            AutoFindReferences();
            EnsureLoadoutRequiredPopup();
            SetupButtonListeners();
            RefreshLoadoutVisuals();
            RefreshMapSelectionVisuals();
            RefreshWeaponToggleVisuals();

            ShowScreen(mainScreen);

            await InitializeUnityServicesAsync();
        }

        private void AutoFindReferences()
        {
            if (mainScreen == null) mainScreen = transform.Find("MainMenuPanel")?.gameObject;
            if (playPanel == null) playPanel = transform.Find("PlayPanel")?.gameObject;
            if (loadoutPanel == null) loadoutPanel = transform.Find("LoadoutPanel")?.gameObject;
            FindSettingsReferences();
            if (createScreen == null) createScreen = transform.Find("CreateScreen")?.gameObject;
            if (joinScreen == null) joinScreen = transform.Find("JoinScreen")?.gameObject;

            if (playButton == null && mainScreen != null)
                playButton = mainScreen.transform.Find("PlayButton")?.GetComponent<Button>();
            if (settingsButton == null && mainScreen != null)
                settingsButton = mainScreen.transform.Find("SettingsButton")?.GetComponent<Button>();
            if (quitButton == null && mainScreen != null)
                quitButton = mainScreen.transform.Find("QuitGame")?.GetComponent<Button>() ?? mainScreen.transform.Find("QuitButton")?.GetComponent<Button>();

            if (settingsBackButton == null)
                settingsBackButton = FindSettingsBackButton();

            if (openCreateScreenButton == null && playPanel != null)
                openCreateScreenButton = playPanel.transform.Find("CreateGameButton")?.GetComponent<Button>();
            if (openJoinScreenButton == null && playPanel != null)
                openJoinScreenButton = playPanel.transform.Find("JoinGameButton")?.GetComponent<Button>();
            if (loadoutButton == null && playPanel != null)
                loadoutButton = playPanel.transform.Find("LoadoutButton")?.GetComponent<Button>();
            if (tutorialButton == null && playPanel != null)
                tutorialButton = playPanel.transform.Find("TutorialButton")?.GetComponent<Button>();
            if (playBackButton == null && playPanel != null)
                playBackButton = playPanel.transform.Find("PlayBackButton")?.GetComponent<Button>();

            if (loadoutBackButton == null && loadoutPanel != null)
                loadoutBackButton = loadoutPanel.transform.Find("LoadoutBackButton")?.GetComponent<Button>();
            if (sniper1Button == null && loadoutPanel != null)
                sniper1Button = loadoutPanel.transform.Find("WeaponCardsContainer/Sniper1Card")?.GetComponent<Button>();
            if (sniper2Button == null && loadoutPanel != null)
                sniper2Button = loadoutPanel.transform.Find("WeaponCardsContainer/Sniper2Card")?.GetComponent<Button>();

            if (createBackButton == null && createScreen != null)
                createBackButton = createScreen.transform.Find("CreateBackButton")?.GetComponent<Button>();
            if (createStartButton == null && createScreen != null)
                createStartButton = createScreen.transform.Find("CreateStartButton")?.GetComponent<Button>();
            if (mapOptionButton == null && createScreen != null)
                mapOptionButton = createScreen.transform.Find("MapSelectRow/MapOptionButton")?.GetComponent<Button>();
            if (mapOptionText == null && mapOptionButton != null)
                mapOptionText = mapOptionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (weaponToggleButton == null && createScreen != null)
                weaponToggleButton = createScreen.transform.Find("WeaponSelectRow/WeaponToggleButton")?.GetComponent<Button>();
            if (weaponToggleText == null && weaponToggleButton != null)
                weaponToggleText = weaponToggleButton.GetComponentInChildren<TextMeshProUGUI>();

            if (joinBackButton == null && joinScreen != null)
                joinBackButton = joinScreen.transform.Find("JoinBackButton")?.GetComponent<Button>();
            if (joinConfirmButton == null && joinScreen != null)
                joinConfirmButton = joinScreen.transform.Find("JoinConfirmButton")?.GetComponent<Button>();
        }

        private void FindSettingsReferences()
        {
            if (_settingsContainer == null && settingsContainer != null)
                _settingsContainer = settingsContainer;

            if (_settingsInnerPanel == null && settingsPanel != null)
            {
                if (settingsPanel.name.IndexOf("main", StringComparison.OrdinalIgnoreCase) >= 0 && _settingsContainer == null)
                {
                    _settingsContainer = settingsPanel;
                }
                else
                {
                    _settingsInnerPanel = settingsPanel;
                }
            }

            if (_settingsInnerPanel == null || _settingsContainer == null)
            {
                List<Transform> allCandidates = new List<Transform>();
                allCandidates.AddRange(GetComponentsInChildren<Transform>(true));
                if (transform.root != null && transform.root != transform)
                {
                    allCandidates.AddRange(transform.root.GetComponentsInChildren<Transform>(true));
                }

                var activeScene = gameObject.scene.isLoaded ? gameObject.scene : SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    foreach (var rootGo in activeScene.GetRootGameObjects())
                    {
                        if (rootGo != null)
                        {
                            allCandidates.AddRange(rootGo.GetComponentsInChildren<Transform>(true));
                        }
                    }
                }

                // 1. Search for outer settings container (e.g. settingspanelmain or any container named with settings + main)
                if (_settingsContainer == null)
                {
                    foreach (var t in allCandidates)
                    {
                        if (t == null) continue;
                        string lower = t.name.ToLowerInvariant();
                        if (lower.Contains("settingspanelmain") || lower.Contains("settingsmain") || lower.Contains("settingpanelmain") || lower.Contains("settings_main"))
                        {
                            _settingsContainer = t.gameObject;
                            break;
                        }
                    }
                }

                // 2. Search for inner SettingsPanel or SettingsUIController
                if (_settingsInnerPanel == null)
                {
                    if (_settingsContainer != null)
                    {
                        foreach (var t in _settingsContainer.GetComponentsInChildren<Transform>(true))
                        {
                            if (t == null || t.gameObject == _settingsContainer) continue;
                            if (t.name.Equals("SettingsPanel", StringComparison.OrdinalIgnoreCase) ||
                                t.GetComponent<Settings.SettingsUIController>() != null)
                            {
                                _settingsInnerPanel = t.gameObject;
                                break;
                            }
                        }
                    }

                    if (_settingsInnerPanel == null)
                    {
                        foreach (var t in allCandidates)
                        {
                            if (t == null) continue;
                            if (t.name.Equals("SettingsPanel", StringComparison.OrdinalIgnoreCase))
                            {
                                _settingsInnerPanel = t.gameObject;
                                break;
                            }
                        }
                    }

                    if (_settingsInnerPanel == null)
                    {
                        foreach (var t in allCandidates)
                        {
                            if (t == null) continue;
                            if (t.GetComponent<Settings.SettingsUIController>() != null)
                            {
                                _settingsInnerPanel = t.gameObject;
                                break;
                            }
                        }
                    }
                }

                // 3. If container wasn't found by name, but inner panel has a parent other than ConnectionCanvas
                if (_settingsContainer == null && _settingsInnerPanel != null)
                {
                    Transform parent = _settingsInnerPanel.transform.parent;
                    if (parent != null && parent != transform)
                    {
                        _settingsContainer = parent.gameObject;
                    }
                }

                // 4. Fallbacks
                if (_settingsContainer == null && _settingsInnerPanel != null)
                {
                    _settingsContainer = _settingsInnerPanel;
                }
                if (_settingsInnerPanel == null && _settingsContainer != null)
                {
                    _settingsInnerPanel = _settingsContainer;
                }
            }

            if (settingsPanel == null)
            {
                settingsPanel = _settingsInnerPanel ?? _settingsContainer;
            }
            if (settingsContainer == null)
            {
                settingsContainer = _settingsContainer;
            }

            EnsureContainerUnderCanvas();

            if (settingsBackButton == null)
            {
                settingsBackButton = FindSettingsBackButton();
            }
        }

        private void EnsureContainerUnderCanvas()
        {
            if (_settingsContainer == null) return;

            Canvas parentCanvas = _settingsContainer.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                _settingsContainer.transform.SetParent(this.transform, false);

                RectTransform rt = _settingsContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = Vector3.one;
                }
                Debug.Log("[RelayUI] Reparented settings container under ConnectionCanvas to ensure Canvas rendering.");
            }
        }

        private Button FindSettingsBackButton()
        {
            if (settingsBackButton != null) return settingsBackButton;

            if (_settingsInnerPanel != null)
            {
                var btn = _settingsInnerPanel.transform.Find("SettingsBackButton")?.GetComponent<Button>();
                if (btn != null) return btn;

                foreach (var b in _settingsInnerPanel.GetComponentsInChildren<Button>(true))
                {
                    if (b != null && b.gameObject.name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0)
                        return b;
                }
            }

            if (_settingsContainer != null && _settingsContainer != _settingsInnerPanel)
            {
                var btn = _settingsContainer.transform.Find("SettingsBackButton")?.GetComponent<Button>();
                if (btn != null) return btn;

                foreach (var b in _settingsContainer.GetComponentsInChildren<Button>(true))
                {
                    if (b != null && b.gameObject.name.IndexOf("back", StringComparison.OrdinalIgnoreCase) >= 0)
                        return b;
                }
            }

            return null;
        }

        private void SetupButtonListeners()
        {
            if (playButton != null) playButton.onClick.AddListener(OnOpenPlayScreen);
            if (playBackButton != null) playBackButton.onClick.AddListener(OnPlayBackClicked);

            if (settingsButton != null) settingsButton.onClick.AddListener(OnOpenSettingsScreen);
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

            if (openCreateScreenButton != null) openCreateScreenButton.onClick.AddListener(OnOpenCreateScreen);
            if (createBackButton != null) createBackButton.onClick.AddListener(OnCreateBackClicked);
            if (createStartButton != null) createStartButton.onClick.AddListener(OnStartGameClicked);
            if (mapOptionButton != null) mapOptionButton.onClick.AddListener(OnCycleMapClicked);
            if (weaponToggleButton != null) weaponToggleButton.onClick.AddListener(OnToggleWeaponSettingsClicked);

            if (openJoinScreenButton != null) openJoinScreenButton.onClick.AddListener(OnOpenJoinScreen);
            if (joinBackButton != null) joinBackButton.onClick.AddListener(OnJoinBackClicked);
            if (joinConfirmButton != null) joinConfirmButton.onClick.AddListener(OnJoinConfirmClicked);

            if (loadoutButton != null) loadoutButton.onClick.AddListener(OnOpenLoadoutScreen);
            if (loadoutBackButton != null) loadoutBackButton.onClick.AddListener(OnLoadoutBackClicked);

            if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialClicked);

            if (sniper1Button != null) sniper1Button.onClick.AddListener(() => OnSelectSniper(0));
            if (sniper2Button != null) sniper2Button.onClick.AddListener(() => OnSelectSniper(1));

            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                ForceUnlockCursor();
            }
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private async Task InitializeUnityServicesAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions();

                    #if UNITY_EDITOR
                    options.SetProfile("Editor_User");
                    #else
                    string uniqueProfile = $"BuildUser_{System.Guid.NewGuid().ToString().Substring(0, 6)}";
                    options.SetProfile(uniqueProfile);
                    #endif

                    await UnityServices.InitializeAsync(options);
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[RelayUI] Signed in as player: {AuthenticationService.Instance.PlayerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayUI] Authentication error: {e.Message}");
            }
        }

        private void ForceUnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #region Persistent Global Background
        private void EnsurePersistentBackground()
        {
            // 1. Ensure camera clears to solid dark slate color rather than Unity Skybox
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
            }

            // 2. Locate or instantiate PersistentGlobalBackground directly under ConnectionCanvas root
            if (persistentBackground == null)
            {
                Transform bgTr = transform.Find("PersistentGlobalBackground");
                if (bgTr != null)
                {
                    persistentBackground = bgTr.gameObject;
                }
            }

            if (persistentBackground == null)
            {
                persistentBackground = new GameObject("PersistentGlobalBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                persistentBackground.transform.SetParent(this.transform, false);
            }

            // Always ensure it is Sibling Index 0 so it renders behind all menu panels
            persistentBackground.transform.SetAsFirstSibling();

            // Stretch RectTransform to fill entire canvas seamlessly
            RectTransform rt = persistentBackground.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localScale = Vector3.one;
            }

            // Configure Image component
            Image bgImg = persistentBackground.GetComponent<Image>();
            if (bgImg != null)
            {
                if (bgImg.sprite == null)
                {
                    bgImg.sprite = FindMenuBackgroundSprite();
                }
                bgImg.color = Color.white;
                bgImg.raycastTarget = false; // Never block mouse clicks!
            }

            persistentBackground.SetActive(true);

            // Clean up redundant child background images inside individual sub-panels so they don't overdraw or fade
            CleanUpSubpanelBackgrounds();
        }

        private Sprite FindMenuBackgroundSprite()
        {
            if (_menuBackgroundSprite != null) return _menuBackgroundSprite;

            // Search child images for existing background sprite (e.g. MainBackround.jpg)
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img != null && img.sprite != null)
                {
                    string sName = img.sprite.name;
                    string goName = img.gameObject.name;
                    if (sName.IndexOf("Backround", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        sName.IndexOf("Screenshot", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Backround", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        goName.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _menuBackgroundSprite = img.sprite;
                        return _menuBackgroundSprite;
                    }
                }
            }
            return null;
        }

        private void CleanUpSubpanelBackgrounds()
        {
            string[] subBgNames = new string[] { "MainBackround", "MainBackroundimage", "BackroundPanel" };
            Transform[] panelTrs = new Transform[] {
                mainScreen != null ? mainScreen.transform : null,
                playPanel != null ? playPanel.transform : null,
                createScreen != null ? createScreen.transform : null,
                joinScreen != null ? joinScreen.transform : null,
                loadoutPanel != null ? loadoutPanel.transform : null
            };

            foreach (var pTr in panelTrs)
            {
                if (pTr == null) continue;
                foreach (var bgName in subBgNames)
                {
                    Transform childBg = pTr.Find(bgName);
                    if (childBg != null)
                    {
                        Image img = childBg.GetComponent<Image>();
                        if (img != null)
                        {
                            img.enabled = false;
                        }
                    }
                }
            }
        }
        #endregion

        #region Loadout Required Popup

        public LoadoutRequiredPopup LoadoutPopup => _loadoutRequiredPopup;

        private void EnsureLoadoutRequiredPopup()
        {
            if (_loadoutRequiredPopup == null)
            {
                Transform existing = transform.Find("LoadoutRequiredPopup");
                if (existing != null)
                {
                    _loadoutRequiredPopup = existing.GetComponent<LoadoutRequiredPopup>();
                    if (_loadoutRequiredPopup == null)
                    {
                        _loadoutRequiredPopup = existing.gameObject.AddComponent<LoadoutRequiredPopup>();
                    }
                }
            }

            if (_loadoutRequiredPopup == null)
            {
                var popupGo = new GameObject("LoadoutRequiredPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(LoadoutRequiredPopup));
                popupGo.layer = gameObject.layer;
                popupGo.transform.SetParent(this.transform, false);
                _loadoutRequiredPopup = popupGo.GetComponent<LoadoutRequiredPopup>();
            }

            TMP_FontAsset fallbackFont = null;
            if (createStatusText != null && createStatusText.font != null) fallbackFont = createStatusText.font;
            else if (gameCodeDisplay != null && gameCodeDisplay.font != null) fallbackFont = gameCodeDisplay.font;

            _loadoutRequiredPopup.Initialize(
                fallbackFont,
                onGoToLoadout: OnPopupGoToLoadoutClicked,
                onClose: HideLoadoutRequiredPopup
            );
            _loadoutRequiredPopup.Hide();
        }

        public void ShowLoadoutRequiredPopup()
        {
            EnsureLoadoutRequiredPopup();
            if (_loadoutRequiredPopup != null)
            {
                _loadoutRequiredPopup.Show(LoadoutRequiredMessage);
            }
        }

        public void HideLoadoutRequiredPopup()
        {
            if (_loadoutRequiredPopup != null && _loadoutRequiredPopup.IsVisible)
            {
                _loadoutRequiredPopup.Hide();
            }
        }

        private void OnPopupGoToLoadoutClicked()
        {
            _returnToCreateScreenFromLoadout = (createScreen != null && createScreen.activeSelf);
            HideLoadoutRequiredPopup();
            ShowScreen(loadoutPanel != null ? loadoutPanel : playPanel);
            RefreshLoadoutVisuals();
        }

        #endregion

        #region Screen Navigation

        public void ShowScreen(GameObject screenToShow)
        {
            ForceUnlockCursor();
            HideLoadoutRequiredPopup();

            // Guarantee persistent background stays active and at sibling 0 behind all panels
            if (persistentBackground != null)
            {
                persistentBackground.SetActive(true);
                persistentBackground.transform.SetAsFirstSibling();
            }

            bool isSettings = (screenToShow != null && (screenToShow == settingsPanel || screenToShow == _settingsContainer || screenToShow == _settingsInnerPanel));

            if (mainScreen != null) mainScreen.SetActive(screenToShow == mainScreen);
            if (playPanel != null) playPanel.SetActive(screenToShow == playPanel);
            if (createScreen != null) createScreen.SetActive(screenToShow == createScreen);
            if (joinScreen != null) joinScreen.SetActive(screenToShow == joinScreen);
            if (loadoutPanel != null) loadoutPanel.SetActive(screenToShow == loadoutPanel);

            if (_settingsContainer != null) _settingsContainer.SetActive(isSettings);
            if (_settingsInnerPanel != null && _settingsInnerPanel != _settingsContainer) _settingsInnerPanel.SetActive(isSettings);
            if (settingsPanel != null && settingsPanel != _settingsContainer && settingsPanel != _settingsInnerPanel) settingsPanel.SetActive(isSettings);

            if (screenToShow == createScreen)
            {
                RefreshMapSelectionVisuals();
                RefreshWeaponToggleVisuals();
                if (createStartButton != null) createStartButton.interactable = true;
            }

            if (openCreateScreenButton != null) openCreateScreenButton.interactable = true;
            if (openJoinScreenButton != null) openJoinScreenButton.interactable = true;
        }

        public void OnOpenPlayScreen()
        {
            ShowScreen(playPanel != null ? playPanel : mainScreen);
        }

        public void OnPlayBackClicked()
        {
            ShowScreen(mainScreen);
        }

        public void OnTutorialClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(tutorialSceneName);
        }

        public void OnOpenSettingsScreen()
        {
            FindSettingsReferences();

            GameObject targetScreen = _settingsContainer != null ? _settingsContainer : (_settingsInnerPanel != null ? _settingsInnerPanel : settingsPanel);
            ShowScreen(targetScreen != null ? targetScreen : mainScreen);

            if (_settingsInnerPanel != null && _settingsContainer != null && _settingsInnerPanel != _settingsContainer)
            {
                _settingsInnerPanel.transform.SetAsLastSibling();
            }

            GameObject activeSettingsTarget = _settingsInnerPanel != null ? _settingsInnerPanel : _settingsContainer;
            if (activeSettingsTarget != null)
            {
                var controller = activeSettingsTarget.GetComponentInChildren<Settings.SettingsUIController>(true);
                if (controller == null)
                {
                    controller = activeSettingsTarget.GetComponent<Settings.SettingsUIController>();
                }
                if (controller == null)
                {
                    controller = activeSettingsTarget.AddComponent<Settings.SettingsUIController>();
                }

                if (controller != null)
                {
                    controller.SwitchTab(0);
                    controller.RefreshAllUI();
                }

                if (settingsBackButton == null)
                {
                    settingsBackButton = FindSettingsBackButton();
                }

                if (settingsBackButton != null)
                {
                    settingsBackButton.onClick.RemoveListener(OnSettingsBackClicked);
                    settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
                }
            }

            Debug.Log($"[RelayUI] Opened Settings screen. Container: {(_settingsContainer != null ? _settingsContainer.name : "null")}, InnerPanel: {(_settingsInnerPanel != null ? _settingsInnerPanel.name : "null")}");
        }

        public void OnSettingsBackClicked()
        {
            ShowScreen(mainScreen);
            Debug.Log("[RelayUI] Closed Settings screen. Returning to Main Menu.");
        }

        public void OnOpenLoadoutScreen()
        {
            _returnToCreateScreenFromLoadout = false;
            ShowScreen(loadoutPanel != null ? loadoutPanel : playPanel);
            RefreshLoadoutVisuals();
        }

        public void OnLoadoutBackClicked()
        {
            if (_returnToCreateScreenFromLoadout && createScreen != null)
            {
                _returnToCreateScreenFromLoadout = false;
                if ((NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) || _isCreatingRelay)
                {
                    ShowScreen(createScreen);
                    return;
                }

                OnOpenCreateScreen();
                return;
            }

            ShowScreen(playPanel != null ? playPanel : mainScreen);
        }

        public void OnSelectSniper(int index)
        {
            if (index != 0 && index != 1)
            {
                Debug.LogWarning($"[RelayUI] Invalid Sniper loadout index: {index}");
                return;
            }

            _hasSelectedLoadout = true;
            PlayerPrefs.SetInt(SelectedLoadoutKey, index);
            PlayerPrefs.Save();
            HostEquippedSniper = index;
            RefreshLoadoutVisuals(playFeedback: true);
            Debug.Log($"[RelayUI] Selected Sniper loadout: {index + 1}");
        }

        private void RefreshLoadoutVisuals(bool playFeedback = false)
        {
            bool hasValid = HasValidLoadoutSelected();
            int selected = hasValid ? PlayerPrefs.GetInt(SelectedLoadoutKey, -1) : -1;
            HostEquippedSniper = hasValid ? selected : 0;

            if (sniper1Button != null)
            {
                bool isSniper1 = hasValid && selected == 0;
                var card1 = sniper1Button.GetComponent<LoadoutCardEffect>();
                if (card1 != null)
                {
                    card1.SetEquipped(isSniper1, playFeedback);
                }
                var outline = sniper1Button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = isSniper1 ? new Color(0f, 0.83f, 1f, 0.9f) : new Color(0.23f, 0.27f, 0.35f, 0.4f);
                    outline.effectDistance = isSniper1 ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }
            }
            if (sniper2Button != null)
            {
                bool isSniper2 = hasValid && selected == 1;
                var card2 = sniper2Button.GetComponent<LoadoutCardEffect>();
                if (card2 != null)
                {
                    card2.SetEquipped(isSniper2, playFeedback);
                }
                var outline = sniper2Button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = isSniper2 ? new Color(1f, 0.69f, 0.23f, 0.9f) : new Color(0.23f, 0.27f, 0.35f, 0.4f);
                    outline.effectDistance = isSniper2 ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }
            }
        }

        public void OnToggleWeaponSettingsClicked()
        {
            HostSniper2Allowed = !HostSniper2Allowed;
            RefreshWeaponToggleVisuals();
            Debug.Log($"[RelayUI] Sniper 2 allowed setting changed: {HostSniper2Allowed}");
        }

        private void RefreshWeaponToggleVisuals()
        {
            if (weaponToggleText != null)
            {
                weaponToggleText.text = HostSniper2Allowed ? "<color=#00FF88>ENABLED</color>" : "<color=#FF4444>DISABLED</color>";
            }
        }

        private async void OnOpenCreateScreen()
        {
            _returnToCreateScreenFromLoadout = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            ShowScreen(createScreen);
            RefreshMapSelectionVisuals();
            RefreshWeaponToggleVisuals();

            if (createStatusText != null) createStatusText.text = "Generating Relay code...";
            if (gameCodeDisplay != null) gameCodeDisplay.text = "LOADING...";
            if (createStartButton != null) createStartButton.interactable = true;
            if (createBackButton != null) createBackButton.interactable = true;

            await CreateRelayGame();
        }

        public void OnCycleMapClicked()
        {
            if (availableMaps == null || availableMaps.Length == 0) return;

            _selectedMapIndex = (_selectedMapIndex + 1) % availableMaps.Length;
            RefreshMapSelectionVisuals();
            Debug.Log($"[RelayUI] Selected map: {availableMaps[_selectedMapIndex].displayName} ({availableMaps[_selectedMapIndex].sceneName})");
        }

        private void RefreshMapSelectionVisuals()
        {
            if (availableMaps == null || availableMaps.Length == 0) return;
            if (_selectedMapIndex < 0 || _selectedMapIndex >= availableMaps.Length) _selectedMapIndex = 0;

            if (mapOptionText != null)
            {
                mapOptionText.text = availableMaps[_selectedMapIndex].displayName;
            }
        }

        private void OnOpenJoinScreen()
        {
            ShowScreen(joinScreen);
            if (joinStatusText != null) joinStatusText.text = "Enter the 6-character room code.";
            if (joinCodeInput != null) joinCodeInput.text = "";
            if (joinConfirmButton != null) joinConfirmButton.interactable = true;
            if (joinBackButton != null) joinBackButton.interactable = true;
        }

        private void OnCreateBackClicked()
        {
            _returnToCreateScreenFromLoadout = false;
            HideLoadoutRequiredPopup();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ShowScreen(playPanel != null ? playPanel : mainScreen);
        }

        private void OnJoinBackClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ShowScreen(playPanel != null ? playPanel : mainScreen);
        }

        /// <summary>
        /// Closes the application. Hook this up to a Button's OnClick event in the Inspector.
        /// </summary>
        public void QuitGame()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Alias for QuitGame to allow selecting 'Quit' in the Inspector.
        /// </summary>
        public void Quit()
        {
            QuitGame();
        }

        #endregion

        #region Relay Host & Join

        private async Task CreateRelayGame()
        {
            _isCreatingRelay = true;
            try
            {
                var regions = await RelayService.Instance.ListRegionsAsync();
                string targetRegion = null;
                foreach (var region in regions)
                {
                    if (region.Id.StartsWith("eu-") || region.Id.Contains("europe"))
                    {
                        targetRegion = region.Id;
                        break;
                    }
                }

                Allocation allocation = targetRegion != null
                    ? await RelayService.Instance.CreateAllocationAsync(1, targetRegion)
                    : await RelayService.Instance.CreateAllocationAsync(1);

                _currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                if (gameCodeDisplay != null) gameCodeDisplay.text = _currentJoinCode;
                if (createStatusText != null) createStatusText.text = "Lobby ready! Waiting for <color=#00D4FF>Player 2</color> to join...";
                if (createStartButton != null) createStartButton.interactable = true;

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetHostRelayData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData
                    );
                }

                NetworkManager.Singleton.StartHost();
            }
            catch (Exception e)
            {
                if (gameCodeDisplay != null) gameCodeDisplay.text = "<color=#FF4444>ERROR</color>";
                if (createStatusText != null) createStatusText.text = "<color=#FF4444>Relay connection failed!</color>";
                if (createBackButton != null) createBackButton.interactable = true;
                Debug.LogError($"[RelayUI] Error during CreateAllocation: {e.Message}");
            }
            finally
            {
                _isCreatingRelay = false;
            }
        }

        public bool TryStartGame()
        {
            if (!HasValidLoadoutSelected())
            {
                Debug.LogWarning($"[RelayUI] Blocked game start: {LoadoutRequiredMessage}");
                ShowLoadoutRequiredPopup();
                return false;
            }

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                if (_isCreatingRelay && createStatusText != null)
                {
                    createStatusText.text = "Generating Relay code... Please wait.";
                }
                Debug.LogWarning("[RelayUI] Cannot start game yet: Host server is not active.");
                return false;
            }

            int selectedLoadout = PlayerPrefs.GetInt(SelectedLoadoutKey, -1);
            if (selectedLoadout != 0 && selectedLoadout != 1)
            {
                ShowLoadoutRequiredPopup();
                return false;
            }

            HostEquippedSniper = selectedLoadout;
            if (createStatusText != null) createStatusText.text = "Loading scene...";
            if (createStartButton != null) createStartButton.interactable = false;

            string targetScene = (availableMaps != null && availableMaps.Length > _selectedMapIndex)
                ? availableMaps[_selectedMapIndex].sceneName
                : gameplaySceneName;

            Debug.Log($"[RelayUI] Starting game on map scene: {targetScene}");
            NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            return true;
        }

        private void OnStartGameClicked()
        {
            TryStartGame();
        }

        private async void OnJoinConfirmClicked()
        {
            string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";

            if (string.IsNullOrEmpty(code))
            {
                if (joinStatusText != null) joinStatusText.text = "<color=#FFCC00>Please enter a code first!</color>";
                return;
            }

            if (joinStatusText != null) joinStatusText.text = $"Connecting with code {code}...";
            if (joinConfirmButton != null) joinConfirmButton.interactable = false;

            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetClientRelayData(
                        joinAllocation.RelayServer.IpV4,
                        (ushort)joinAllocation.RelayServer.Port,
                        joinAllocation.AllocationIdBytes,
                        joinAllocation.Key,
                        joinAllocation.ConnectionData,
                        joinAllocation.HostConnectionData
                    );
                }

                NetworkManager.Singleton.StartClient();
            }
            catch (Exception e)
            {
                if (joinStatusText != null) joinStatusText.text = "<color=#FF4444>Invalid code or host unreachable!</color>";
                if (joinConfirmButton != null) joinConfirmButton.interactable = true;
                Debug.LogError($"[RelayUI] JoinAllocation failed: {e.Message}");
            }
        }

        #endregion

        #region Netcode Callbacks

        private void HandleClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (clientId != NetworkManager.Singleton.LocalClientId)
                {
                    if (createStatusText != null)
                    {
                        createStatusText.text = "<color=#00FF00>Player 2 connected!</color> Click START.";
                    }
                    if (createStartButton != null)
                    {
                        createStartButton.interactable = true;
                    }
                }
            }
            else
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    if (joinStatusText != null)
                    {
                        joinStatusText.text = "<color=#00FF00>Connected!</color> Waiting for host...";
                    }
                }
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (joinStatusText != null) joinStatusText.text = "<color=#FF4444>Disconnected from host!</color>";
                if (joinConfirmButton != null) joinConfirmButton.interactable = true;
            }
        }

        #endregion
    }
}