using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SniperGame.UI
{
    public class NetworkConnectUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Het paneel dat verborgen moet worden na het verbinden.")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        [SerializeField] private TMP_InputField ipInputField;

        [Header("Default Network Settings")]
        [SerializeField] private string defaultIP = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        private void Awake()
        {
            // Zorg dat de muis vrij en zichtbaar is om knoppen aan te klikken
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (ipInputField != null)
            {
                ipInputField.text = defaultIP;
            }

            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (clientButton != null) clientButton.onClick.AddListener(OnClientClicked);
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnHostClicked()
        {
            if (NetworkManager.Singleton == null) return;

            SetTransportConnection(defaultIP);

            if (NetworkManager.Singleton.StartHost())
            {
                HideUI();
            }
        }

        private void OnClientClicked()
        {
            if (NetworkManager.Singleton == null) return;

            string targetIP = string.IsNullOrWhiteSpace(ipInputField?.text) ? defaultIP : ipInputField.text.Trim();
            SetTransportConnection(targetIP);

            if (NetworkManager.Singleton.StartClient())
            {
                HideUI();
            }
        }

        private void SetTransportConnection(string ip)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port);
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
            {
                HideUI();
            }
        }

        private void HideUI()
        {
            if (connectionPanel != null)
            {
                connectionPanel.SetActive(false);
            }
        }
    }
}