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
        [Tooltip("Exacte naam van de gameplay scene in Build Settings.")]
        [SerializeField] private string gameplaySceneName = "Maintestgameplay";

        [Header("Screens / Panels")]
        [SerializeField] private GameObject mainScreen;
        [SerializeField] private GameObject createScreen;
        [SerializeField] private GameObject joinScreen;

        [Header("Main Screen Elements")]
        [SerializeField] private Button openCreateScreenButton;
        [SerializeField] private Button openJoinScreenButton;

        [Header("Create Screen Elements")]
        [SerializeField] private TextMeshProUGUI gameCodeDisplay;
        [SerializeField] private Button createBackButton;
        [SerializeField] private Button createStartButton;
        [SerializeField] private TextMeshProUGUI createStatusText;

        [Header("Join Screen Elements")]
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private Button joinBackButton;
        [SerializeField] private Button joinConfirmButton;
        [SerializeField] private TextMeshProUGUI joinStatusText;

        private string _currentJoinCode = "";

        private async void Awake()
        {
            UnlockCursor();

            if (openCreateScreenButton != null) openCreateScreenButton.onClick.AddListener(OnOpenCreateScreen);
            if (openJoinScreenButton != null) openJoinScreenButton.onClick.AddListener(OnOpenJoinScreen);

            if (createBackButton != null) createBackButton.onClick.AddListener(OnCreateBackClicked);
            if (createStartButton != null) createStartButton.onClick.AddListener(OnStartGameClicked);

            if (joinBackButton != null) joinBackButton.onClick.AddListener(OnJoinBackClicked);
            if (joinConfirmButton != null) joinConfirmButton.onClick.AddListener(OnJoinConfirmClicked);

            ShowScreen(mainScreen);

            await InitializeUnityServicesAsync();
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
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayUI] Authentication fout: {e.Message}");
            }
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #region Screen Navigation

        private void ShowScreen(GameObject screenToShow)
        {
            UnlockCursor();

            if (mainScreen != null) mainScreen.SetActive(screenToShow == mainScreen);
            if (createScreen != null) createScreen.SetActive(screenToShow == createScreen);
            if (joinScreen != null) joinScreen.SetActive(screenToShow == joinScreen);

            // Zorg dat knoppen op het hoofdscherm altijd klikbaar zijn
            if (openCreateScreenButton != null) openCreateScreenButton.interactable = true;
            if (openJoinScreenButton != null) openJoinScreenButton.interactable = true;
        }

        private async void OnOpenCreateScreen()
        {
            ShowScreen(createScreen);

            if (createStatusText != null) createStatusText.text = "Relay code genereren...";
            if (gameCodeDisplay != null) gameCodeDisplay.text = "LADEN...";
            if (createStartButton != null) createStartButton.interactable = false;
            if (createBackButton != null) createBackButton.interactable = true;

            await CreateRelayGame();
        }

        private void OnOpenJoinScreen()
        {
            ShowScreen(joinScreen);
            if (joinStatusText != null) joinStatusText.text = "Voer de 6-letterige code in.";
            if (joinCodeInput != null) joinCodeInput.text = "";
            if (joinConfirmButton != null) joinConfirmButton.interactable = true;
            if (joinBackButton != null) joinBackButton.interactable = true;
        }

        private void OnCreateBackClicked()
        {
            // Verbreek actieve host sessie netjes
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ShowScreen(mainScreen);
        }

        private void OnJoinBackClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ShowScreen(mainScreen);
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
                if (createStatusText != null) createStatusText.text = "Code gereed! Wachten tot Speler 2 joint...";
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
                if (gameCodeDisplay != null) gameCodeDisplay.text = "FOUT";
                if (createStatusText != null) createStatusText.text = "Relay mislukt!";
                if (createBackButton != null) createBackButton.interactable = true;
                Debug.LogError($"[RelayUI] Fout bij CreateAllocation: {e.Message}");
            }
        }

        private void OnStartGameClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (createStatusText != null) createStatusText.text = "Scene inladen...";
                if (createStartButton != null) createStartButton.interactable = false;

                NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
            }
        }

        private async void OnJoinConfirmClicked()
        {
            string code = joinCodeInput != null ? joinCodeInput.text.Trim() : "";

            if (string.IsNullOrEmpty(code))
            {
                if (joinStatusText != null) joinStatusText.text = "Vul eerst een code in!";
                return;
            }

            if (joinStatusText != null) joinStatusText.text = $"Verbinden met code {code}...";
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
                if (joinStatusText != null) joinStatusText.text = "Code ongeldig of host onbereikbaar!";
                if (joinConfirmButton != null) joinConfirmButton.interactable = true;
                Debug.LogError($"[RelayUI] JoinAllocation mislukt: {e.Message}");
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
                        createStatusText.text = "<color=#00FF00>Speler 2 verbonden!</color> Klik op START.";
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
                        joinStatusText.text = "<color=#00FF00>Verbonden!</color> Wachten op host...";
                    }
                }
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer && clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (joinStatusText != null) joinStatusText.text = "Verbinding verbroken!";
                if (joinConfirmButton != null) joinConfirmButton.interactable = true;
            }
        }

        #endregion
    }
}