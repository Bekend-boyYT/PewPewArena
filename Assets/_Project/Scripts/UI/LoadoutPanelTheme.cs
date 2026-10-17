using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Applies the "Loadout" redesign to the existing LoadoutPanel hierarchy at runtime:
    /// - Heavy italic LOADOUT title over a dark ink brush stroke.
    /// - Subtitle flanked by split cyan laser lines.
    /// - Chamfered weapon cards (class tag + name, bracketed weapon bay, icon stat rows, equip button).
    /// - "—— BACK ——" footer.
    /// Only restyles / adds child visuals; all existing objects, button wiring, entrance animation and
    /// LoadoutCardEffect behaviour are preserved. Idempotent (safe to call multiple times).
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadoutPanelTheme : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.00f, 0.83f, 1.00f, 1f);
        private static readonly Color Amber = new Color(1.00f, 0.71f, 0.00f, 1f);
        private static readonly Color StatText = new Color(0.80f, 0.86f, 0.90f, 1f);

        private const float CardW = 480f, CardH = 580f;

        private struct CardData
        {
            public string Tag;
            public Color Theme;
            public string[] Values;
            public float[] Fills;
        }

        private static readonly string[] StatKeys = { "DAMAGE", "FIRE RATE", "CAPACITY", "RECOIL" };

        private static readonly CardData Sniper1 = new CardData
        {
            Tag = "MEDIUM SNIPER BOLT",
            Theme = Cyan,
            Values = new[] { "70 / MEDIUM", "85 / BOLT", "5 ROUNDS", "LIGHT" },
            Fills = new[] { 0.76f, 0.85f, 0.72f, 0.37f }
        };

        private static readonly CardData Sniper2 = new CardData
        {
            Tag = "HEAVY SNIPER BOLT",
            Theme = Amber,
            Values = new[] { "100 / VETERAN", "36 / BOLT", "8 ROUNDS", "MEDIUM" },
            Fills = new[] { 1.00f, 0.80f, 0.52f, 0.60f }
        };

        private TMP_FontAsset _titleFont;
        private TMP_FontAsset _bodyFont;
        private bool _applied;

        private RectTransform _sniper1SidePanel;
        private RectTransform _sniper2SidePanel;
        private CanvasGroup _sniper1SideGroup;
        private CanvasGroup _sniper2SideGroup;
        private Button _sniper1InspectButton;
        private Button _sniper2InspectButton;
        private Coroutine _sidePanelAnimCoroutine;

        public Button Sniper1InspectButton => _sniper1InspectButton;
        public Button Sniper2InspectButton => _sniper2InspectButton;

        public void Apply()
        {
            if (_applied) return;
            _applied = true;

            _titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF");
            _bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");

            ApplyHeader();

            Transform container = transform.Find("WeaponCardsContainer");
            if (container != null)
            {
                var hlg = container.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.enabled = false;

                var containerRt = container as RectTransform;
                if (containerRt != null)
                {
                    containerRt.anchoredPosition = new Vector2(0f, -24f);
                    containerRt.sizeDelta = new Vector2(1000f, CardH);
                }

                Transform card1 = container.Find("Sniper1Card");
                Transform card2 = container.Find("Sniper2Card");

                ApplyCard(card1, Sniper1, -260f);
                ApplyCard(card2, Sniper2, 260f);

                _sniper1SidePanel = ApplyInspectSidePanel(container, card1, Sniper1, 0, -705f, out _sniper1SideGroup, out _sniper1InspectButton);
                _sniper2SidePanel = ApplyInspectSidePanel(container, card2, Sniper2, 1, 705f, out _sniper2SideGroup, out _sniper2InspectButton);
            }

            ApplyBackButton();
        }

        #region Header
        private void ApplyHeader()
        {
            var title = transform.Find("LoadoutTitle")?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                // Ink brush stroke behind the title (sibling placed right before it so it renders underneath)
                var brush = GetOrCreateImage(transform, "LoadoutTitleBrush");
                brush.sprite = LoadoutUISprites.TitleBrush;
                brush.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);
                SetRect(brush.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 386f), new Vector2(640f, 132f));
                brush.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
                brush.transform.SetSiblingIndex(title.transform.GetSiblingIndex());

                SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 388f), new Vector2(760f, 120f));
                StyleText(title, _titleFont, 100f, 3f, Color.white, TextAlignmentOptions.Center);
                title.fontStyle = FontStyles.Italic;
                title.text = "LOADOUT";
            }

            var subtitle = transform.Find("LoadoutSubtitle")?.GetComponent<TextMeshProUGUI>();
            if (subtitle != null)
            {
                SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 314f), new Vector2(440f, 32f));
                StyleText(subtitle, _bodyFont, 21f, 7f, new Color(0.74f, 0.82f, 0.88f, 1f), TextAlignmentOptions.Center);
                subtitle.fontStyle = FontStyles.Normal;
                subtitle.text = "SELECT PRIMARY COMBAT RIFLE";
            }

            // Split laser lines flanking the subtitle. They live under LaserDivider so the existing
            // entrance animation still expands them outward from the centre.
            var divider = transform.Find("LaserDivider") as RectTransform;
            if (divider != null)
            {
                var dividerImg = divider.GetComponent<Image>();
                if (dividerImg != null)
                {
                    dividerImg.color = new Color(0f, 0f, 0f, 0f);
                    dividerImg.raycastTarget = false;
                }
                SetRect(divider, new Vector2(0.5f, 0.5f), new Vector2(0f, 307f), new Vector2(1000f, 2f));

                Color line = new Color(0.05f, 0.72f, 0.92f, 0.90f);
                var left = GetOrCreateImage(divider, "LineLeft");
                left.color = line;
                SetRect(left.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-362f, 0f), new Vector2(270f, 2f));

                var right = GetOrCreateImage(divider, "LineRight");
                right.color = line;
                SetRect(right.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(362f, 0f), new Vector2(270f, 2f));
            }
        }
        #endregion

        #region Cards
        private void ApplyCard(Transform card, CardData data, float x)
        {
            if (card == null) return;
            var cardRt = (RectTransform)card;
            SetRect(cardRt, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(CardW, CardH));

            // Body (chamfered, tinted by LoadoutCardEffect depending on equip state)
            var body = card.GetComponent<Image>();
            if (body != null)
            {
                body.sprite = LoadoutUISprites.CardFill;
                body.type = Image.Type.Sliced;
                body.raycastTarget = true;
            }
            var outline = card.GetComponent<Outline>();
            if (outline != null) outline.enabled = false; // replaced by the crisp chamfered frame

            var frame = GetOrCreateImage(card, "CardFrame");
            frame.sprite = LoadoutUISprites.CardFrame;
            frame.type = Image.Type.Sliced;
            Stretch(frame.rectTransform);
            frame.transform.SetSiblingIndex(0);

            var accent = GetOrCreateImage(card, "CornerAccentTL");
            accent.sprite = LoadoutUISprites.CornerTriangle;
            SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(0f, 1f));
            accent.transform.SetSiblingIndex(1);

            // Class tag row: "/// MEDIUM SNIPER BOLT"
            var slashes = GetOrCreateImage(card, "TagSlashes");
            slashes.sprite = LoadoutUISprites.TagSlashes;
            slashes.color = data.Theme;
            SetRect(slashes.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-196f, 252f), new Vector2(36f, 14f), new Vector2(0f, 0.5f));

            var tag = card.Find("TopTagRow")?.GetComponent<TextMeshProUGUI>();
            if (tag != null)
            {
                SetRect(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-152f, 252f), new Vector2(370f, 26f), new Vector2(0f, 0.5f));
                StyleText(tag, _bodyFont, 18f, 2f, data.Theme, TextAlignmentOptions.MidlineLeft);
                tag.fontStyle = FontStyles.Normal;
                tag.text = data.Tag;
            }

            var underline = GetOrCreateImage(card, "TagUnderline");
            underline.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.28f);
            SetRect(underline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-196f, 236f), new Vector2(414f, 1.5f), new Vector2(0f, 0.5f));

            // Weapon name
            var header = card.Find("Header TMP")?.GetComponent<TextMeshProUGUI>();
            if (header != null)
            {
                SetRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, 204f), new Vector2(390f, 54f), new Vector2(0f, 0.5f));
                StyleText(header, _bodyFont, 42f, 2f, Color.white, TextAlignmentOptions.MidlineLeft);
                header.fontStyle = FontStyles.Normal;
            }

            // Weapon bay
            Image bayFill = null, brackets = null;
            CanvasGroup auraGroup = null;
            Transform bay = FindChildStartingWith(card, "StagingBay");
            if (bay != null)
            {
                SetRect((RectTransform)bay, new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(410f, 160f));

                bayFill = GetOrCreateImage(bay, "BayFill");
                Stretch(bayFill.rectTransform);
                bayFill.transform.SetSiblingIndex(0);

                var aura = bay.Find("AuraGlow");
                if (aura != null)
                {
                    var auraImg = aura.GetComponent<Image>();
                    if (auraImg != null)
                    {
                        auraImg.sprite = LoadoutUISprites.RadialSoft;
                        auraImg.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.55f);
                        auraImg.raycastTarget = false;
                    }
                    SetRect((RectTransform)aura, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 150f));
                    auraGroup = aura.GetComponent<CanvasGroup>();
                    if (auraGroup == null) auraGroup = aura.gameObject.AddComponent<CanvasGroup>();
                    auraGroup.blocksRaycasts = false;
                    aura.SetSiblingIndex(1);
                }

                var weapon = bay.Find("WeaponCutout")?.GetComponent<Image>();
                if (weapon != null)
                {
                    weapon.preserveAspect = true;
                    weapon.raycastTarget = false;
                    weapon.rectTransform.sizeDelta = new Vector2(330f, 128f);
                }

                brackets = GetOrCreateImage(bay, "BayBrackets");
                brackets.sprite = LoadoutUISprites.BayBrackets;
                brackets.type = Image.Type.Sliced;
                brackets.fillCenter = false;
                Stretch(brackets.rectTransform);
                brackets.transform.SetAsLastSibling();
            }

            // Stats
            Image[] fills = new Image[StatKeys.Length];
            Transform stats = FindChildStartingWith(card, "StatDashboard");
            if (stats != null)
            {
                SetRect((RectTransform)stats, new Vector2(0.5f, 0.5f), new Vector2(0f, -87f), new Vector2(410f, 160f));
                Sprite[] icons =
                {
                    LoadoutUISprites.IconDamage, LoadoutUISprites.IconFireRate,
                    LoadoutUISprites.IconCapacity, LoadoutUISprites.IconRecoil
                };
                float[] rowY = { 58f, 19.5f, -19.5f, -58f };

                for (int i = 0; i < StatKeys.Length; i++)
                {
                    string key = StatKeys[i];
                    float y = rowY[i];

                    var icon = GetOrCreateImage(stats, "Icon_" + key);
                    icon.sprite = icons[i];
                    icon.color = data.Theme;
                    icon.preserveAspect = true;
                    SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-189f, y), new Vector2(21f, 21f));

                    var label = stats.Find("Label_" + key)?.GetComponent<TextMeshProUGUI>();
                    if (label != null)
                    {
                        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-169f, y), new Vector2(110f, 24f), new Vector2(0f, 0.5f));
                        StyleText(label, _bodyFont, 15f, 1.5f, StatText, TextAlignmentOptions.MidlineLeft);
                        label.fontStyle = FontStyles.Normal;
                        label.text = key;
                    }

                    var track = stats.Find("Track_" + key)?.GetComponent<Image>();
                    if (track != null)
                    {
                        track.sprite = LoadoutUISprites.White;
                        track.type = Image.Type.Simple;
                        track.color = new Color(0.02f, 0.07f, 0.11f, 0.85f);
                        track.raycastTarget = false;
                        SetRect(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-62f, y), new Vector2(174f, 9f), new Vector2(0f, 0.5f));
                    }

                    var fill = stats.Find("Fill_" + key)?.GetComponent<Image>();
                    if (fill != null)
                    {
                        fill.sprite = LoadoutUISprites.White;
                        fill.type = Image.Type.Filled;
                        fill.fillMethod = Image.FillMethod.Horizontal;
                        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                        fill.color = data.Theme;
                        fill.raycastTarget = false;
                        fill.fillAmount = data.Fills[i];
                        SetRect(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-62f, y), new Vector2(174f, 9f), new Vector2(0f, 0.5f));
                        fills[i] = fill;
                    }

                    var val = stats.Find("Val_" + key)?.GetComponent<TextMeshProUGUI>();
                    if (val != null)
                    {
                        SetRect(val.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(202f, y), new Vector2(120f, 24f), new Vector2(1f, 0.5f));
                        StyleText(val, _bodyFont, 14f, 1f, StatText, TextAlignmentOptions.MidlineRight);
                        val.fontStyle = FontStyles.Normal;
                        val.text = data.Values[i];
                    }
                }
            }

            // Equip button
            Image badgeBg = null, badgeFrame = null, badgeIcon = null;
            TextMeshProUGUI badgeText = null;
            Transform badge = card.Find("EquipBadge");
            if (badge != null)
            {
                SetRect((RectTransform)badge, new Vector2(0.5f, 0.5f), new Vector2(0f, -224f), new Vector2(410f, 66f));
                badgeBg = badge.GetComponent<Image>();
                if (badgeBg != null)
                {
                    badgeBg.sprite = LoadoutUISprites.ButtonFill;
                    badgeBg.type = Image.Type.Sliced;
                }
                var badgeOutline = badge.GetComponent<Outline>();
                if (badgeOutline != null) badgeOutline.enabled = false;

                badgeFrame = GetOrCreateImage(badge, "BadgeFrame");
                badgeFrame.sprite = LoadoutUISprites.ButtonFrame;
                badgeFrame.type = Image.Type.Sliced;
                Stretch(badgeFrame.rectTransform);
                badgeFrame.transform.SetSiblingIndex(0);

                badgeIcon = GetOrCreateImage(badge, "BadgeIcon");
                badgeIcon.preserveAspect = true;
                SetRect(badgeIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));

                badgeText = badge.Find("BadgeText")?.GetComponent<TextMeshProUGUI>();
                if (badgeText != null)
                {
                    SetRect(badgeText.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 50f));
                    StyleText(badgeText, _bodyFont, 22f, 3f, Color.white, TextAlignmentOptions.Center);
                    badgeText.fontStyle = FontStyles.Normal;
                }
            }

            var effect = card.GetComponent<LoadoutCardEffect>();
            if (effect != null)
            {
                effect.BindThemeVisuals(data.Theme, body, frame, new[] { accent }, bayFill, brackets,
                    badgeBg, badgeFrame, badgeIcon, badgeText, auraGroup, fills, (float[])data.Fills.Clone());
            }
        }

        private RectTransform ApplyInspectSidePanel(Transform container, Transform card, CardData data, int sniperIndex,
            float targetX, out CanvasGroup sideGroup, out Button inspectButton)
        {
            sideGroup = null;
            inspectButton = null;
            if (container == null) return null;

            string panelName = (sniperIndex == 0) ? "Sniper1SidePanel" : "Sniper2SidePanel";

            // Reuse existing side panel if present on card or container, otherwise create under container
            Transform existing = container.Find(panelName);
            if (existing == null && card != null)
            {
                existing = card.Find("SidePanel") ?? card.Find("InspectSidePanel");
                if (existing != null)
                {
                    existing.SetParent(container, false);
                    existing.name = panelName;
                }
            }

            RectTransform sideRt;
            if (existing != null)
            {
                sideRt = existing as RectTransform ?? existing.gameObject.AddComponent<RectTransform>();
            }
            else
            {
                var sideGo = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                sideGo.layer = container.gameObject.layer;
                sideGo.transform.SetParent(container, false);
                sideRt = sideGo.GetComponent<RectTransform>();
            }

            SetRect(sideRt, new Vector2(0.5f, 0.5f), new Vector2(targetX, 0f), new Vector2(346f, 496f));

            sideGroup = sideRt.GetComponent<CanvasGroup>();
            if (sideGroup == null) sideGroup = sideRt.gameObject.AddComponent<CanvasGroup>();

            // Body & Chamfered Frame
            var body = sideRt.GetComponent<Image>();
            if (body == null) body = sideRt.gameObject.AddComponent<Image>();
            body.sprite = LoadoutUISprites.CardFill;
            body.type = Image.Type.Sliced;
            body.color = new Color(0.02f, 0.05f, 0.09f, 0.92f);
            body.raycastTarget = true;

            var frame = GetOrCreateImage(sideRt, "SideFrame");
            frame.sprite = LoadoutUISprites.CardFrame;
            frame.type = Image.Type.Sliced;
            frame.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.88f);
            Stretch(frame.rectTransform);
            frame.transform.SetSiblingIndex(0);

            var accent = GetOrCreateImage(sideRt, "CornerAccentTL");
            accent.sprite = LoadoutUISprites.CornerTriangle;
            accent.color = data.Theme;
            SetRect(accent.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(16f, 16f), new Vector2(0f, 1f));

            // Glowing connector bridge linking side panel to selected sniper card
            var connector = GetOrCreateImage(sideRt, "CardConnectorBeam");
            connector.sprite = LoadoutUISprites.White;
            connector.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.78f);
            float connectorX = (sniperIndex == 0) ? 189f : -189f;
            SetRect(connector.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(connectorX, 0f), new Vector2(32f, 2.5f));

            // Top Tag Row
            var slashes = GetOrCreateImage(sideRt, "SideTagSlashes");
            slashes.sprite = LoadoutUISprites.TagSlashes;
            slashes.color = data.Theme;
            SetRect(slashes.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140f, 208f), new Vector2(30f, 12f), new Vector2(0f, 0.5f));

            var tagTmp = GetOrCreateText(sideRt, "SideTagText");
            SetRect(tagTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-102f, 208f), new Vector2(250f, 24f), new Vector2(0f, 0.5f));
            StyleText(tagTmp, _bodyFont, 15f, 2f, data.Theme, TextAlignmentOptions.MidlineLeft);
            tagTmp.text = "WEAPON SHOWCASE";

            var underline = GetOrCreateImage(sideRt, "SideUnderline");
            underline.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.28f);
            SetRect(underline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140f, 192f), new Vector2(280f, 1.5f), new Vector2(0f, 0.5f));

            // Weapon Title & Subtitle
            var titleTmp = GetOrCreateText(sideRt, "SideWeaponTitle");
            SetRect(titleTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140f, 156f), new Vector2(280f, 44f), new Vector2(0f, 0.5f));
            StyleText(titleTmp, _bodyFont, 34f, 2f, Color.white, TextAlignmentOptions.MidlineLeft);
            titleTmp.text = (sniperIndex == 0) ? "SNIPER 01" : "SNIPER 02";

            var subTmp = GetOrCreateText(sideRt, "SideWeaponSubtitle");
            SetRect(subTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-140f, 122f), new Vector2(280f, 22f), new Vector2(0f, 0.5f));
            StyleText(subTmp, _bodyFont, 13f, 2.2f, StatText, TextAlignmentOptions.MidlineLeft);
            subTmp.text = (sniperIndex == 0) ? "COSMIC 3D INSPECTION BAY" : "HEAVY ORDNANCE INSPECTION BAY";

            // Bracketed Telemetry & Feature Box
            var infoBay = GetOrCreateImage(sideRt, "SideInfoBay");
            infoBay.sprite = LoadoutUISprites.White;
            infoBay.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.08f);
            SetRect(infoBay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(292f, 200f));

            var infoBrackets = GetOrCreateImage(infoBay.transform, "InfoBrackets");
            infoBrackets.sprite = LoadoutUISprites.BayBrackets;
            infoBrackets.type = Image.Type.Sliced;
            infoBrackets.fillCenter = false;
            infoBrackets.color = new Color(data.Theme.r, data.Theme.g, data.Theme.b, 0.72f);
            Stretch(infoBrackets.rectTransform);

            string[] rowKeys = { "CALIBER", "VELOCITY", "OPTICS", "LAB MODE" };
            string[] rowVals = (sniperIndex == 0)
                ? new[] { ".338 LAPUA MAG", "500 M/S", "18° PRECISION", "360° + VFX + SLOW-MO" }
                : new[] { ".50 BMG HEAVY", "550 M/S", "18° HIGH-MAG", "360° + VFX + SLOW-MO" };
            float[] rowY = { 62f, 21f, -20f, -61f };

            for (int i = 0; i < rowKeys.Length; i++)
            {
                var kTmp = GetOrCreateText(infoBay.transform, "InfoKey_" + i);
                SetRect(kTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-126f, rowY[i]), new Vector2(110f, 22f), new Vector2(0f, 0.5f));
                StyleText(kTmp, _bodyFont, 13f, 1.5f, StatText, TextAlignmentOptions.MidlineLeft);
                kTmp.text = rowKeys[i];

                var vTmp = GetOrCreateText(infoBay.transform, "InfoVal_" + i);
                SetRect(vTmp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(126f, rowY[i]), new Vector2(165f, 22f), new Vector2(1f, 0.5f));
                StyleText(vTmp, _bodyFont, 13f, 1.2f, data.Theme, TextAlignmentOptions.MidlineRight);
                vTmp.text = rowVals[i];
            }

            // Inspect Button
            Transform btnTr = sideRt.Find("InspectButton");
            GameObject btnGo;
            if (btnTr != null)
            {
                btnGo = btnTr.gameObject;
            }
            else
            {
                btnGo = new GameObject("InspectButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.layer = sideRt.gameObject.layer;
                btnGo.transform.SetParent(sideRt, false);
            }

            var btnRt = btnGo.GetComponent<RectTransform>();
            SetRect(btnRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -172f), new Vector2(292f, 62f));

            var btnBg = btnGo.GetComponent<Image>();
            if (btnBg == null) btnBg = btnGo.AddComponent<Image>();
            btnBg.sprite = LoadoutUISprites.ButtonFill;
            btnBg.type = Image.Type.Sliced;
            btnBg.color = data.Theme;
            btnBg.raycastTarget = true;

            var btnFrame = GetOrCreateImage(btnRt, "InspectBtnFrame");
            btnFrame.sprite = LoadoutUISprites.ButtonFrame;
            btnFrame.type = Image.Type.Sliced;
            btnFrame.color = Color.Lerp(data.Theme, Color.white, 0.35f);
            Stretch(btnFrame.rectTransform);
            btnFrame.transform.SetSiblingIndex(0);

            Color darkLabel = new Color(data.Theme.r * 0.16f, data.Theme.g * 0.14f, data.Theme.b * 0.10f + 0.02f, 1f);

            var btnIcon = GetOrCreateImage(btnRt, "InspectBtnIcon");
            btnIcon.sprite = LoadoutUISprites.IconCrosshair;
            btnIcon.preserveAspect = true;
            btnIcon.color = darkLabel;
            SetRect(btnIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-58f, 0f), new Vector2(22f, 22f));

            var btnText = GetOrCreateText(btnRt, "InspectBtnText");
            SetRect(btnText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), new Vector2(220f, 48f));
            StyleText(btnText, _bodyFont, 22f, 3.5f, darkLabel, TextAlignmentOptions.Center);
            btnText.text = "INSPECT";

            inspectButton = btnGo.GetComponent<Button>();
            if (inspectButton == null) inspectButton = btnGo.AddComponent<Button>();
            inspectButton.targetGraphic = btnBg;
            var colors = inspectButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.88f, 0.92f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            inspectButton.colors = colors;

            int capturedIndex = sniperIndex;
            inspectButton.onClick.RemoveAllListeners();
            inspectButton.onClick.AddListener(() =>
            {
                SniperInspectionController.OpenInspection(capturedIndex);
            });

            // Initially hidden until the player selects Sniper 1 or Sniper 2
            sideRt.gameObject.SetActive(false);
            return sideRt;
        }

        /// <summary>
        /// Shows the side panel (with its Inspect button) next to the currently selected sniper
        /// and hides the other sniper's side panel.
        /// </summary>
        public void UpdateInspectSidePanels(int selectedSniperIndex, bool animate = false)
        {
            if (!_applied) Apply();

            bool show1 = (selectedSniperIndex == 0);
            bool show2 = (selectedSniperIndex == 1);

            if (_sidePanelAnimCoroutine != null)
            {
                StopCoroutine(_sidePanelAnimCoroutine);
                _sidePanelAnimCoroutine = null;
            }

            if (_sniper1SidePanel != null)
            {
                _sniper1SidePanel.gameObject.SetActive(show1);
                if (!show1 || !animate || !gameObject.activeInHierarchy)
                {
                    _sniper1SidePanel.anchoredPosition = new Vector2(-705f, 0f);
                    if (_sniper1SideGroup != null) _sniper1SideGroup.alpha = show1 ? 1f : 0f;
                }
            }

            if (_sniper2SidePanel != null)
            {
                _sniper2SidePanel.gameObject.SetActive(show2);
                if (!show2 || !animate || !gameObject.activeInHierarchy)
                {
                    _sniper2SidePanel.anchoredPosition = new Vector2(705f, 0f);
                    if (_sniper2SideGroup != null) _sniper2SideGroup.alpha = show2 ? 1f : 0f;
                }
            }

            if (animate && gameObject.activeInHierarchy)
            {
                if (show1 && _sniper1SidePanel != null)
                {
                    _sidePanelAnimCoroutine = StartCoroutine(AnimateSidePanelIn(_sniper1SidePanel, _sniper1SideGroup, -670f, -705f));
                }
                else if (show2 && _sniper2SidePanel != null)
                {
                    _sidePanelAnimCoroutine = StartCoroutine(AnimateSidePanelIn(_sniper2SidePanel, _sniper2SideGroup, 670f, 705f));
                }
            }
        }

        private System.Collections.IEnumerator AnimateSidePanelIn(RectTransform panelRt, CanvasGroup group, float startX, float endX)
        {
            float duration = 0.20f;
            float elapsed = 0f;
            if (group != null) group.alpha = 0f;
            panelRt.anchoredPosition = new Vector2(startX, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                panelRt.anchoredPosition = new Vector2(Mathf.LerpUnclamped(startX, endX, ease), 0f);
                if (group != null) group.alpha = ease;
                yield return null;
            }

            panelRt.anchoredPosition = new Vector2(endX, 0f);
            if (group != null) group.alpha = 1f;
            _sidePanelAnimCoroutine = null;
        }
        #endregion

        #region Back Button
        private void ApplyBackButton()
        {
            var back = transform.Find("LoadoutBackButton") as RectTransform;
            if (back == null) return;

            if (back.sizeDelta == Vector2.zero)
            {
                SetRect(back, new Vector2(0.5f, 0.5f), new Vector2(0f, -373f), new Vector2(280f, 50f));
            }

            var text = back.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                if (_bodyFont != null) text.font = _bodyFont;
                text.fontSize = 24f;
                text.characterSpacing = 6f;
                text.fontStyle = FontStyles.Normal;
                text.alignment = TextAlignmentOptions.Center;
            }

            var left = back.Find("BackLineLeft");
            if (left != null) left.gameObject.SetActive(false);

            var right = back.Find("BackLineRight");
            if (right != null) right.gameObject.SetActive(false);

            var btnEffect = back.GetComponent<MainMenuButtonEffect>();
            if (btnEffect != null)
            {
                btnEffect.InitializeComponents();
                btnEffect.FindRightArrow();

                // If right arrow was not found (e.g. if unsaved scene was reloaded), ensure fallback right arrow exists
                if (btnEffect.RightArrowTransform == null)
                {
                    var existingLeft = back.Find("HoverArrow") ?? back.Find("Arrow");
                    var rightArrowGo = new GameObject("HoverArrowRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                    rightArrowGo.layer = back.gameObject.layer;
                    rightArrowGo.transform.SetParent(back, false);

                    var rt = rightArrowGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(40f, 34f);
                    rt.localScale = Vector3.one;

                    var cg = rightArrowGo.GetComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;

                    var img = rightArrowGo.GetComponent<Image>();
                    img.raycastTarget = false;
                    img.sprite = MainMenuButtonEffect.GetDefaultRightArrow();
                    if (existingLeft != null)
                    {
                        var leftImg = existingLeft.GetComponent<Image>();
                        if (leftImg != null)
                        {
                            img.color = leftImg.color;
                        }
                    }

                    btnEffect.FindRightArrow();
                }

                btnEffect.CalculateArrowPositions();
                btnEffect.ApplyInstantIdleState();
            }
        }
        #endregion

        #region Helpers
        private static Image GetOrCreateImage(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            Image img;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                img = t.GetComponent<Image>();
                if (img == null) img = t.gameObject.AddComponent<Image>();
            }
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            TextMeshProUGUI tmp;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                tmp = go.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
            }
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void StyleText(TextMeshProUGUI tmp, TMP_FontAsset font, float size, float spacing, Color color, TextAlignmentOptions align)
        {
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.enableAutoSizing = false;
            tmp.characterSpacing = spacing;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
        }

        private static Transform FindChildStartingWith(Transform parent, string prefix)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name.StartsWith(prefix)) return c;
            }
            return null;
        }
        #endregion
    }
}
