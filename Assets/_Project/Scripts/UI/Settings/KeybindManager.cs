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
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
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
            { KeyAction.MoveForward, KeyCode.W },
            { KeyAction.MoveBackward, KeyCode.S },
            { KeyAction.MoveLeft, KeyCode.A },
            { KeyAction.MoveRight, KeyCode.D },
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

        private TextMeshProUGUI _activeButtonText;
        private TextMeshProUGUI _activeActionLabel;
        private string _activeOriginalBtnText;
        private string _activeOriginalLabelText;
        private RectTransform _activeBtnRect;
        private UnityEngine.UI.Image _activeBtnImg;
        private UnityEngine.UI.Image _activeBorderImg;

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

        private void OnDisable()
        {
            CleanupActiveRebind();
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

        public static float GetHorizontalAxisRaw()
        {
            bool right = GetKey(KeyAction.MoveRight);
            bool left = GetKey(KeyAction.MoveLeft);
            if (right || left)
            {
                float val = 0f;
                if (right) val += 1f;
                if (left) val -= 1f;
                return val;
            }
            return Input.GetAxisRaw("Horizontal");
        }

        public static float GetVerticalAxisRaw()
        {
            bool forward = GetKey(KeyAction.MoveForward);
            bool backward = GetKey(KeyAction.MoveBackward);
            if (forward || backward)
            {
                float val = 0f;
                if (forward) val += 1f;
                if (backward) val -= 1f;
                return val;
            }
            return Input.GetAxisRaw("Vertical");
        }

        public static string GetKeyDisplayName(KeyAction action)
        {
            if (action == KeyAction.Move)
            {
                KeyCode f = GetKeyCode(KeyAction.MoveForward);
                KeyCode l = GetKeyCode(KeyAction.MoveLeft);
                KeyCode b = GetKeyCode(KeyAction.MoveBackward);
                KeyCode r = GetKeyCode(KeyAction.MoveRight);
                return $"{FormatKeyCode(f)}, {FormatKeyCode(l)}, {FormatKeyCode(b)}, {FormatKeyCode(r)}";
            }
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
                KeyCode.KeypadEnter => "ENTER",
                KeyCode.BackQuote => "~",
                KeyCode.UpArrow => "UP",
                KeyCode.DownArrow => "DOWN",
                KeyCode.LeftArrow => "LEFT",
                KeyCode.RightArrow => "RIGHT",
                _ => code.ToString().ToUpper()
            };
        }

        public void CancelRebinding()
        {
            CleanupActiveRebind();
        }

        private void CleanupActiveRebind()
        {
            if (_rebindingRoutine != null)
            {
                StopCoroutine(_rebindingRoutine);
                _rebindingRoutine = null;
            }

            if (_activeButtonText != null && !string.IsNullOrEmpty(_activeOriginalBtnText))
            {
                _activeButtonText.text = _activeOriginalBtnText;
                _activeButtonText.color = new Color(1f, 0.85f, 0.2f, 1f);
            }
            if (_activeActionLabel != null && !string.IsNullOrEmpty(_activeOriginalLabelText))
            {
                _activeActionLabel.text = _activeOriginalLabelText;
                _activeActionLabel.color = new Color(0.81f, 0.84f, 0.90f, 1f);
            }
            if (_activeBtnRect != null) _activeBtnRect.localScale = Vector3.one;
            if (_activeBtnImg != null) _activeBtnImg.color = new Color(0.086f, 0.098f, 0.145f, 0.95f);
            if (_activeBorderImg != null) _activeBorderImg.color = new Color(0.25f, 0.42f, 0.52f, 0.65f);

            _activeButtonText = null;
            _activeActionLabel = null;
            _activeOriginalBtnText = null;
            _activeOriginalLabelText = null;
            _activeBtnRect = null;
            _activeBtnImg = null;
            _activeBorderImg = null;

            IsRebinding = false;
        }

        public void StartRebinding(KeyAction action, TextMeshProUGUI buttonText, Action onDone = null)
        {
            CleanupActiveRebind();

            if (action == KeyAction.Move)
            {
                _rebindingRoutine = StartCoroutine(RebindMoveRoutine(buttonText, onDone));
            }
            else
            {
                _rebindingRoutine = StartCoroutine(RebindRoutine(action, buttonText, onDone));
            }
        }

        private IEnumerator RebindMoveRoutine(TextMeshProUGUI buttonText, Action onDone)
        {
            IsRebinding = true;
            KeyCode origForward = GetKeyCode(KeyAction.MoveForward);
            KeyCode origLeft = GetKeyCode(KeyAction.MoveLeft);
            KeyCode origBackward = GetKeyCode(KeyAction.MoveBackward);
            KeyCode origRight = GetKeyCode(KeyAction.MoveRight);

            string origBtnText = buttonText != null ? buttonText.text : GetKeyDisplayName(KeyAction.Move);

            RectTransform btnRect = buttonText != null && buttonText.transform.parent != null
                ? buttonText.transform.parent.GetComponent<RectTransform>()
                : null;
            UnityEngine.UI.Image btnImg = btnRect != null ? btnRect.GetComponent<UnityEngine.UI.Image>() : null;
            Transform borderTr = btnRect != null ? btnRect.Find("KeyButtonBorder") : null;
            UnityEngine.UI.Image borderImg = borderTr != null ? borderTr.GetComponent<UnityEngine.UI.Image>() : null;

            Transform rowTr = btnRect != null ? btnRect.parent : null;
            TextMeshProUGUI actionLabel = null;
            string origLabelText = "MOVE";
            if (rowTr != null)
            {
                var labelTr = rowTr.Find("ActionLabel") ?? rowTr.Find("Action_Label");
                if (labelTr != null) actionLabel = labelTr.GetComponent<TextMeshProUGUI>();
                if (actionLabel != null) origLabelText = actionLabel.text;
            }

            _activeButtonText = buttonText;
            _activeActionLabel = actionLabel;
            _activeOriginalBtnText = origBtnText;
            _activeOriginalLabelText = origLabelText;
            _activeBtnRect = btnRect;
            _activeBtnImg = btnImg;
            _activeBorderImg = borderImg;

            Color idleBoxBg = new Color(0.086f, 0.098f, 0.145f, 0.95f);
            Color activeBoxBg = new Color(0.24f, 0.20f, 0.08f, 1f);
            Color idleBorderCol = new Color(0.25f, 0.42f, 0.52f, 0.65f);
            Color goldCol = new Color(1f, 0.85f, 0.2f, 1f);

            if (btnImg != null) btnImg.color = activeBoxBg;
            if (borderImg != null) borderImg.color = new Color(1f, 0.85f, 0.22f, 1f);

            // Wait one frame to avoid capturing the triggering mouse click
            yield return null;
            while (Input.GetMouseButton(0))
            {
                yield return null;
            }

            var steps = new (KeyAction action, string labelName, string promptName)[]
            {
                (KeyAction.MoveForward, "FORWARD", "< FORWARD >"),
                (KeyAction.MoveLeft, "LEFT", "< LEFT >"),
                (KeyAction.MoveBackward, "BACKWARD", "< BACK >"),
                (KeyAction.MoveRight, "RIGHT", "< RIGHT >")
            };

            for (int sIdx = 0; sIdx < steps.Length; sIdx++)
            {
                var step = steps[sIdx];
                if (actionLabel != null)
                {
                    actionLabel.text = step.labelName;
                    actionLabel.color = new Color(1f, 0.92f, 0.42f, 1f);
                }
                if (buttonText != null)
                {
                    buttonText.text = step.promptName;
                    buttonText.color = new Color(1f, 0.88f, 0.35f, 1f);
                }

                bool captured = false;
                float pulseTimer = 0f;

                while (!captured)
                {
                    pulseTimer += Time.unscaledDeltaTime * 7.5f;
                    float wave = 0.5f + 0.5f * Mathf.Sin(pulseTimer);

                    if (buttonText != null)
                    {
                        buttonText.color = Color.Lerp(new Color(1f, 0.65f, 0.10f, 0.78f), new Color(1f, 0.92f, 0.42f, 1f), wave);
                    }
                    if (btnRect != null)
                    {
                        float s = Mathf.Lerp(1.0f, 1.055f, wave);
                        btnRect.localScale = new Vector3(s, s, 1f);
                    }
                    if (btnImg != null)
                    {
                        btnImg.color = Color.Lerp(new Color(0.16f, 0.13f, 0.06f, 0.98f), activeBoxBg, wave);
                    }
                    if (borderImg != null)
                    {
                        borderImg.color = Color.Lerp(new Color(1f, 0.72f, 0.12f, 0.75f), new Color(1f, 0.95f, 0.48f, 1f), wave);
                    }

                    // Escape cancels entire sequence
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        _currentKeybinds[KeyAction.MoveForward] = origForward;
                        _currentKeybinds[KeyAction.MoveLeft] = origLeft;
                        _currentKeybinds[KeyAction.MoveBackward] = origBackward;
                        _currentKeybinds[KeyAction.MoveRight] = origRight;

                        CleanupActiveRebind();
                        onDone?.Invoke();
                        yield break;
                    }

                    // Enter/Return retains current key for this direction and advances
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        captured = true;
                        break;
                    }

                    // Check mouse buttons
                    for (int m = 0; m <= 2; m++)
                    {
                        if (Input.GetMouseButtonDown(m))
                        {
                            KeyCode mouseKey = m == 0 ? KeyCode.Mouse0 : (m == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2);
                            _currentKeybinds[step.action] = mouseKey;
                            PlayerPrefs.SetString($"Keybind_{step.action}", mouseKey.ToString());
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
                            if (k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue;
                            if (k == KeyCode.None || k == KeyCode.Escape) continue;

                            if (Input.GetKeyDown(k))
                            {
                                _currentKeybinds[step.action] = k;
                                PlayerPrefs.SetString($"Keybind_{step.action}", k.ToString());
                                captured = true;
                                break;
                            }
                        }
                    }

                    yield return null;
                }

                // Wait until key/mouse is released before next step
                while (Input.anyKey || Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
                {
                    yield return null;
                }
            }

            PlayerPrefs.Save();

            if (actionLabel != null)
            {
                actionLabel.text = origLabelText;
                actionLabel.color = new Color(0.81f, 0.84f, 0.90f, 1f);
            }
            if (buttonText != null)
            {
                buttonText.text = GetKeyDisplayName(KeyAction.Move);
                buttonText.color = goldCol;
            }
            if (btnImg != null) btnImg.color = idleBoxBg;
            if (borderImg != null) borderImg.color = idleBorderCol;
            if (btnRect != null)
            {
                StartCoroutine(ConfirmPopRoutine(btnRect));
            }

            _activeButtonText = null;
            _activeActionLabel = null;
            _activeOriginalBtnText = null;
            _activeOriginalLabelText = null;
            _activeBtnRect = null;
            _activeBtnImg = null;
            _activeBorderImg = null;

            IsRebinding = false;
            _rebindingRoutine = null;
            onDone?.Invoke();
        }

        private IEnumerator RebindRoutine(KeyAction action, TextMeshProUGUI buttonText, Action onDone)
        {
            IsRebinding = true;
            string originalText = buttonText != null ? buttonText.text : GetKeyDisplayName(action);
            RectTransform btnRect = buttonText != null && buttonText.transform.parent != null
                ? buttonText.transform.parent.GetComponent<RectTransform>()
                : null;
            UnityEngine.UI.Image btnImg = btnRect != null ? btnRect.GetComponent<UnityEngine.UI.Image>() : null;
            Transform borderTr = btnRect != null ? btnRect.Find("KeyButtonBorder") : null;
            UnityEngine.UI.Image borderImg = borderTr != null ? borderTr.GetComponent<UnityEngine.UI.Image>() : null;

            _activeButtonText = buttonText;
            _activeOriginalBtnText = originalText;
            _activeBtnRect = btnRect;
            _activeBtnImg = btnImg;
            _activeBorderImg = borderImg;

            Color idleBoxBg = new Color(0.086f, 0.098f, 0.145f, 0.95f);
            Color activeBoxBg = new Color(0.24f, 0.20f, 0.08f, 1f);
            Color idleBorderCol = new Color(0.25f, 0.42f, 0.52f, 0.65f);

            if (buttonText != null)
            {
                buttonText.text = "< PRESS KEY >";
                buttonText.color = new Color(1f, 0.88f, 0.35f, 1f);
            }
            if (btnImg != null)
            {
                btnImg.color = activeBoxBg;
            }
            if (borderImg != null)
            {
                borderImg.color = new Color(1f, 0.85f, 0.22f, 1f);
            }

            // Wait one frame to avoid capturing the button click
            yield return null;

            bool captured = false;
            float pulseTimer = 0f;
            while (!captured)
            {
                pulseTimer += Time.unscaledDeltaTime * 7.5f;
                float wave = 0.5f + 0.5f * Mathf.Sin(pulseTimer);

                if (buttonText != null)
                {
                    buttonText.color = Color.Lerp(new Color(1f, 0.65f, 0.10f, 0.78f), new Color(1f, 0.92f, 0.42f, 1f), wave);
                }
                if (btnRect != null)
                {
                    float s = Mathf.Lerp(1.0f, 1.055f, wave);
                    btnRect.localScale = new Vector3(s, s, 1f);
                }
                if (btnImg != null)
                {
                    btnImg.color = Color.Lerp(new Color(0.16f, 0.13f, 0.06f, 0.98f), activeBoxBg, wave);
                }
                if (borderImg != null)
                {
                    borderImg.color = Color.Lerp(new Color(1f, 0.72f, 0.12f, 0.75f), new Color(1f, 0.95f, 0.48f, 1f), wave);
                }

                // Escape cancels
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CleanupActiveRebind();
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
                        if (k == KeyCode.None || k == KeyCode.Escape) continue;

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

            if (btnImg != null) btnImg.color = idleBoxBg;
            if (borderImg != null) borderImg.color = idleBorderCol;
            if (btnRect != null)
            {
                StartCoroutine(ConfirmPopRoutine(btnRect));
            }

            _activeButtonText = null;
            _activeOriginalBtnText = null;
            _activeBtnRect = null;
            _activeBtnImg = null;
            _activeBorderImg = null;

            IsRebinding = false;
            _rebindingRoutine = null;
            onDone?.Invoke();
        }

        private IEnumerator ConfirmPopRoutine(RectTransform targetRect)
        {
            if (targetRect == null) yield break;
            float duration = 0.14f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (targetRect == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = Mathf.Lerp(1.12f, 1.0f, Mathf.Sin(t * Mathf.PI * 0.5f));
                targetRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (targetRect != null) targetRect.localScale = Vector3.one;
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
