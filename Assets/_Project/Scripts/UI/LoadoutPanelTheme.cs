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

                ApplyCard(container.Find("Sniper1Card"), Sniper1, -260f);
                ApplyCard(container.Find("Sniper2Card"), Sniper2, 260f);
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
        #endregion

        #region Back Button
        private void ApplyBackButton()
        {
            var back = transform.Find("LoadoutBackButton") as RectTransform;
            if (back == null) return;

            SetRect(back, new Vector2(0.5f, 0.5f), new Vector2(0f, -373f), new Vector2(220f, 50f));

            var text = back.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                if (_bodyFont != null) text.font = _bodyFont;
                text.fontSize = 24f;
                text.characterSpacing = 6f;
                text.fontStyle = FontStyles.Normal;
                text.alignment = TextAlignmentOptions.Center;
            }

            Color line = new Color(0.78f, 0.84f, 0.89f, 0.75f);
            var left = GetOrCreateImage(back, "BackLineLeft");
            left.color = line;
            SetRect(left.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-116f, 0f), new Vector2(64f, 2f));

            var right = GetOrCreateImage(back, "BackLineRight");
            right.color = line;
            SetRect(right.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(116f, 0f), new Vector2(64f, 2f));
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
