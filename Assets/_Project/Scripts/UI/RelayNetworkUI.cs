using System;
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
        [SerializeField] private GameObject mainScreen;
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject loadoutPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject createScreen;
        [SerializeField] private GameObject joinScreen;

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

            AutoFindReferences();
            SetupButtonListeners();
            RefreshLoadoutVisuals();
            RefreshMapSelectionVisuals();

            ShowScreen(mainScreen);

            await InitializeUnityServicesAsync();
        }

        private void AutoFindReferences()
        {
            if (mainScreen == null) mainScreen = transform.Find("MainMenuPanel")?.gameObject;
            if (playPanel == null) playPanel = transform.Find("PlayPanel")?.gameObject;
            if (loadoutPanel == null) loadoutPanel = transform.Find("LoadoutPanel")?.gameObject;
            if (settingsPanel == null)
            {
                settingsPanel = transform.Find("SettingsPanel")?.gameObject
                    ?? (transform.parent != null ? transform.parent.Find("SettingsPanel")?.gameObject : null)
                    ?? GameObject.Find("SettingsPanel");
            }
            if (createScreen == null) createScreen = transform.Find("CreateScreen")?.gameObject;
            if (joinScreen == null) joinScreen = transform.Find("JoinScreen")?.gameObject;

            if (playButton == null && mainScreen != null)
                playButton = mainScreen.transform.Find("PlayButton")?.GetComponent<Button>();
            if (settingsButton == null && mainScreen != null)
                settingsButton = mainScreen.transform.Find("SettingsButton")?.GetComponent<Button>();
            if (quitButton == null && mainScreen != null)
                quitButton = mainScreen.transform.Find("QuitGame")?.GetComponent<Button>() ?? mainScreen.transform.Find("QuitButton")?.GetComponent<Button>();

            if (settingsBackButton == null && settingsPanel != null)
                settingsBackButton = settingsPanel.transform.Find("SettingsBackButton")?.GetComponent<Button>();

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

            if (joinBackButton == null && joinScreen != null)
                joinBackButton = joinScreen.transform.Find("JoinBackButton")?.GetComponent<Button>();
            if (joinConfirmButton == null && joinScreen != null)
                joinConfirmButton = joinScreen.transform.Find("JoinConfirmButton")?.GetComponent<Button>();
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

        #region Screen Navigation

        public void ShowScreen(GameObject screenToShow)
        {
            ForceUnlockCursor();

            if (mainScreen != null) mainScreen.SetActive(screenToShow == mainScreen);
            if (playPanel != null) playPanel.SetActive(screenToShow == playPanel);
            if (settingsPanel != null) settingsPanel.SetActive(screenToShow == settingsPanel);
            if (createScreen != null) createScreen.SetActive(screenToShow == createScreen);
            if (joinScreen != null) joinScreen.SetActive(screenToShow == joinScreen);
            if (loadoutPanel != null) loadoutPanel.SetActive(screenToShow == loadoutPanel);

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
            if (settingsPanel == null)
            {
                AutoFindReferences();
            }

            ShowScreen(settingsPanel != null ? settingsPanel : mainScreen);

            if (settingsPanel != null)
            {
                var controller = settingsPanel.GetComponent<Settings.SettingsUIController>();
                if (controller == null)
                {
                    controller = settingsPanel.AddComponent<Settings.SettingsUIController>();
                }
                if (controller != null)
                {
                    controller.SwitchTab(0);
                    controller.RefreshAllUI();
                }

                if (settingsBackButton == null)
                {
                    settingsBackButton = settingsPanel.transform.Find("SettingsBackButton")?.GetComponent<Button>();
                }

                if (settingsBackButton != null)
                {
                    settingsBackButton.onClick.RemoveListener(OnSettingsBackClicked);
                    settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
                }
            }
        }

        public void OnSettingsBackClicked()
        {
            ShowScreen(mainScreen);
        }

        public void OnOpenLoadoutScreen()
        {
            ShowScreen(loadoutPanel != null ? loadoutPanel : playPanel);
            RefreshLoadoutVisuals();
        }

        public void OnLoadoutBackClicked()
        {
            ShowScreen(playPanel != null ? playPanel : mainScreen);
        }

        public void OnSelectSniper(int index)
        {
            PlayerPrefs.SetInt("SelectedLoadout", index);
            PlayerPrefs.Save();
            RefreshLoadoutVisuals();
            Debug.Log($"[RelayUI] Selected Sniper loadout: {index + 1}");
        }

        private void RefreshLoadoutVisuals()
        {
            int selected = PlayerPrefs.GetInt("SelectedLoadout", 0);
            if (sniper1Button != null)
            {
                var outline = sniper1Button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected == 0 ? new Color(0f, 0.83f, 1f, 0.9f) : new Color(0.23f, 0.27f, 0.35f, 0.4f);
                    outline.effectDistance = selected == 0 ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }
            }
            if (sniper2Button != null)
            {
                var outline = sniper2Button.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected == 1 ? new Color(1f, 0.69f, 0.23f, 0.9f) : new Color(0.23f, 0.27f, 0.35f, 0.4f);
                    outline.effectDistance = selected == 1 ? new Vector2(2.5f, -2.5f) : new Vector2(1.5f, -1.5f);
                }
            }
        }

        private async void OnOpenCreateScreen()
        {
            ShowScreen(createScreen);
            RefreshMapSelectionVisuals();

            if (createStatusText != null) createStatusText.text = "Generating Relay code...";
            if (gameCodeDisplay != null) gameCodeDisplay.text = "LOADING...";
            if (createStartButton != null) createStartButton.interactable = false;
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
        }

        private void OnStartGameClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (createStatusText != null) createStatusText.text = "Loading scene...";
                if (createStartButton != null) createStartButton.interactable = false;

                string targetScene = (availableMaps != null && availableMaps.Length > _selectedMapIndex)
                    ? availableMaps[_selectedMapIndex].sceneName
                    : gameplaySceneName;

                Debug.Log($"[RelayUI] Starting game on map scene: {targetScene}");
                NetworkManager.Singleton.SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
            }
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