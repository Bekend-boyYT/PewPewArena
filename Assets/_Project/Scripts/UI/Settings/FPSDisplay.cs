using TMPro;
using UnityEngine;

namespace SniperGame.UI.Settings
{
    public class FPSDisplay : MonoBehaviour
    {
        public static FPSDisplay Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI customFpsText;
        [SerializeField] private bool showOnGUI = true;

        private float _deltaTime = 0f;
        private bool _isVisible = false;
        private GUIStyle _guiStyle;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _isVisible = SettingsManager.ShowFps;
            SettingsManager.OnShowFpsToggled += SetVisible;
        }

        private void OnDestroy()
        {
            SettingsManager.OnShowFpsToggled -= SetVisible;
        }

        private void Update()
        {
            if (!_isVisible) return;

            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            if (customFpsText != null)
            {
                float fps = 1.0f / _deltaTime;
                customFpsText.text = $"{Mathf.CeilToInt(fps)} FPS";
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (customFpsText != null)
            {
                customFpsText.gameObject.SetActive(visible);
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || !showOnGUI || customFpsText != null) return;

            if (_guiStyle == null)
            {
                _guiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
                _guiStyle.normal.textColor = new Color(0.2f, 1.0f, 0.4f, 0.9f); // Bright clean green
            }

            float fps = 1.0f / _deltaTime;
            GUI.Label(new Rect(15, 15, 150, 30), $"{Mathf.CeilToInt(fps)} FPS", _guiStyle);
        }
    }
}
