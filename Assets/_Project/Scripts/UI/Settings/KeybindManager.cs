using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SniperGame.UI.Settings
{
    public enum KeyAction
    {
        Move,
        Jump,
        Crouch,
        Slide,
        WallJump,
        Shoot,
        Aim,
        Reload
    }

    public class KeybindManager : MonoBehaviour
    {
        public static KeybindManager Instance { get; private set; }

        private static readonly Dictionary<KeyAction, KeyCode> DefaultKeybinds = new()
        {
            { KeyAction.Move, KeyCode.None }, // Composite WASD
            { KeyAction.Jump, KeyCode.Space },
            { KeyAction.Crouch, KeyCode.C },
            { KeyAction.Slide, KeyCode.C },
            { KeyAction.WallJump, KeyCode.Space },
            { KeyAction.Shoot, KeyCode.Mouse0 },
            { KeyAction.Aim, KeyCode.Mouse1 },
            { KeyAction.Reload, KeyCode.R }
        };

        private static Dictionary<KeyAction, KeyCode> _currentKeybinds = new();

        public static bool IsRebinding { get; private set; } = false;
        private Coroutine _rebindingRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllKeybinds();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static void LoadAllKeybinds()
        {
            _currentKeybinds.Clear();
            foreach (var kvp in DefaultKeybinds)
            {
                string prefKey = $"Keybind_{kvp.Key}";
                if (PlayerPrefs.HasKey(prefKey))
                {
                    string savedKey = PlayerPrefs.GetString(prefKey);
                    if (Enum.TryParse(savedKey, out KeyCode parsedKey))
                    {
                        _currentKeybinds[kvp.Key] = parsedKey;
                        continue;
                    }
                }
                _currentKeybinds[kvp.Key] = kvp.Value;
            }
        }

        public static void ResetAllKeybinds()
        {
            foreach (var kvp in DefaultKeybinds)
            {
                _currentKeybinds[kvp.Key] = kvp.Value;
                PlayerPrefs.DeleteKey($"Keybind_{kvp.Key}");
            }
            PlayerPrefs.Save();
        }

        public static KeyCode GetKeyCode(KeyAction action)
        {
            if (_currentKeybinds.Count == 0) LoadAllKeybinds();
            return _currentKeybinds.TryGetValue(action, out var key) ? key : DefaultKeybinds[action];
        }

        public static bool GetKey(KeyAction action)
        {
            KeyCode code = GetKeyCode(action);
            return code != KeyCode.None && Input.GetKey(code);
        }

        public static bool GetKeyDown(KeyAction action)
        {
            KeyCode code = GetKeyCode(action);
            return code != KeyCode.None && Input.GetKeyDown(code);
        }

        public static bool GetKeyUp(KeyAction action)
        {
            KeyCode code = GetKeyCode(action);
            return code != KeyCode.None && Input.GetKeyUp(code);
        }

        public static string GetKeyDisplayName(KeyAction action)
        {
            if (action == KeyAction.Move) return "W, A, S, D";
            KeyCode code = GetKeyCode(action);
            return FormatKeyCode(code);
        }

        public static string FormatKeyCode(KeyCode code)
        {
            return code switch
            {
                KeyCode.Mouse0 => "LMB",
                KeyCode.Mouse1 => "RMB",
                KeyCode.Mouse2 => "MMB",
                KeyCode.Space => "SPACE",
                KeyCode.LeftShift => "SHIFT",
                KeyCode.RightShift => "R-SHIFT",
                KeyCode.LeftControl => "CTRL",
                KeyCode.RightControl => "R-CTRL",
                KeyCode.LeftAlt => "ALT",
                KeyCode.RightAlt => "R-ALT",
                KeyCode.Return => "ENTER",
                KeyCode.BackQuote => "~",
                _ => code.ToString().ToUpper()
            };
        }

        public void StartRebinding(KeyAction action, TextMeshProUGUI buttonText, Action onDone = null)
        {
            if (action == KeyAction.Move)
            {
                // Composite move doesn't remap to single key
                return;
            }

            if (_rebindingRoutine != null)
            {
                StopCoroutine(_rebindingRoutine);
            }

            _rebindingRoutine = StartCoroutine(RebindRoutine(action, buttonText, onDone));
        }

        private IEnumerator RebindRoutine(KeyAction action, TextMeshProUGUI buttonText, Action onDone)
        {
            IsRebinding = true;
            string originalText = buttonText != null ? buttonText.text : GetKeyDisplayName(action);
            if (buttonText != null)
            {
                buttonText.text = "< PRESS KEY >";
                buttonText.color = new Color(1f, 0.7f, 0.1f); // Accent yellow
            }

            // Wait one frame to avoid capturing the button click
            yield return null;

            bool captured = false;
            while (!captured)
            {
                // Escape cancels
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (buttonText != null)
                    {
                        buttonText.text = originalText;
                        buttonText.color = new Color(1f, 0.85f, 0.2f, 1f); // Cyber Gold
                    }
                    IsRebinding = false;
                    onDone?.Invoke();
                    yield break;
                }

                // Check mouse buttons
                for (int m = 0; m <= 2; m++)
                {
                    if (Input.GetMouseButtonDown(m))
                    {
                        KeyCode mouseKey = m == 0 ? KeyCode.Mouse0 : (m == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2);
                        ApplyKeybind(action, mouseKey, buttonText);
                        captured = true;
                        break;
                    }
                }

                if (captured) break;

                // Check keyboard keys
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                    {
                        // Exclude mouse codes (handled above) and invalid keys
                        if (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue;
                        if (k == KeyCode.None) continue;

                        if (Input.GetKeyDown(k))
                        {
                            ApplyKeybind(action, k, buttonText);
                            captured = true;
                            break;
                        }
                    }
                }

                yield return null;
            }

            IsRebinding = false;
            onDone?.Invoke();
        }

        private void ApplyKeybind(KeyAction action, KeyCode newKey, TextMeshProUGUI buttonText)
        {
            _currentKeybinds[action] = newKey;
            PlayerPrefs.SetString($"Keybind_{action}", newKey.ToString());
            PlayerPrefs.Save();

            if (buttonText != null)
            {
                buttonText.text = FormatKeyCode(newKey);
                buttonText.color = new Color(1f, 0.85f, 0.2f, 1f); // Cyber Gold
            }
        }
    }
}
