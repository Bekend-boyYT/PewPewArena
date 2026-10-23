using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Delivers the redesigned, sci-fi Join Game screen matching the mockup:
    /// - Glowing "JOIN GAME" title flanked by dual cyan horizontal laser beams.
    /// - Uppercase subtitle "ENTER THE 6-CHARACTER ROOM CODE."
    /// - 4-corner chamfered dark glass card container with subtle cyan wireframe border.
    /// - Sci-fi input field with "ENTER CODE..." placeholder and live "0/6" character counter.
    /// - Glowing cyan JOIN button inside the container preserving all existing button animations & hover arrows.
    /// - Centered BACK button below the container.
    /// - Ethereal ambient emerald/teal atmosphere glows on left and right screen edges.
    /// </summary>
    [DisallowMultipleComponent]
    public class JoinPanelTheme : MonoBehaviour
    {
        private static readonly Color CyanAccent = new Color(0f, 0.88f, 1f, 1f);
        private static readonly Color CyanLaser = new Color(0f, 0.90f, 1f, 0.95f);
        private static readonly Color CyanWireframe = new Color(0f, 0.82f, 0.95f, 0.40f);
        private static readonly Color CyanInputFrame = new Color(0f, 0.85f, 1f, 0.85f);
        private static readonly Color CardBg = new Color(0.035f, 0.07f, 0.11f, 0.85f);
        private static readonly Color InputBg = new Color(0.02f, 0.045f, 0.075f, 0.95f);
        private static readonly Color SubtitleColor = new Color(0.50f, 0.65f, 0.72f, 1f);
        private static readonly Color CounterIdleColor = new Color(0.35f, 0.55f, 0.65f, 0.85f);
        private static readonly Color CounterFullColor = new Color(0f, 0.95f, 1f, 1f);
        private static readonly Color AtmosphereColor = new Color(0.02f, 0.70f, 0.45f, 0.16f);

        private TMP_FontAsset _font;
        private TMP_FontAsset _codeFont;
        private TMP_InputField _inputField;
        private TextMeshProUGUI _counterText;
        private Button _joinConfirmBtn;
        private Button _joinBackBtn;
        private bool _applied = false;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
            SyncCounter();
        }

        public void Apply()
        {
            if (_applied) return;
            _applied = true;

            // Grab existing font from JoinTitle
            var titleComp = transform.Find("JoinTitle")?.GetComponent<TextMeshProUGUI>();
            if (titleComp != null && titleComp.font != null)
            {
                _font = titleComp.font;
            }

            _codeFont = GetKomikaFont();

            SetupAtmosphere();
            SetupHeader();
            SetupCardContainer();
            SetupInputField();
            SetupConfirmButton();
            SetupBackButton();
            SyncCounter();
        }

        public TMP_FontAsset GetKomikaFont()
        {
            if (_codeFont != null) return _codeFont;

            // 1. Check existing font on JoinCodeInput text component or placeholder
            Transform inputTrans = transform.Find("JoinCodeInput");
            if (inputTrans != null)
            {
                var inputField = inputTrans.GetComponent<TMP_InputField>();
                if (inputField != null && inputField.textComponent != null && inputField.textComponent.font != null)
                {
                    if (inputField.textComponent.font.name.IndexOf("Komika", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _codeFont = inputField.textComponent.font;
                        return _codeFont;
                    }
                }
                if (inputField != null && inputField.placeholder is TextMeshProUGUI ph && ph.font != null)
                {
                    if (ph.font.name.IndexOf("Komika", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _codeFont = ph.font;
                        return _codeFont;
                    }
                }
            }

            // 2. Check loaded font assets in build memory
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in allFonts)
            {
                if (f != null && f.name.IndexOf("Komika", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _codeFont = f;
                    return _codeFont;
                }
            }

            // 3. Fallback to Resources
            _codeFont = Resources.Load<TMP_FontAsset>("KomikaDemo") ?? Resources.Load<TMP_FontAsset>("Fonts/KomikaDemo");
            if (_codeFont != null) return _codeFont;

#if UNITY_EDITOR
            _codeFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro Effect/Examples/KomikaDemo.asset");
#endif
            return _codeFont;
        }

        #region Atmosphere
        private void SetupAtmosphere()
        {
            // Left ethereal glow
            var leftAtmo = GetOrCreateImage(transform, "LeftAtmosphere");
            leftAtmo.sprite = JoinUISprites.AtmosphereGlow;
            leftAtmo.color = AtmosphereColor;
            leftAtmo.raycastTarget = false;
            leftAtmo.type = Image.Type.Simple;
            var lRt = leftAtmo.rectTransform;
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0.5f, 0.5f);
            lRt.anchoredPosition = new Vector2(240f, 0f);
            lRt.sizeDelta = new Vector2(600f, 850f);
            leftAtmo.transform.SetSiblingIndex(0);

            // Right ethereal glow
            var rightAtmo = GetOrCreateImage(transform, "RightAtmosphere");
            rightAtmo.sprite = JoinUISprites.AtmosphereGlow;
            rightAtmo.color = AtmosphereColor;
            rightAtmo.raycastTarget = false;
            rightAtmo.type = Image.Type.Simple;
            var rRt = rightAtmo.rectTransform;
            rRt.anchorMin = new Vector2(1f, 0.5f);
            rRt.anchorMax = new Vector2(1f, 0.5f);
            rRt.pivot = new Vector2(0.5f, 0.5f);
            rRt.anchoredPosition = new Vector2(-240f, 0f);
            rRt.sizeDelta = new Vector2(600f, 850f);
            rightAtmo.transform.SetSiblingIndex(1);
        }
        #endregion

        #region Header
        private void SetupHeader()
        {
            Transform titleTrans = transform.Find("JoinTitle");
            if (titleTrans != null)
            {
                var titleText = titleTrans.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "JOIN GAME";
                    titleText.fontSize = 58f;
                    titleText.characterSpacing = 4f;
                    titleText.color = Color.white;
                    titleText.alignment = TextAlignmentOptions.Center;
                    titleText.raycastTarget = false;
                }

                var titleRt = titleTrans as RectTransform;
                if (titleRt != null)
                {
                    titleRt.anchorMin = new Vector2(0.5f, 0.5f);
                    titleRt.anchorMax = new Vector2(0.5f, 0.5f);
                    titleRt.pivot = new Vector2(0.5f, 0.5f);
                    titleRt.anchoredPosition = new Vector2(0f, 215f);
                    titleRt.sizeDelta = new Vector2(420f, 70f);
                }

                // Left flanking laser beam
                var leftLaser = GetOrCreateImage(titleTrans, "LeftLaserLine");
                leftLaser.sprite = JoinUISprites.LaserLine;
                leftLaser.color = CyanLaser;
                leftLaser.raycastTarget = false;
                leftLaser.type = Image.Type.Simple;
                var llRt = leftLaser.rectTransform;
                llRt.anchorMin = new Vector2(0.5f, 0.5f);
                llRt.anchorMax = new Vector2(0.5f, 0.5f);
                llRt.pivot = new Vector2(0.5f, 0.5f);
                llRt.anchoredPosition = new Vector2(-225f, 0f);
                llRt.sizeDelta = new Vector2(140f, 2.5f);
                llRt.localScale = new Vector3(-1f, 1f, 1f); // flip horizontally so it fades outward

                // Right flanking laser beam
                var rightLaser = GetOrCreateImage(titleTrans, "RightLaserLine");
                rightLaser.sprite = JoinUISprites.LaserLine;
                rightLaser.color = CyanLaser;
                rightLaser.raycastTarget = false;
                rightLaser.type = Image.Type.Simple;
                var rlRt = rightLaser.rectTransform;
                rlRt.anchorMin = new Vector2(0.5f, 0.5f);
                rlRt.anchorMax = new Vector2(0.5f, 0.5f);
                rlRt.pivot = new Vector2(0.5f, 0.5f);
                rlRt.anchoredPosition = new Vector2(225f, 0f);
                rlRt.sizeDelta = new Vector2(140f, 2.5f);
                rlRt.localScale = Vector3.one;
            }

            Transform statusTrans = transform.Find("JoinStatusText");
            if (statusTrans != null)
            {
                var statusText = statusTrans.GetComponent<TextMeshProUGUI>();
                if (statusText != null)
                {
                    statusText.fontSize = 18f;
                    statusText.characterSpacing = 2.5f;
                    statusText.fontStyle = FontStyles.UpperCase;
                    statusText.color = SubtitleColor;
                    statusText.alignment = TextAlignmentOptions.Center;
                    statusText.raycastTarget = false;
                    if (string.IsNullOrEmpty(statusText.text) || statusText.text.StartsWith("Enter", StringComparison.OrdinalIgnoreCase))
                    {
                        statusText.text = "ENTER THE 6-CHARACTER ROOM CODE.";
                    }
                }

                var statusRt = statusTrans as RectTransform;
                if (statusRt != null)
                {
                    statusRt.anchorMin = new Vector2(0.5f, 0.5f);
                    statusRt.anchorMax = new Vector2(0.5f, 0.5f);
                    statusRt.pivot = new Vector2(0.5f, 0.5f);
                    statusRt.anchoredPosition = new Vector2(0f, 155f);
                    statusRt.sizeDelta = new Vector2(700f, 36f);
                }
            }
        }
        #endregion

        #region Card Container
        private void SetupCardContainer()
        {
            var cardImg = GetOrCreateImage(transform, "JoinCardContainer");
            cardImg.sprite = JoinUISprites.CardFill;
            cardImg.color = CardBg;
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = false;

            var cardRt = cardImg.rectTransform;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -10f);
            cardRt.sizeDelta = new Vector2(520f, 225f);

            // Position card directly behind JoinCodeInput so buttons/inputs render on top
            Transform inputTrans = transform.Find("JoinCodeInput");
            if (inputTrans != null)
            {
                int inputIdx = inputTrans.GetSiblingIndex();
                cardImg.transform.SetSiblingIndex(Mathf.Max(0, inputIdx - 1));
            }

            // Outer cyan chamfered wireframe border
            var borderImg = GetOrCreateImage(cardImg.transform, "CardBorder");
            borderImg.sprite = JoinUISprites.CardFrame;
            borderImg.color = CyanWireframe;
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;
            StretchFull(borderImg.rectTransform);
        }
        #endregion

        #region Input Field
        private void SetupInputField()
        {
            Transform inputTrans = transform.Find("JoinCodeInput");
            if (inputTrans == null) return;

            var inputRt = inputTrans as RectTransform;
            if (inputRt != null)
            {
                inputRt.anchorMin = new Vector2(0.5f, 0.5f);
                inputRt.anchorMax = new Vector2(0.5f, 0.5f);
                inputRt.pivot = new Vector2(0.5f, 0.5f);
                inputRt.anchoredPosition = new Vector2(0f, 32f);
                inputRt.sizeDelta = new Vector2(450f, 58f);
            }

            // Input background
            var bgImg = inputTrans.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = JoinUISprites.InputFill;
                bgImg.color = InputBg;
                bgImg.type = Image.Type.Sliced;
                bgImg.raycastTarget = true;
            }

            // Input cyan outline
            var outlineImg = GetOrCreateImage(inputTrans, "InputOutline");
            outlineImg.sprite = JoinUISprites.InputFrame;
            outlineImg.color = CyanInputFrame;
            outlineImg.type = Image.Type.Sliced;
            outlineImg.raycastTarget = false;
            StretchFull(outlineImg.rectTransform);
            outlineImg.transform.SetSiblingIndex(0);

            _inputField = inputTrans.GetComponent<TMP_InputField>();
            if (_inputField != null)
            {
                _inputField.characterLimit = 6;
                _inputField.contentType = TMP_InputField.ContentType.Alphanumeric;

                _inputField.onValueChanged.RemoveListener(OnInputChanged);
                _inputField.onValueChanged.AddListener(OnInputChanged);

                _inputField.onSubmit.RemoveListener(OnInputSubmit);
                _inputField.onSubmit.AddListener(OnInputSubmit);

                var codeFont = GetKomikaFont();

                // Configure input text component
                if (_inputField.textComponent != null)
                {
                    if (codeFont != null)
                    {
                        _inputField.textComponent.font = codeFont;
                        _inputField.fontAsset = codeFont;
                    }
                    _inputField.textComponent.fontSize = 24f;
                    _inputField.textComponent.characterSpacing = 3f;
                    _inputField.textComponent.color = Color.white;
                    _inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
                }

                // Configure placeholder component
                if (_inputField.placeholder != null)
                {
                    var placeholderText = _inputField.placeholder as TextMeshProUGUI;
                    if (placeholderText != null)
                    {
                        if (codeFont != null)
                        {
                            placeholderText.font = codeFont;
                        }
                        placeholderText.text = "ENTER CODE...";
                        placeholderText.fontSize = 19f;
                        placeholderText.characterSpacing = 2f;
                        placeholderText.color = new Color(0.40f, 0.60f, 0.72f, 0.55f);
                        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
                    }
                }

                // Configure Text Area Viewport padding
                if (_inputField.textViewport != null)
                {
                    _inputField.textViewport.offsetMin = new Vector2(22f, 4f);
                    _inputField.textViewport.offsetMax = new Vector2(-95f, -4f);
                }
            }

            // Vertical divider before counter
            var divImg = GetOrCreateImage(inputTrans, "CounterDivider");
            divImg.sprite = JoinUISprites.White;
            divImg.color = new Color(0f, 0.85f, 1f, 0.25f);
            divImg.raycastTarget = false;
            divImg.type = Image.Type.Simple;
            var dRt = divImg.rectTransform;
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = new Vector2(132f, 0f);
            dRt.sizeDelta = new Vector2(1.5f, 30f);

            // Counter text "0/6"
            var counterGo = inputTrans.Find("CodeCounterText")?.gameObject;
            if (counterGo == null)
            {
                counterGo = new GameObject("CodeCounterText", typeof(RectTransform), typeof(TextMeshProUGUI));
                counterGo.transform.SetParent(inputTrans, false);
            }
            _counterText = counterGo.GetComponent<TextMeshProUGUI>();
            if (_counterText != null)
            {
                if (_codeFont != null) _counterText.font = _codeFont;
                else if (_font != null) _counterText.font = _font;
                _counterText.fontSize = 18f;
                _counterText.characterSpacing = 1.5f;
                _counterText.alignment = TextAlignmentOptions.MidlineRight;
                _counterText.color = CounterIdleColor;
                _counterText.raycastTarget = false;
                _counterText.text = "0/6";
            }
            var cRt = counterGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(175f, 0f);
            cRt.sizeDelta = new Vector2(65f, 40f);
        }

        private void OnInputChanged(string val)
        {
            string upper = val.ToUpperInvariant();
            if (upper != val)
            {
                _inputField.text = upper;
                return;
            }
            SyncCounter();
        }

        private void OnInputSubmit(string val)
        {
            if (val != null && val.Trim().Length == 6)
            {
                if (_joinConfirmBtn != null && _joinConfirmBtn.interactable)
                {
                    _joinConfirmBtn.onClick.Invoke();
                }
            }
        }

        private void SyncCounter()
        {
            int len = _inputField != null && _inputField.text != null ? _inputField.text.Length : 0;
            if (_counterText != null)
            {
                _counterText.text = $"{len}/6";
                _counterText.color = len == 6 ? CounterFullColor : CounterIdleColor;
            }
        }
        #endregion

        #region Buttons
        private void SetupConfirmButton()
        {
            Transform btnTrans = transform.Find("JoinConfirmButton");
            if (btnTrans == null) return;

            _joinConfirmBtn = btnTrans.GetComponent<Button>();

            var btnRt = btnTrans as RectTransform;
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.pivot = new Vector2(0.5f, 0.5f);
                btnRt.anchoredPosition = new Vector2(0f, -52f);
                if (btnRt.sizeDelta == Vector2.zero)
                {
                    btnRt.sizeDelta = new Vector2(450f, 54f);
                }
            }

            // Fill background
            var bgImg = btnTrans.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = JoinUISprites.ButtonFill;
                bgImg.color = new Color(0f, 0.25f, 0.38f, 0.35f);
                bgImg.type = Image.Type.Sliced;
                bgImg.raycastTarget = true;
            }

            // Subtle steel-cyan button border
            var borderImg = GetOrCreateImage(btnTrans, "ButtonBorder");
            borderImg.sprite = JoinUISprites.ButtonFrame;
            borderImg.color = new Color(0.25f, 0.42f, 0.52f, 0.60f);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;
            StretchFull(borderImg.rectTransform);
            borderImg.transform.SetSiblingIndex(0);

            // Glowing cyan button frame
            var glowImg = GetOrCreateImage(btnTrans, "ButtonGlowFrame");
            glowImg.sprite = JoinUISprites.ButtonGlowFrame;
            glowImg.color = new Color(0f, 0.88f, 1f, 0.85f);
            glowImg.type = Image.Type.Sliced;
            glowImg.raycastTarget = false;
            var gRt = glowImg.rectTransform;
            gRt.anchorMin = Vector2.zero;
            gRt.anchorMax = Vector2.one;
            gRt.offsetMin = new Vector2(-4f, -4f);
            gRt.offsetMax = new Vector2(4f, 4f);
            glowImg.transform.SetSiblingIndex(1);

            // Button text
            var label = btnTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "JOIN";
                if (_font != null) label.font = _font;
                label.fontSize = 26f;
                label.characterSpacing = 3f;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            var effect = btnTrans.GetComponent<MainMenuButtonEffect>();
            if (effect != null)
            {
                effect.InitializeComponents();
                effect.CalculateArrowPositions();
                effect.ApplyInstantIdleState();
            }
        }

        private void SetupBackButton()
        {
            Transform btnTrans = transform.Find("JoinBackButton");
            if (btnTrans == null) return;

            _joinBackBtn = btnTrans.GetComponent<Button>();

            var btnRt = btnTrans as RectTransform;
            if (btnRt != null)
            {
                btnRt.anchorMin = new Vector2(0.5f, 0.5f);
                btnRt.anchorMax = new Vector2(0.5f, 0.5f);
                btnRt.pivot = new Vector2(0.5f, 0.5f);
                btnRt.anchoredPosition = new Vector2(0f, -175f);
                if (btnRt.sizeDelta == Vector2.zero)
                {
                    btnRt.sizeDelta = new Vector2(280f, 50f);
                }
            }

            // Fill background
            var bgImg = btnTrans.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = JoinUISprites.ButtonFill;
                bgImg.color = new Color(0.035f, 0.07f, 0.11f, 0.70f);
                bgImg.type = Image.Type.Sliced;
                bgImg.raycastTarget = true;
            }

            // Subtle steel-cyan button border
            var borderImg = GetOrCreateImage(btnTrans, "ButtonBorder");
            borderImg.sprite = JoinUISprites.ButtonFrame;
            borderImg.color = new Color(0.25f, 0.42f, 0.52f, 0.60f);
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;
            StretchFull(borderImg.rectTransform);
            borderImg.transform.SetSiblingIndex(0);

            // Button text
            var label = btnTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "BACK";
                if (_font != null) label.font = _font;
                label.fontSize = 24f;
                label.characterSpacing = 3f;
                label.color = new Color(0.80f, 0.88f, 0.92f, 1f);
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }

            var backEffect = btnTrans.GetComponent<MainMenuButtonEffect>();
            if (backEffect != null)
            {
                backEffect.InitializeComponents();
                backEffect.CalculateArrowPositions();
                backEffect.ApplyInstantIdleState();
            }
        }
        #endregion

        #region Helpers
        private static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                var img = existing.GetComponent<Image>();
                if (img != null) return img;
                return existing.gameObject.AddComponent<Image>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
        #endregion
    }
}
