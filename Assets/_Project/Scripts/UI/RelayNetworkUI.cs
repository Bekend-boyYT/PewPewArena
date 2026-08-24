using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SniperGame.Player;

namespace SniperGame.UI
{
    public class RelayNetworkUI : MonoBehaviour
    {
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
            PlayerLook.IsGameplayActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (openCreateScreenButton != null) openCreateScreenButton.onClick.AddListener(OnOpenCreateScreen);
            if (openJoinScreenButton != null) openJoinScreenButton.onClick.AddListener(OnOpenJoinScreen);

            if (createBackButton != null) createBackButton.onClick.AddListener(OnCreateBackClicked);
            if (createStartButton != null) createStartButton.onClick.AddListener(OnStartGameClicked);

            if (joinBackButton != null) joinBackButton.onClick.AddListener(OnJoinBackClicked);
            if (joinConfirmButton != null) joinConfirmButton.onClick.AddListener(OnJoinConfirmClicked);

            ShowScreen(mainScreen);

            await InitializeUnityServicesAsync();
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

        private void ShowScreen(GameObject screenToShow)
        {
            if (mainScreen != null) mainScreen.SetActive(screenToShow == mainScreen);
            if (createScreen != null) createScreen.SetActive(screenToShow == createScreen);
            if (joinScreen != null) joinScreen.SetActive(screenToShow == joinScreen);
        }

        private async void OnOpenCreateScreen()
        {
            ShowScreen(createScreen);
            
            if (createStatusText != null) createStatusText.text = "Relay code genereren...";
            if (gameCodeDisplay != null) gameCodeDisplay.text = "LADEN...";
            if (createStartButton != null) createStartButton.interactable = false;

            await CreateRelayGame();
        }

        private void OnOpenJoinScreen()
        {
            ShowScreen(joinScreen);
            if (joinStatusText != null) joinStatusText.text = "Voer de 6-letterige code in.";
            if (joinCodeInput != null) joinCodeInput.text = "";
        }

        private void OnCreateBackClicked()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            ShowScreen(mainScreen);
        }

        private void OnJoinBackClicked()
        {
            ShowScreen(mainScreen);
        }

        private async Task CreateRelayGame()
{
    try
    {
        // 1. Zoek beschikbare regio's en kies de beste (laagste latency)
        var regions = await RelayService.Instance.ListRegionsAsync();
        string targetRegion = null;

        // Geef voorrang aan Europese datacenters voor minimale ping
        foreach (var region in regions)
        {
            if (region.Id.StartsWith("eu-") || region.Id.Contains("europe"))
            {
                targetRegion = region.Id;
                break;
            }
        }

        // 2. Maak allocatie aan in de gekozen regio
        Allocation allocation = targetRegion != null 
            ? await RelayService.Instance.CreateAllocationAsync(1, targetRegion)
            : await RelayService.Instance.CreateAllocationAsync(1);

        _currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        if (gameCodeDisplay != null) gameCodeDisplay.text = _currentJoinCode;
        if (createStatusText != null) createStatusText.text = $"Regio: {allocation.Region} | Klik op START";
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
        Debug.LogError($"[RelayUI] Fout bij CreateAllocation: {e.Message}");
    }
}

        private void OnStartGameClicked()
        {
            StartGameplay();
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

                if (NetworkManager.Singleton.StartClient())
                {
                    StartGameplay();
                }
            }
            catch (Exception e)
            {
                if (joinStatusText != null) joinStatusText.text = "Code ongeldig of host onbereikbaar!";
                Debug.LogError($"[RelayUI] JoinAllocation mislukt: {e.Message}");
            }
        }

        private void StartGameplay()
        {
            if (mainScreen != null) mainScreen.SetActive(false);
            if (createScreen != null) createScreen.SetActive(false);
            if (joinScreen != null) joinScreen.SetActive(false);

            PlayerLook.IsGameplayActive = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}