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
        private GUIStyle _boxStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var fpsGo = new GameObject("FPSDisplay");
                Instance = fpsGo.AddComponent<FPSDisplay>();
                DontDestroyOnLoad(fpsGo);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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
                float fps = 1.0f / Mathf.Max(_deltaTime, 0.0001f);
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

            GUI.depth = -100; // Always render on top of all canvases and panels

            if (_guiStyle == null)
            {
                _guiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _guiStyle.normal.textColor = new Color(0.2f, 1.0f, 0.4f, 0.95f); // Bright clean green
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(0.06f, 0.07f, 0.10f, 0.82f));
                tex.Apply();
                _boxStyle.normal.background = tex;
            }

            float fps = 1.0f / Mathf.Max(_deltaTime, 0.0001f);
            int fpsInt = Mathf.CeilToInt(fps);

            GUI.Box(new Rect(14, 14, 92, 26), GUIContent.none, _boxStyle);
            GUI.Label(new Rect(14, 14, 92, 26), $"{fpsInt} FPS", _guiStyle);
        }
    }
}
