using Unity.Netcode;
using UnityEngine;
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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            IsPaused = false;

            // Zorg dat de sub-panelen standaard dicht staan
            if (mainPausePanel != null) mainPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            SetupButtonListeners();
            LoadSavedSettings();
        }

        private void Start()
        {
            ApplyVolume(volumeSlider != null ? volumeSlider.value : 0.8f);
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

            if (mainPausePanel != null) mainPausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenSettings()
        {
            if (mainPausePanel != null) mainPausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void BackToMainPause()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (mainPausePanel != null) mainPausePanel.SetActive(true);
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

            if (mainPausePanel != null) mainPausePanel.SetActive(false);
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

        private void LoadSavedSettings()
        {
            float savedSensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, 0.15f);
            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.02f;
                sensitivitySlider.maxValue = 0.60f;
                sensitivitySlider.value = savedSensitivity;
            }
            OnSensitivityChanged(savedSensitivity);

            float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 0.8f);
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
        }

        private void ApplyVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public static float GetSensitivity()
        {
            return PlayerPrefs.GetFloat(SensitivityPrefKey, 0.15f);
        }
    }
}