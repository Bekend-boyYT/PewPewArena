using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Delivers the redesigned sci-fi Create Game screen matching the reference mockup:
    /// - "CREATE GAME" bold italic header flanked by glowing cyan laser beams.
    /// - "ROOM CODE" subtitle flanked by thin horizontal lines.
    /// - Chamfered dark tech box with corner bracket accents & bold italic gold room code.
    /// - Dynamic status bar with glowing cyan indicator dot and flanking horizontal lines.
    /// - 4-corner chamfered settings card containing:
    ///     1. MAP row with chamfered location-pin icon badge and < ARENA > selector.
    ///     2. Center horizontal divider line.
    ///     3. WEAPON SWITCHING row with chamfered gear icon badge and < ENABLED > selector.
    /// - Glowing cyan chamfered START button and dark slate chamfered BACK button with hover arrows.
    /// </summary>
    [DisallowMultipleComponent]
    public class CreatePanelTheme : MonoBehaviour
    {
        private static readonly Color CyanLaser = new Color(0f, 0.90f, 1f, 0.95f);
        private static readonly Color MutedLineColor = new Color(0.35f, 0.58f, 0.70f, 0.50f);
        private static readonly Color SubtitleColor = new Color(0.55f, 0.68f, 0.76f, 0.95f);
        private static readonly Color GoldCodeColor = new Color(0.98f, 0.76f, 0.22f, 1f);
        private static readonly Color StatusTextColor = new Color(0.65f, 0.75f, 0.82f, 1f);
        private static readonly Color CardBgColor = new Color(0.025f, 0.055f, 0.095f, 0.88f);
        private static readonly Color CardBorderColor = new Color(0.06f, 0.65f, 0.82f, 0.58f);
        private static readonly Color RowLabelColor = new Color(0.78f, 0.85f, 0.90f, 1f);
        private static readonly Color BadgeBgColor = new Color(0.02f, 0.12f, 0.19f, 0.95f);
        private static readonly Color BadgeBorderColor = new Color(0.05f, 0.75f, 0.95f, 0.70f);
        private static readonly Color IconCyanColor = new Color(0.05f, 0.90f, 1f, 1f);
        private static readonly Color SelectorBgColor = new Color(0.018f, 0.045f, 0.08f, 0.95f);
        private static readonly Color SelectorBorderColor = new Color(0.06f, 0.65f, 0.84f, 0.68f);
        private static readonly Color AtmosphereColor = new Color(0.02f, 0.70f, 0.45f, 0.16f);

        private TMP_FontAsset _font;
        private TextMeshProUGUI _statusText;
        private RectTransform _statusDotRt;
        private Image _statusDotImg;
        private RectTransform _leftStatusLineRt;
        private RectTransform _rightStatusLineRt;
        private bool _applied;

        private void Awake()
        {
            Apply();
        }

        private TMP_FontAsset _komikaFont;

        public TMP_FontAsset GetKomikaFont()
        {
            if (_komikaFont != null) return _komikaFont;

#if UNITY_EDITOR
            _komikaFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro Effect/Examples/KomikaDemo.asset");
#endif
            if (_komikaFont == null)
            {
                _komikaFont = Resources.Load<TMP_FontAsset>("KomikaDemo") ?? Resources.Load<TMP_FontAsset>("Fonts/KomikaDemo");
            }
            if (_komikaFont == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in allFonts)
                {
                    if (f != null && f.name.IndexOf("Komika", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _komikaFont = f;
                        break;
                    }
                }
            }
            return _komikaFont;
        }

        private void OnEnable()
        {
            Apply();
            ApplyKomikaFonts();
            UpdateStatusBarLayout();
        }

        private void LateUpdate()
        {
            UpdateStatusBarLayout();
        }

        public void Apply()
        {
            if (_applied)
            {
                ApplyKomikaFonts();
                return;
            }
            _applied = true;

            var titleComp = transform.Find("CreateTitle")?.GetComponent<TextMeshProUGUI>();
            if (titleComp != null && titleComp.font != null)
            {
                _font = titleComp.font;
            }

            SetupAtmosphere();
            SetupHeader();
            SetupRoomCodeSection();
            SetupStatusLine();
            SetupSettingsCard();
            SetupButtons();
            ApplyKomikaFonts();
            UpdateStatusBarLayout();
        }

        public void ApplyKomikaFonts()
        {
            var komika = GetKomikaFont();
            if (komika == null) return;

            // 1. Code Display
            var codeText = transform.Find("GameCodeDisplay")?.GetComponent<TextMeshProUGUI>();
            if (codeText != null)
            {
                codeText.font = komika;
            }

            // 2. Status Line ("LOBBY READY! WAITING FOR PLAYER 2 TO JOIN...")
            if (_statusText != null)
            {
                _statusText.font = komika;
            }
            else
            {
                var st = transform.Find("CreateStatusText")?.GetComponent<TextMeshProUGUI>();
                if (st != null) st.font = komika;
            }

            // 3. Map Option Text ("ARENA" / "ARENA 2")
            var mapText = transform.Find("MapSelectRow/MapOptionButton")?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (mapText != null)
            {
                mapText.font = komika;
            }

            // 4. Weapon Toggle Text ("ENABLED" / "DISABLED")
            var weaponText = transform.Find("WeaponSelectRow/WeaponToggleButton")?.GetComponentInChildren<TextMeshProUGUI>(true);
            if (weaponText != null)
            {
                weaponText.font = komika;
            }
        }

        #region Atmosphere
        private void SetupAtmosphere()
        {
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

        #region Header ("CREATE GAME")
        private void SetupHeader()
        {
            Transform titleTrans = transform.Find("CreateTitle");
            if (titleTrans == null) return;

            var titleText = titleTrans.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = "CREATE GAME";
                titleText.fontSize = 56f;
                titleText.characterSpacing = 3.5f;
                titleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
                titleText.color = Color.white;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.raycastTarget = false;
            }

            var titleRt = titleTrans as RectTransform;
            if (titleRt != null)
            {
                SetCenteredRect(titleRt, new Vector2(0f, 355f), new Vector2(480f, 70f));
            }

            var leftLaser = GetOrCreateImage(titleTrans, "LeftLaserLine");
            leftLaser.sprite = JoinUISprites.LaserLine;
            leftLaser.color = CyanLaser;
            leftLaser.raycastTarget = false;
            leftLaser.type = Image.Type.Simple;
            SetCenteredRect(leftLaser.rectTransform, new Vector2(-260f, 0f), new Vector2(145f, 2.8f));
            leftLaser.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var rightLaser = GetOrCreateImage(titleTrans, "RightLaserLine");
            rightLaser.sprite = JoinUISprites.LaserLine;
            rightLaser.color = CyanLaser;
            rightLaser.raycastTarget = false;
            rightLaser.type = Image.Type.Simple;
            SetCenteredRect(rightLaser.rectTransform, new Vector2(260f, 0f), new Vector2(145f, 2.8f));
            rightLaser.rectTransform.localScale = Vector3.one;
        }
        #endregion

        #region Room Code Section ("ROOM CODE" + "KWMDT8" Box)
        private void SetupRoomCodeSection()
        {
            Transform codeLabelTrans = transform.Find("CodeLabel");
            if (codeLabelTrans != null)
            {
                var lbl = codeLabelTrans.GetComponent<TextMeshProUGUI>();
                if (lbl != null)
                {
                    lbl.text = "ROOM CODE";
                    if (_font != null) lbl.font = _font;
                    lbl.fontSize = 15.5f;
                    lbl.characterSpacing = 5f;
                    lbl.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    lbl.color = SubtitleColor;
                    lbl.alignment = TextAlignmentOptions.Center;
                    lbl.raycastTarget = false;
                }

                var lblRt = codeLabelTrans as RectTransform;
                if (lblRt != null)
                {
                    SetCenteredRect(lblRt, new Vector2(0f, 276f), new Vector2(220f, 30f));
                }

                var leftLine = GetOrCreateImage(codeLabelTrans, "LeftCodeLine");
                leftLine.sprite = JoinUISprites.LaserLine;
                leftLine.color = MutedLineColor;
                leftLine.raycastTarget = false;
                SetCenteredRect(leftLine.rectTransform, new Vector2(-135f, 0f), new Vector2(110f, 1.5f));
                leftLine.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

                var rightLine = GetOrCreateImage(codeLabelTrans, "RightCodeLine");
                rightLine.sprite = JoinUISprites.LaserLine;
                rightLine.color = MutedLineColor;
                rightLine.raycastTarget = false;
                SetCenteredRect(rightLine.rectTransform, new Vector2(135f, 0f), new Vector2(110f, 1.5f));
                rightLine.rectTransform.localScale = Vector3.one;
            }

            // Room Code Chamfered Box Container
            var codeBoxImg = GetOrCreateImage(transform, "RoomCodeBox");
            codeBoxImg.sprite = JoinUISprites.CardFill;
            codeBoxImg.color = new Color(0.02f, 0.06f, 0.10f, 0.92f);
            codeBoxImg.type = Image.Type.Sliced;
            codeBoxImg.raycastTarget = false;
            SetCenteredRect(codeBoxImg.rectTransform, new Vector2(0f, 204f), new Vector2(380f, 84f));

            var innerGlow = GetOrCreateImage(codeBoxImg.transform, "InnerGlow");
            innerGlow.sprite = JoinUISprites.AtmosphereGlow;
            innerGlow.color = new Color(0f, 0.62f, 0.85f, 0.22f);
            innerGlow.raycastTarget = false;
            StretchFull(innerGlow.rectTransform);

            var boxFrame = GetOrCreateImage(codeBoxImg.transform, "BoxFrame");
            boxFrame.sprite = JoinUISprites.CardFrame;
            boxFrame.color = new Color(0f, 0.74f, 0.95f, 0.52f);
            boxFrame.type = Image.Type.Sliced;
            boxFrame.raycastTarget = false;
            StretchFull(boxFrame.rectTransform);

            var boxBrackets = GetOrCreateImage(codeBoxImg.transform, "BoxBrackets");
            boxBrackets.sprite = JoinUISprites.RoomCodeBrackets;
            boxBrackets.color = new Color(0.10f, 0.92f, 1f, 0.95f);
            boxBrackets.type = Image.Type.Sliced;
            boxBrackets.raycastTarget = false;
            StretchFull(boxBrackets.rectTransform);

            Transform codeDisplayTrans = transform.Find("GameCodeDisplay");
            if (codeDisplayTrans != null)
            {
                int codeIdx = codeDisplayTrans.GetSiblingIndex();
                codeBoxImg.transform.SetSiblingIndex(Mathf.Max(0, codeIdx));

                var codeRt = codeDisplayTrans as RectTransform;
                if (codeRt != null)
                {
                    SetCenteredRect(codeRt, new Vector2(0f, 204f), new Vector2(340f, 74f));
                }

                var codeText = codeDisplayTrans.GetComponent<TextMeshProUGUI>();
                if (codeText != null)
                {
                    var komika = GetKomikaFont();
                    if (komika != null) codeText.font = komika;
                    else if (_font != null) codeText.font = _font;
                    codeText.fontSize = 44f;
                    codeText.enableAutoSizing = true;
                    codeText.fontSizeMin = 24f;
                    codeText.fontSizeMax = 44f;
                    codeText.characterSpacing = 6f;
                    codeText.fontStyle = FontStyles.Bold | FontStyles.Italic;
                    codeText.color = GoldCodeColor;
                    codeText.alignment = TextAlignmentOptions.Center;
                    codeText.raycastTarget = false;
                }
            }
        }
        #endregion

        #region Status Line
        private void SetupStatusLine()
        {
            Transform statusTrans = transform.Find("CreateStatusText");
            if (statusTrans == null) return;

            _statusText = statusTrans.GetComponent<TextMeshProUGUI>();
            if (_statusText != null)
            {
                var komika = GetKomikaFont();
                if (komika != null) _statusText.font = komika;
                else if (_font != null) _statusText.font = _font;
                _statusText.fontSize = 15f;
                _statusText.characterSpacing = 2.5f;
                _statusText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                _statusText.color = StatusTextColor;
                _statusText.alignment = TextAlignmentOptions.Center;
                _statusText.raycastTarget = false;
            }

            var statusRt = statusTrans as RectTransform;
            if (statusRt != null)
            {
                SetCenteredRect(statusRt, new Vector2(8f, 114f), new Vector2(680f, 32f));
            }

            _statusDotImg = GetOrCreateImage(statusTrans, "StatusDot");
            _statusDotImg.sprite = JoinUISprites.StatusDot;
            _statusDotImg.color = new Color(0f, 0.88f, 1f, 1f);
            _statusDotImg.raycastTarget = false;
            _statusDotRt = _statusDotImg.rectTransform;
            SetCenteredRect(_statusDotRt, new Vector2(-190f, 0f), new Vector2(13f, 13f));

            var leftLine = GetOrCreateImage(statusTrans, "LeftStatusLine");
            leftLine.sprite = JoinUISprites.LaserLine;
            leftLine.color = MutedLineColor;
            leftLine.raycastTarget = false;
            _leftStatusLineRt = leftLine.rectTransform;
            SetCenteredRect(_leftStatusLineRt, new Vector2(-265f, 0f), new Vector2(105f, 1.5f));
            _leftStatusLineRt.localScale = new Vector3(-1f, 1f, 1f);

            var rightLine = GetOrCreateImage(statusTrans, "RightStatusLine");
            rightLine.sprite = JoinUISprites.LaserLine;
            rightLine.color = MutedLineColor;
            rightLine.raycastTarget = false;
            _rightStatusLineRt = rightLine.rectTransform;
            SetCenteredRect(_rightStatusLineRt, new Vector2(250f, 0f), new Vector2(105f, 1.5f));
            _rightStatusLineRt.localScale = Vector3.one;
        }

        private void UpdateStatusBarLayout()
        {
            if (_statusText == null) return;

            float textW = Mathf.Clamp(_statusText.preferredWidth, 120f, 540f);
            float halfW = textW * 0.5f;

            if (_statusDotRt != null)
            {
                _statusDotRt.anchoredPosition = new Vector2(-halfW - 14f, 0f);
            }
            if (_leftStatusLineRt != null)
            {
                _leftStatusLineRt.anchoredPosition = new Vector2(-halfW - 82f, 0f);
            }
            if (_rightStatusLineRt != null)
            {
                _rightStatusLineRt.anchoredPosition = new Vector2(halfW + 66f, 0f);
            }

            if (_statusDotImg != null)
            {
                string raw = _statusText.text ?? string.Empty;
                if (raw.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    raw.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _statusDotImg.color = new Color(1f, 0.28f, 0.28f, 1f);
                }
                else if (raw.IndexOf("connected", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _statusDotImg.color = new Color(0f, 1f, 0.55f, 1f);
                }
                else
                {
                    _statusDotImg.color = new Color(0f, 0.88f, 1f, 1f);
                }
            }
        }
        #endregion

        #region Settings Card Container (MAP & WEAPON SWITCHING)
        private void SetupSettingsCard()
        {
            var cardImg = GetOrCreateImage(transform, "CreateSettingsCard");
            cardImg.sprite = JoinUISprites.CardFill;
            cardImg.color = CardBgColor;
            cardImg.type = Image.Type.Sliced;
            cardImg.raycastTarget = false;
            SetCenteredRect(cardImg.rectTransform, new Vector2(0f, -26f), new Vector2(620f, 176f));

            var borderImg = GetOrCreateImage(cardImg.transform, "CardBorder");
            borderImg.sprite = JoinUISprites.CardFrame;
            borderImg.color = CardBorderColor;
            borderImg.type = Image.Type.Sliced;
            borderImg.raycastTarget = false;
            StretchFull(borderImg.rectTransform);

            var dividerImg = GetOrCreateImage(cardImg.transform, "CardDivider");
            dividerImg.sprite = JoinUISprites.White;
            dividerImg.color = new Color(0.12f, 0.45f, 0.58f, 0.42f);
            dividerImg.raycastTarget = false;
            SetCenteredRect(dividerImg.rectTransform, Vector2.zero, new Vector2(564f, 1.5f));

            Transform mapRow = transform.Find("MapSelectRow");
            Transform weaponRow = transform.Find("WeaponSelectRow");

            if (mapRow != null)
            {
                int mapIdx = mapRow.GetSiblingIndex();
                cardImg.transform.SetSiblingIndex(Mathf.Max(0, mapIdx));
                StyleOptionRow(
                    mapRow,
                    yPos: 17f,
                    labelObjName: "MapLabel",
                    labelText: "MAP",
                    iconSprite: JoinUISprites.MapPinIcon,
                    iconSize: new Vector2(20f, 20f),
                    buttonObjName: "MapOptionButton"
                );
            }

            if (weaponRow != null)
            {
                StyleOptionRow(
                    weaponRow,
                    yPos: -69f,
                    labelObjName: "WeaponLabel",
                    labelText: "WEAPON SWITCHING",
                    iconSprite: JoinUISprites.GearIcon,
                    iconSize: new Vector2(21f, 21f),
                    buttonObjName: "WeaponToggleButton"
                );
            }
        }

        private void StyleOptionRow(
            Transform rowTrans,
            float yPos,
            string labelObjName,
            string labelText,
            Sprite iconSprite,
            Vector2 iconSize,
            string buttonObjName)
        {
            var rowRt = rowTrans as RectTransform;
            if (rowRt != null)
            {
                SetCenteredRect(rowRt, new Vector2(0f, yPos), new Vector2(564f, 50f));
            }

            // 1. Chamfered Icon Badge on the left
            var badgeFill = GetOrCreateImage(rowTrans, "RowIconBadge");
            badgeFill.sprite = JoinUISprites.IconBadgeFill;
            badgeFill.color = BadgeBgColor;
            badgeFill.type = Image.Type.Sliced;
            badgeFill.raycastTarget = false;
            SetCenteredRect(badgeFill.rectTransform, new Vector2(-262f, 0f), new Vector2(38f, 38f));

            var badgeBorder = GetOrCreateImage(badgeFill.transform, "BadgeBorder");
            badgeBorder.sprite = JoinUISprites.IconBadgeFrame;
            badgeBorder.color = BadgeBorderColor;
            badgeBorder.type = Image.Type.Sliced;
            badgeBorder.raycastTarget = false;
            StretchFull(badgeBorder.rectTransform);

            var iconImg = GetOrCreateImage(badgeFill.transform, "BadgeIcon");
            iconImg.sprite = iconSprite;
            iconImg.color = IconCyanColor;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            SetCenteredRect(iconImg.rectTransform, Vector2.zero, iconSize);

            // 2. Row Label ("MAP" / "WEAPON SWITCHING")
            Transform lblTrans = rowTrans.Find(labelObjName);
            if (lblTrans != null)
            {
                var lblRt = lblTrans as RectTransform;
                if (lblRt != null)
                {
                    SetCenteredRect(lblRt, new Vector2(-98f, 0f), new Vector2(250f, 40f));
                }

                var lbl = lblTrans.GetComponent<TextMeshProUGUI>();
                if (lbl != null)
                {
                    if (_font != null) lbl.font = _font;
                    lbl.text = labelText;
                    lbl.fontSize = 16.5f;
                    lbl.characterSpacing = 2.5f;
                    lbl.fontStyle = FontStyles.Bold;
                    lbl.color = RowLabelColor;
                    lbl.alignment = TextAlignmentOptions.MidlineLeft;
                    lbl.raycastTarget = false;
                }
            }

            // 3. Selector Button ("<  ARENA  >" / "<  ENABLED  >")
            Transform btnTrans = rowTrans.Find(buttonObjName);
            if (btnTrans != null)
            {
                var btnRt = btnTrans as RectTransform;
                if (btnRt != null)
                {
                    SetCenteredRect(btnRt, new Vector2(152f, 0f), new Vector2(260f, 42f));
                }

                var btn = btnTrans.GetComponent<Button>();
                if (btn != null)
                {
                    btn.transition = Selectable.Transition.None;
                }

                var btnBg = btnTrans.GetComponent<Image>();
                if (btnBg != null)
                {
                    btnBg.sprite = JoinUISprites.ChamferBoxFill;
                    btnBg.color = SelectorBgColor;
                    btnBg.type = Image.Type.Sliced;
                    btnBg.raycastTarget = true;
                }

                var selBorder = GetOrCreateImage(btnTrans, "SelectorBorder");
                selBorder.sprite = JoinUISprites.ChamferBoxFrame;
                selBorder.color = SelectorBorderColor;
                selBorder.type = Image.Type.Sliced;
                selBorder.raycastTarget = false;
                StretchFull(selBorder.rectTransform);
                selBorder.transform.SetSiblingIndex(0);

                var leftChev = GetOrCreateImage(btnTrans, "ChevronLeft");
                leftChev.sprite = JoinUISprites.ChevronLeft;
                leftChev.color = new Color(0.25f, 0.88f, 1f, 0.92f);
                leftChev.raycastTarget = false;
                SetCenteredRect(leftChev.rectTransform, new Vector2(-108f, 0f), new Vector2(15f, 15f));

                var rightChev = GetOrCreateImage(btnTrans, "ChevronRight");
                rightChev.sprite = JoinUISprites.ChevronRight;
                rightChev.color = new Color(0.25f, 0.88f, 1f, 0.92f);
                rightChev.raycastTarget = false;
                SetCenteredRect(rightChev.rectTransform, new Vector2(108f, 0f), new Vector2(15f, 15f));

                var valText = btnTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (valText != null)
                {
                    var komika = GetKomikaFont();
                    if (komika != null) valText.font = komika;
                    else if (_font != null) valText.font = _font;
                    valText.fontSize = 16.5f;
                    valText.enableAutoSizing = true;
                    valText.fontSizeMin = 11.5f;
                    valText.fontSizeMax = 16.5f;
                    valText.textWrappingMode = TextWrappingModes.NoWrap;
                    valText.characterSpacing = 2.5f;
                    valText.fontStyle = FontStyles.Bold;
                    valText.color = Color.white;
                    valText.alignment = TextAlignmentOptions.Center;
                    valText.raycastTarget = false;

                    var vtRt = valText.rectTransform;
                    vtRt.anchorMin = Vector2.zero;
                    vtRt.anchorMax = Vector2.one;
                    vtRt.offsetMin = new Vector2(28f, 2f);
                    vtRt.offsetMax = new Vector2(-28f, -2f);
                }

                var hoverEffect = btnTrans.GetComponent<CreateSelectorHoverEffect>();
                if (hoverEffect == null) hoverEffect = btnTrans.gameObject.AddComponent<CreateSelectorHoverEffect>();
                hoverEffect.Init(btnBg, selBorder, leftChev.rectTransform, rightChev.rectTransform, leftChev, rightChev);
            }
        }
        #endregion

        #region Action Buttons (START & BACK)
        private void SetupButtons()
        {
            // 1. START Button (Bright glowing cyan chamfered button)
            Transform startTrans = transform.Find("CreateStartButton");
            if (startTrans != null)
            {
                var startRt = startTrans as RectTransform;
                if (startRt != null)
                {
                    SetCenteredRect(startRt, new Vector2(0f, -184f), new Vector2(284f, 56f));
                }

                var fillIdle = new Color(0.03f, 0.48f, 0.68f, 0.92f);
                var fillHover = new Color(0.06f, 0.62f, 0.84f, 0.98f);
                var borderIdle = new Color(0.35f, 0.95f, 1f, 1f);
                var borderHover = Color.white;

                var bgImg = startTrans.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = JoinUISprites.ChamferBoxFill;
                    bgImg.color = fillIdle;
                    bgImg.type = Image.Type.Sliced;
                    bgImg.raycastTarget = true;
                }

                var borderImg = GetOrCreateImage(startTrans, "ButtonBorder");
                borderImg.sprite = JoinUISprites.ChamferBoxFrame;
                borderImg.color = borderIdle;
                borderImg.type = Image.Type.Sliced;
                borderImg.raycastTarget = false;
                StretchFull(borderImg.rectTransform);
                borderImg.transform.SetSiblingIndex(0);

                // Outer cyan bloom glow
                var glowImg = GetOrCreateImage(startTrans, "ButtonGlowFrame");
                glowImg.sprite = JoinUISprites.ChamferGlowFrame;
                glowImg.color = new Color(0f, 0.88f, 1f, 0.85f);
                glowImg.type = Image.Type.Sliced;
                glowImg.raycastTarget = false;
                var gRt = glowImg.rectTransform;
                gRt.anchorMin = Vector2.zero;
                gRt.anchorMax = Vector2.one;
                gRt.offsetMin = new Vector2(-7f, -7f);
                gRt.offsetMax = new Vector2(7f, 7f);
                glowImg.transform.SetSiblingIndex(1);

                // Inner center cyan illumination
                var centerGlow = GetOrCreateImage(startTrans, "ButtonInnerHighlight");
                centerGlow.sprite = JoinUISprites.AtmosphereGlow;
                centerGlow.color = new Color(0.18f, 0.95f, 1f, 0.48f);
                centerGlow.raycastTarget = false;
                StretchFull(centerGlow.rectTransform);
                centerGlow.transform.SetSiblingIndex(2);

                var label = startTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "START";
                    if (_font != null) label.font = _font;
                    label.fontSize = 25f;
                    label.characterSpacing = 3f;
                    label.fontStyle = FontStyles.Bold;
                    label.color = Color.white;
                    label.alignment = TextAlignmentOptions.Center;
                    label.raycastTarget = false;
                }

                var effect = startTrans.GetComponent<MainMenuButtonEffect>();
                if (effect == null) effect = startTrans.gameObject.AddComponent<MainMenuButtonEffect>();
                effect.ConfigureColors(fillIdle, fillHover, borderIdle, borderHover, Color.white, Color.white);
                effect.InitializeComponents();
                effect.CalculateArrowPositions();
                effect.ApplyInstantIdleState();
            }

            // 2. BACK Button (Dark slate chamfered button)
            Transform backTrans = transform.Find("CreateBackButton");
            if (backTrans != null)
            {
                var backRt = backTrans as RectTransform;
                if (backRt != null)
                {
                    SetCenteredRect(backRt, new Vector2(0f, -254f), new Vector2(284f, 52f));
                }

                var fillIdle = new Color(0.08f, 0.11f, 0.15f, 0.80f);
                var fillHover = new Color(0.13f, 0.18f, 0.24f, 0.92f);
                var borderIdle = new Color(0.32f, 0.39f, 0.46f, 0.58f);
                var borderHover = new Color(0.45f, 0.65f, 0.78f, 0.88f);
                var textIdle = new Color(0.65f, 0.72f, 0.78f, 1f);

                var bgImg = backTrans.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.sprite = JoinUISprites.ChamferBoxFill;
                    bgImg.color = fillIdle;
                    bgImg.type = Image.Type.Sliced;
                    bgImg.raycastTarget = true;
                }

                var borderImg = GetOrCreateImage(backTrans, "ButtonBorder");
                borderImg.sprite = JoinUISprites.ChamferBoxFrame;
                borderImg.color = borderIdle;
                borderImg.type = Image.Type.Sliced;
                borderImg.raycastTarget = false;
                StretchFull(borderImg.rectTransform);
                borderImg.transform.SetSiblingIndex(0);

                var label = backTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "BACK";
                    if (_font != null) label.font = _font;
                    label.fontSize = 23f;
                    label.characterSpacing = 3f;
                    label.fontStyle = FontStyles.Bold;
                    label.color = textIdle;
                    label.alignment = TextAlignmentOptions.Center;
                    label.raycastTarget = false;
                }

                var backEffect = backTrans.GetComponent<MainMenuButtonEffect>();
                if (backEffect == null) backEffect = backTrans.gameObject.AddComponent<MainMenuButtonEffect>();
                backEffect.ConfigureColors(fillIdle, fillHover, borderIdle, borderHover, textIdle, Color.white);
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
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static void SetCenteredRect(RectTransform rt, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            rt.localScale = Vector3.one;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
        }
        #endregion
    }

    /// <summary>
    /// Subtle hover and click feedback for the < ARENA > and < ENABLED > selector boxes.
    /// </summary>
    public class CreateSelectorHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Image _bg;
        private Image _border;
        private RectTransform _leftChevRt;
        private RectTransform _rightChevRt;
        private Image _leftChevImg;
        private Image _rightChevImg;
        private bool _hovered;
        private bool _pressed;

        private static readonly Color BgIdle = new Color(0.018f, 0.045f, 0.08f, 0.95f);
        private static readonly Color BgHover = new Color(0.03f, 0.085f, 0.14f, 0.98f);
        private static readonly Color BorderIdle = new Color(0.06f, 0.65f, 0.84f, 0.68f);
        private static readonly Color BorderHover = new Color(0.15f, 0.92f, 1f, 0.98f);
        private static readonly Color ChevIdle = new Color(0.25f, 0.88f, 1f, 0.90f);
        private static readonly Color ChevHover = Color.white;

        public void Init(Image bg, Image border, RectTransform leftChevRt, RectTransform rightChevRt, Image leftChevImg, Image rightChevImg)
        {
            _bg = bg;
            _border = border;
            _leftChevRt = leftChevRt;
            _rightChevRt = rightChevRt;
            _leftChevImg = leftChevImg;
            _rightChevImg = rightChevImg;
        }

        public void OnPointerEnter(PointerEventData eventData) => _hovered = true;
        public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            if (_bg != null) _bg.color = BgIdle;
            if (_border != null) _border.color = BorderIdle;
            if (_leftChevRt != null) _leftChevRt.anchoredPosition = new Vector2(-108f, 0f);
            if (_rightChevRt != null) _rightChevRt.anchoredPosition = new Vector2(108f, 0f);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * 16f;
            if (_bg != null) _bg.color = Color.Lerp(_bg.color, _hovered ? BgHover : BgIdle, dt);
            if (_border != null) _border.color = Color.Lerp(_border.color, _hovered ? BorderHover : BorderIdle, dt);
            if (_leftChevImg != null) _leftChevImg.color = Color.Lerp(_leftChevImg.color, _hovered ? ChevHover : ChevIdle, dt);
            if (_rightChevImg != null) _rightChevImg.color = Color.Lerp(_rightChevImg.color, _hovered ? ChevHover : ChevIdle, dt);

            float nudge = _pressed ? 4.5f : (_hovered ? 2.5f : 0f);
            if (_leftChevRt != null)
            {
                float x = Mathf.Lerp(_leftChevRt.anchoredPosition.x, -108f - nudge, dt);
                _leftChevRt.anchoredPosition = new Vector2(x, 0f);
            }
            if (_rightChevRt != null)
            {
                float x = Mathf.Lerp(_rightChevRt.anchoredPosition.x, 108f + nudge, dt);
                _rightChevRt.anchoredPosition = new Vector2(x, 0f);
            }
        }
    }
}
