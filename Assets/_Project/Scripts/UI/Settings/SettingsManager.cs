using System;
using UnityEngine;

namespace SniperGame.UI.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        #region PlayerPrefs Keys
        // General
        private const string KeyDisplayMode = "Setting_DisplayMode";
        private const string KeyResolution = "Setting_Resolution";
        private const string KeyFpsLimit = "Setting_FpsLimit";
        private const string KeyShowFps = "Setting_ShowFps";

        // Game
        private const string KeyCrosshair = "Setting_Crosshair";
        private const string KeyHitmarkers = "Setting_Hitmarkers";
        private const string KeyDamageNumbers = "Setting_DamageNumbers";

        // Graphics
        private const string KeyQuality = "Setting_Quality";
        private const string KeyVsync = "Setting_Vsync";
        private const string KeyShadows = "Setting_Shadows";
        private const string KeyAntiAliasing = "Setting_AntiAliasing";
        private const string KeyFOV = "Setting_FOV";
        private const string KeyViewDistance = "Setting_ViewDistance";

        // Audio
        private const string KeyMasterVol = "Setting_MasterVolume";
        private const string KeySfxVol = "Setting_SfxVolume";
        private const string KeyMusicVol = "Setting_MusicVolume";
        private const string KeyVoiceVol = "Setting_VoiceVolume";

        // Controls
        private const string KeyMouseSens = "Setting_MouseSens";
        private const string KeyAdsSens = "Setting_AdsSens";
        private const string KeyInvertY = "Setting_InvertY";
        #endregion

        #region Properties
        // General
        public static int DisplayModeIndex { get; private set; } = 0; // 0 = Fullscreen, 1 = Borderless, 2 = Windowed
        public static int ResolutionIndex { get; private set; } = 1;
        public static int FpsLimitIndex { get; private set; } = 4; // 0=60, 1=120, 2=165, 3=240, 4=Unlimited
        public static bool ShowFps { get; private set; } = false;

        // Game
        public static bool ShowCrosshair { get; private set; } = true;
        public static bool ShowHitmarkers { get; private set; } = true;
        public static bool ShowDamageNumbers { get; private set; } = true;

        // Graphics
        public static int QualityIndex { get; private set; } = 1; // 0=Low, 1=Medium, 2=High
        public static bool VSyncEnabled { get; private set; } = false;
        public static bool ShadowsEnabled { get; private set; } = true;
        public static bool AntiAliasingEnabled { get; private set; } = true;
        public static float FOV { get; private set; } = 80f;
        public static float ViewDistance { get; private set; } = 500f;

        // Audio
        public static float MasterVolume { get; private set; } = 0.8f;
        public static float SfxVolume { get; private set; } = 0.8f;
        public static float MusicVolume { get; private set; } = 0.6f;
        public static float VoiceVolume { get; private set; } = 0.8f;

        // Controls
        public static float MouseSensitivity { get; private set; } = 2.0f;
        public static float ADSSensitivity { get; private set; } = 0.8f;
        public static bool InvertY { get; private set; } = false;
        #endregion

        #region Events
        public static event Action OnSettingsChanged;
        public static event Action<float> OnFOVChanged;
        public static event Action<float> OnViewDistanceChanged;
        public static event Action<bool> OnCrosshairToggled;
        public static event Action<bool> OnShowFpsToggled;
        #endregion

        private static readonly int[] FpsCaps = { 60, 120, 165, 240, -1 };
        private static readonly Vector2Int[] SupportedResolutions = {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("SettingsManager");
                Instance = go.AddComponent<SettingsManager>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllSettings();
                ApplyAllSettings();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        #region Load & Save
        public static void LoadAllSettings()
        {
            // General
            DisplayModeIndex = PlayerPrefs.GetInt(KeyDisplayMode, 0);
            ResolutionIndex = PlayerPrefs.GetInt(KeyResolution, 1);
            FpsLimitIndex = PlayerPrefs.GetInt(KeyFpsLimit, 4);
            ShowFps = PlayerPrefs.GetInt(KeyShowFps, 0) == 1;

            // Game
            ShowCrosshair = PlayerPrefs.GetInt(KeyCrosshair, 1) == 1;
            ShowHitmarkers = PlayerPrefs.GetInt(KeyHitmarkers, 1) == 1;
            ShowDamageNumbers = PlayerPrefs.GetInt(KeyDamageNumbers, 1) == 1;

            // Graphics
            QualityIndex = PlayerPrefs.GetInt(KeyQuality, 1);
            VSyncEnabled = PlayerPrefs.GetInt(KeyVsync, 0) == 1;
            ShadowsEnabled = PlayerPrefs.GetInt(KeyShadows, 1) == 1;
            AntiAliasingEnabled = PlayerPrefs.GetInt(KeyAntiAliasing, 1) == 1;
            FOV = PlayerPrefs.GetFloat(KeyFOV, 80f);
            ViewDistance = PlayerPrefs.GetFloat(KeyViewDistance, 500f);

            // Audio (backward-compatible with old MasterVolume key)
            MasterVolume = PlayerPrefs.GetFloat("MasterVolume", PlayerPrefs.GetFloat(KeyMasterVol, 0.8f));
            if (MasterVolume <= 0.001f && !PlayerPrefs.HasKey(KeyMasterVol)) MasterVolume = 0.8f;
            SfxVolume = PlayerPrefs.GetFloat(KeySfxVol, 0.8f);
            MusicVolume = PlayerPrefs.GetFloat(KeyMusicVol, 0.6f);
            VoiceVolume = PlayerPrefs.GetFloat(KeyVoiceVol, 0.8f);

            // Controls (backward-compatible with old MouseSensitivity key)
            MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", PlayerPrefs.GetFloat(KeyMouseSens, 2.0f));
            if (MouseSensitivity < 0.2f) MouseSensitivity = 2.0f;
            ADSSensitivity = PlayerPrefs.GetFloat(KeyAdsSens, 0.8f);
            InvertY = PlayerPrefs.GetInt(KeyInvertY, 0) == 1;
        }

        public static void ApplyAllSettings()
        {
            ApplyDisplayMode(DisplayModeIndex);
            ApplyResolution(ResolutionIndex);
            ApplyFpsLimit(FpsLimitIndex);
            ApplyShowFps(ShowFps);

            ApplyQuality(QualityIndex);
            ApplyVSync(VSyncEnabled);
            ApplyShadows(ShadowsEnabled);
            ApplyAntiAliasing(AntiAliasingEnabled);
            ApplyFOV(FOV);
            ApplyViewDistance(ViewDistance);

            ApplyMasterVolume(MasterVolume);
            ApplySfxVolume(SfxVolume);
            ApplyMusicVolume(MusicVolume);
            ApplyVoiceVolume(VoiceVolume);

            OnSettingsChanged?.Invoke();
        }

        public static void ResetToDefaults()
        {
            SetDisplayMode(0);
            SetResolution(1);
            SetFpsLimit(4);
            SetShowFps(false);

            SetCrosshair(true);
            SetHitmarkers(true);
            SetDamageNumbers(true);

            SetQuality(1);
            SetVSync(false);
            SetShadows(true);
            SetAntiAliasing(true);
            SetFOV(80f);
            SetViewDistance(500f);

            SetMasterVolume(0.8f);
            SetSfxVolume(0.8f);
            SetMusicVolume(0.6f);
            SetVoiceVolume(0.8f);

            SetMouseSensitivity(2.0f);
            SetADSSensitivity(0.8f);
            SetInvertY(false);

            KeybindManager.ResetAllKeybinds();
            PlayerPrefs.Save();
        }
        #endregion

        #region Setters & Appliers

        // --- General ---
        public static void SetDisplayMode(int index)
        {
            DisplayModeIndex = Mathf.Clamp(index, 0, 2);
            PlayerPrefs.SetInt(KeyDisplayMode, DisplayModeIndex);
            PlayerPrefs.Save();
            ApplyDisplayMode(DisplayModeIndex);
        }

        private static void ApplyDisplayMode(int index)
        {
            FullScreenMode mode = index switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };

            var res = GetResolution(ResolutionIndex);
            Screen.SetResolution(res.x, res.y, mode);
        }

        public static void SetResolution(int index)
        {
            ResolutionIndex = Mathf.Clamp(index, 0, SupportedResolutions.Length - 1);
            PlayerPrefs.SetInt(KeyResolution, ResolutionIndex);
            PlayerPrefs.Save();
            ApplyResolution(ResolutionIndex);
        }

        private static void ApplyResolution(int index)
        {
            var res = GetResolution(index);
            FullScreenMode mode = DisplayModeIndex switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => Screen.fullScreenMode
            };
            Screen.SetResolution(res.x, res.y, mode);
        }

        private static Vector2Int GetResolution(int index)
        {
            if (index >= 0 && index < SupportedResolutions.Length)
                return SupportedResolutions[index];
            return new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        public static void SetFpsLimit(int index)
        {
            FpsLimitIndex = Mathf.Clamp(index, 0, FpsCaps.Length - 1);
            PlayerPrefs.SetInt(KeyFpsLimit, FpsLimitIndex);
            PlayerPrefs.Save();
            ApplyFpsLimit(FpsLimitIndex);
        }

        private static void ApplyFpsLimit(int index)
        {
            int cap = (index >= 0 && index < FpsCaps.Length) ? FpsCaps[index] : -1;
            Application.targetFrameRate = cap;
        }

        public static void SetShowFps(bool show)
        {
            ShowFps = show;
            PlayerPrefs.SetInt(KeyShowFps, show ? 1 : 0);
            PlayerPrefs.Save();
            ApplyShowFps(show);
        }

        private static void ApplyShowFps(bool show)
        {
            OnShowFpsToggled?.Invoke(show);
            if (FPSDisplay.Instance != null)
            {
                FPSDisplay.Instance.SetVisible(show);
            }
        }

        // --- Game ---
        public static void SetCrosshair(bool show)
        {
            ShowCrosshair = show;
            PlayerPrefs.SetInt(KeyCrosshair, show ? 1 : 0);
            PlayerPrefs.Save();
            OnCrosshairToggled?.Invoke(show);
        }

        public static void SetHitmarkers(bool show)
        {
            ShowHitmarkers = show;
            PlayerPrefs.SetInt(KeyHitmarkers, show ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetDamageNumbers(bool show)
        {
            ShowDamageNumbers = show;
            PlayerPrefs.SetInt(KeyDamageNumbers, show ? 1 : 0);
            PlayerPrefs.Save();
        }

        // --- Graphics ---
        public static void SetQuality(int index)
        {
            QualityIndex = Mathf.Clamp(index, 0, 2);
            PlayerPrefs.SetInt(KeyQuality, QualityIndex);
            PlayerPrefs.Save();
            ApplyQuality(QualityIndex);
        }

        private static void ApplyQuality(int index)
        {
            int totalLevels = QualitySettings.names.Length;
            if (totalLevels > 0)
            {
                int targetLevel = Mathf.Clamp(index, 0, totalLevels - 1);
                QualitySettings.SetQualityLevel(targetLevel, true);
            }
        }

        public static void SetVSync(bool enable)
        {
            VSyncEnabled = enable;
            PlayerPrefs.SetInt(KeyVsync, enable ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVSync(enable);
        }

        private static void ApplyVSync(bool enable)
        {
            QualitySettings.vSyncCount = enable ? 1 : 0;
        }

        public static void SetShadows(bool enable)
        {
            ShadowsEnabled = enable;
            PlayerPrefs.SetInt(KeyShadows, enable ? 1 : 0);
            PlayerPrefs.Save();
            ApplyShadows(enable);
        }

        private static void ApplyShadows(bool enable)
        {
            QualitySettings.shadows = enable ? ShadowQuality.All : ShadowQuality.Disable;
        }

        public static void SetAntiAliasing(bool enable)
        {
            AntiAliasingEnabled = enable;
            PlayerPrefs.SetInt(KeyAntiAliasing, enable ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAntiAliasing(enable);
        }

        private static void ApplyAntiAliasing(bool enable)
        {
            QualitySettings.antiAliasing = enable ? 4 : 0;
        }

        public static void SetFOV(float fov)
        {
            FOV = Mathf.Clamp(fov, 60f, 110f);
            PlayerPrefs.SetFloat(KeyFOV, FOV);
            PlayerPrefs.Save();
            ApplyFOV(FOV);
        }

        private static void ApplyFOV(float fov)
        {
            OnFOVChanged?.Invoke(fov);
            if (Camera.main != null)
            {
                Camera.main.fieldOfView = fov;
            }
        }

        public static void SetViewDistance(float distance)
        {
            ViewDistance = Mathf.Clamp(distance, 100f, 1000f);
            PlayerPrefs.SetFloat(KeyViewDistance, ViewDistance);
            PlayerPrefs.Save();
            ApplyViewDistance(ViewDistance);
        }

        private static void ApplyViewDistance(float distance)
        {
            OnViewDistanceChanged?.Invoke(distance);
            if (Camera.main != null)
            {
                Camera.main.farClipPlane = distance;
            }
        }

        // --- Audio ---
        public static void SetMasterVolume(float vol)
        {
            MasterVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
            PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
            PlayerPrefs.Save();
            ApplyMasterVolume(MasterVolume);
        }

        private static void ApplyMasterVolume(float vol)
        {
            AudioListener.volume = vol;
        }

        private static void ApplySfxVolume(float vol) { }
        private static void ApplyMusicVolume(float vol) { }
        private static void ApplyVoiceVolume(float vol) { }

        public static void SetSfxVolume(float vol)
        {
            SfxVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KeySfxVol, SfxVolume);
            PlayerPrefs.Save();
        }

        public static void SetMusicVolume(float vol)
        {
            MusicVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KeyMusicVol, MusicVolume);
            PlayerPrefs.Save();
        }

        public static void SetVoiceVolume(float vol)
        {
            VoiceVolume = Mathf.Clamp01(vol);
            PlayerPrefs.SetFloat(KeyVoiceVol, VoiceVolume);
            PlayerPrefs.Save();
        }

        // --- Controls ---
        public static void SetMouseSensitivity(float sens)
        {
            MouseSensitivity = Mathf.Clamp(sens, 0.2f, 5.0f);
            PlayerPrefs.SetFloat(KeyMouseSens, MouseSensitivity);
            PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
            PlayerPrefs.Save();
        }

        public static void SetADSSensitivity(float sens)
        {
            ADSSensitivity = Mathf.Clamp(sens, 0.1f, 2.0f);
            PlayerPrefs.SetFloat(KeyAdsSens, ADSSensitivity);
            PlayerPrefs.Save();
        }

        public static void SetInvertY(bool invert)
        {
            InvertY = invert;
            PlayerPrefs.SetInt(KeyInvertY, invert ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion
    }
}
