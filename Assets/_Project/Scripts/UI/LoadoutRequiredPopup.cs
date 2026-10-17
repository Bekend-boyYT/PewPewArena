using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Sci-fi modal popup displayed when a player attempts to start a game without selecting a loadout first.
    /// Matches the tactical cyan/dark-glass aesthetic:
    /// - "ATTENTION" header flanked by horizontal cyan laser lines.
    /// - 4-corner chamfered dark glass card with top-edge cyan flare and wireframe border.
    /// - "SELECT YOUR LOADOUT FIRST" main heading with cyan "LOADOUT" highlight.
    /// - "You must select your loadout before starting a game!" message.
    /// - "GO TO LOADOUT" primary glowing cyan button and "CLOSE" secondary button.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadoutRequiredPopup : MonoBehaviour
    {
        public const string DefaultPopupMessage = "You must select your loadout before starting a game!";

        private static readonly Color CyanBright = new Color(0.11f, 0.78f, 0.93f, 1f);
        private static readonly Color CyanLaser = new Color(0f, 0.90f, 1f, 0.95f);
        private static readonly Color CyanWireframe = new Color(0.05f, 0.78f, 0.92f, 0.48f);
        private static readonly Color CardBg = new Color(0.03f, 0.065f, 0.105f, 0.95f);
        private static readonly Color BackdropColor = new Color(0.015f, 0.035f, 0.065f, 0.90f);
        private static readonly Color SubtitleColor = new Color(0.30f, 0.72f, 0.84f, 1f);
        private static readonly Color AtmosphereColor = new Color(0.02f, 0.72f, 0.48f, 0.20f);

        // Primary Button ("GO TO LOADOUT") colors
        private static readonly Color PrimaryFillIdle = new Color(0.02f, 0.48f, 0.68f, 0.90f);
        private static readonly Color PrimaryFillHover = new Color(0.05f, 0.62f, 0.84f, 0.96f);
        private static readonly Color PrimaryBorderIdle = new Color(0.25f, 0.95f, 1f, 1f);
        private static readonly Color PrimaryBorderHover = new Color(0.65f, 1f, 1f, 1f);
        private static readonly Color PrimaryGlowIdle = new Color(0f, 0.85f, 1f, 0.88f);
        private static readonly Color PrimaryGlowHover = new Color(0.15f, 0.95f, 1f, 1f);

        // Secondary Button ("CLOSE") colors
        private static readonly Color SecondaryFillIdle = new Color(0.035f, 0.07f, 0.11f, 0.82f);
        private static readonly Color SecondaryFillHover = new Color(0.06f, 0.12f, 0.18f, 0.92f);
        private static readonly Color SecondaryBorderIdle = new Color(0.25f, 0.42f, 0.52f, 0.68f);
        private static readonly Color SecondaryBorderHover = new Color(0.42f, 0.68f, 0.82f, 0.92f);
        private static readonly Color SecondaryTextIdle = new Color(0.76f, 0.85f, 0.90f, 1f);
        private static readonly Color SecondaryTextHover = new Color(0.95f, 0.98f, 1f, 1f);

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private CanvasGroup _rootCanvasGroup;
        private RectTransform _cardTransform;
        private TextMeshProUGUI _messageText;
        private Button _goToLoadoutButton;
        private Button _closeButton;
        private TMP_FontAsset _font;
        private Coroutine _animCoroutine;
        private bool _built;

        private Action _onGoToLoadout;
        private Action _onClose;

        public bool IsVisible => gameObject.activeSelf;
        public string CurrentMessage => _messageText != null ? _messageText.text : DefaultPopupMessage;
        public Button GoToLoadoutButton => _goToLoadoutButton;
        public Button CloseButton => _closeButton;

        public void Initialize(TMP_FontAsset fallbackFont, Action onGoToLoadout, Action onClose)
        {
            if (_font == null)
            {
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF") ?? fallbackFont;
            }
            _onGoToLoadout = onGoToLoadout;
            _onClose = onClose;
            BuildIfNeeded();
        }

        public void Show(string message = DefaultPopupMessage)
        {
            BuildIfNeeded();

            if (_messageText != null)
            {
                _messageText.text = string.IsNullOrEmpty(message) ? DefaultPopupMessage : message;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
            }
            _animCoroutine = StartCoroutine(AnimateShow());
        }

        public void Hide()
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        private void BuildIfNeeded()
        {
            if (_built) return;
            _built = true;

            if (_font == null)
            {
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");
            }

            var rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();
            StretchFull(rootRt);
            rootRt.localScale = Vector3.one;

            _rootCanvasGroup = GetComponent<CanvasGroup>();
            if (_rootCanvasGroup == null) _rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            _rootCanvasGroup.alpha = 1f;
            _rootCanvasGroup.interactable = true;
            _rootCanvasGroup.blocksRaycasts = true;

            // 1. Fullscreen dark backdrop (blocks clicks to underlying screen)
            var backdrop = CreateImage(transform, "Backdrop", JoinUISprites.White, BackdropColor, Image.Type.Simple);
            backdrop.raycastTarget = true;
            StretchFull(backdrop.rectTransform);

            // 2. Ambient emerald/teal atmosphere glows matching reference image
            var leftGlow = CreateImage(transform, "LeftAtmosphereGlow", JoinUISprites.AtmosphereGlow, AtmosphereColor, Image.Type.Simple);
            SetRect(leftGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-340f, -80f), new Vector2(750f, 750f));

            var rightGlow = CreateImage(transform, "RightAtmosphereGlow", JoinUISprites.AtmosphereGlow, AtmosphereColor, Image.Type.Simple);
            SetRect(rightGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(360f, 120f), new Vector2(820f, 820f));

            // 3. Content container (scales smoothly on entrance)
            var contentGo = new GameObject("PopupContent", typeof(RectTransform));
            contentGo.layer = gameObject.layer;
            contentGo.transform.SetParent(transform, false);
            _cardTransform = contentGo.GetComponent<RectTransform>();
            SetRect(_cardTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f));

            // 4. "ATTENTION" Header + flanking cyan laser lines
            var attentionHeader = CreateText(_cardTransform, "AttentionHeader", "ATTENTION", 46f, 5f, Color.white);
            SetRect(attentionHeader.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 212f), new Vector2(340f, 64f));

            var leftLaser = CreateImage(attentionHeader.transform, "LeftLaserLine", JoinUISprites.LaserLine, CyanLaser, Image.Type.Simple);
            SetRect(leftLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-195f, 0f), new Vector2(95f, 2.5f));
            leftLaser.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var rightLaser = CreateImage(attentionHeader.transform, "RightLaserLine", JoinUISprites.LaserLine, CyanLaser, Image.Type.Simple);
            SetRect(rightLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(195f, 0f), new Vector2(95f, 2.5f));

            // 5. Chamfered Card Container
            var cardImg = CreateImage(_cardTransform, "PopupCard", JoinUISprites.CardFill, CardBg, Image.Type.Sliced);
            SetRect(cardImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(660f, 336f));

            var cardBorder = CreateImage(cardImg.transform, "CardBorder", JoinUISprites.CardFrame, CyanWireframe, Image.Type.Sliced);
            StretchFull(cardBorder.rectTransform);

            // Top-center cyan edge flare on the card border
            var topFlareSoft = CreateImage(cardImg.transform, "TopEdgeFlareSoft", GetTopFlareSprite(), new Color(0f, 0.90f, 1f, 0.55f), Image.Type.Simple);
            SetRect(topFlareSoft.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(340f, 28f));

            var topFlareCore = CreateImage(cardImg.transform, "TopEdgeFlareCore", GetTopFlareSprite(), new Color(0.55f, 0.98f, 1f, 0.95f), Image.Type.Simple);
            SetRect(topFlareCore.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(180f, 4f));

            // 6. Main Heading: "SELECT YOUR\nLOADOUT FIRST"
            var mainHeading = CreateText(cardImg.transform, "PopupMainHeading", "SELECT YOUR\n<color=#1DC8EE>LOADOUT</color> FIRST", 48f, 2.5f, Color.white);
            mainHeading.lineSpacing = -10f;
            mainHeading.richText = true;
            SetRect(mainHeading.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 68f), new Vector2(580f, 124f));

            // 7. Subtitle / Required Popup Message
            _messageText = CreateText(cardImg.transform, "PopupMessageText", DefaultPopupMessage, 16.5f, 3f, SubtitleColor);
            _messageText.fontStyle = FontStyles.UpperCase;
            SetRect(_messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(600f, 36f));

            // 8. Action Buttons: "GO TO LOADOUT" and "CLOSE" (equipped with MainMenuButtonEffect hover arrows)
            _goToLoadoutButton = CreatePopupButton(
                cardImg.transform,
                "GoToLoadoutButton",
                "GO TO LOADOUT",
                new Vector2(-135f, -92f),
                new Vector2(250f, 52f),
                isPrimary: true,
                onClick: () => _onGoToLoadout?.Invoke()
            );

            _closeButton = CreatePopupButton(
                cardImg.transform,
                "ClosePopupButton",
                "CLOSE",
                new Vector2(135f, -92f),
                new Vector2(250f, 52f),
                isPrimary: false,
                onClick: () => _onClose?.Invoke()
            );
        }

        private Button CreatePopupButton(Transform parent, string name, string labelText, Vector2 anchoredPos, Vector2 size, bool isPrimary, Action onClick)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.layer = gameObject.layer;
            btnGo.transform.SetParent(parent, false);

            var rt = btnGo.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 0.5f), anchoredPos, size);

            if (isPrimary)
            {
                var glowImg = CreateImage(btnGo.transform, "ButtonGlow", GetChamferButtonGlowSprite(), PrimaryGlowIdle, Image.Type.Sliced);
                glowImg.rectTransform.anchorMin = Vector2.zero;
                glowImg.rectTransform.anchorMax = Vector2.one;
                glowImg.rectTransform.offsetMin = new Vector2(-10f, -10f);
                glowImg.rectTransform.offsetMax = new Vector2(10f, 10f);
                glowImg.transform.SetSiblingIndex(0);
            }

            var fillImg = btnGo.GetComponent<Image>();
            fillImg.sprite = GetChamferButtonFillSprite();
            fillImg.type = Image.Type.Sliced;
            fillImg.color = isPrimary ? PrimaryFillIdle : SecondaryFillIdle;
            fillImg.raycastTarget = true;

            var borderImg = CreateImage(btnGo.transform, "ButtonBorder", GetChamferButtonFrameSprite(),
                isPrimary ? PrimaryBorderIdle : SecondaryBorderIdle, Image.Type.Sliced);
            StretchFull(borderImg.rectTransform);

            var label = CreateText(btnGo.transform, "Label", labelText, isPrimary ? 19.5f : 20.5f, isPrimary ? 2.5f : 3.5f,
                isPrimary ? Color.white : SecondaryTextIdle);
            StretchFull(label.rectTransform);

            var btn = btnGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = fillImg;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var effect = btnGo.AddComponent<MainMenuButtonEffect>();
            effect.ConfigureColors(
                fillIdle: isPrimary ? PrimaryFillIdle : SecondaryFillIdle,
                fillHover: isPrimary ? PrimaryFillHover : SecondaryFillHover,
                borderIdle: isPrimary ? PrimaryBorderIdle : SecondaryBorderIdle,
                borderHover: isPrimary ? PrimaryBorderHover : SecondaryBorderHover,
                textIdle: isPrimary ? Color.white : SecondaryTextIdle,
                textHover: isPrimary ? Color.white : SecondaryTextHover,
                clickText: isPrimary ? Color.white : SecondaryTextHover,
                arrowCol: isPrimary ? Color.white : new Color(0.85f, 0.92f, 0.98f, 1f)
            );
            effect.InitializeComponents();
            effect.FindRightArrow();
            effect.ConfigureArrows(new Vector2(34f, 29f), margin: 10f, slideDistance: 5f);
            effect.ApplyInstantIdleState();

            return btn;
        }

        private IEnumerator AnimateShow()
        {
            float duration = 0.18f;
            float elapsed = 0f;

            if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = 0f;
            if (_cardTransform != null) _cardTransform.localScale = new Vector3(0.94f, 0.94f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = ease;
                if (_cardTransform != null) _cardTransform.localScale = Vector3.LerpUnclamped(new Vector3(0.94f, 0.94f, 1f), Vector3.one, ease);

                yield return null;
            }

            if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = 1f;
            if (_cardTransform != null) _cardTransform.localScale = Vector3.one;
            _animCoroutine = null;
        }

        #region UI Builders & Procedural Sprites
        private Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Image.Type type)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, float spacing, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) tmp.font = _font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.characterSpacing = spacing;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static Sprite GetChamferButtonFillSprite()
        {
            return BuildSprite("popup_btn_fill", 48, 48,
                (x, y) => Chamfer4(x, y, 48f, 9f, 0f) ? 1f : 0f,
                new Vector4(12, 12, 12, 12));
        }

        private static Sprite GetChamferButtonFrameSprite()
        {
            return BuildSprite("popup_btn_frame", 48, 48,
                (x, y) => Chamfer4(x, y, 48f, 9f, 0f) && !Chamfer4(x, y, 48f, 9f, 2f) ? 1f : 0f,
                new Vector4(12, 12, 12, 12));
        }

        private static Sprite GetChamferButtonGlowSprite()
        {
            return BuildSprite("popup_btn_glow", 64, 64, (x, y) =>
            {
                const float pad = 10f;
                const float inner = 64f - 2f * pad;
                const float chamfer = 9f;
                float rx = x - pad;
                float ry = y - pad;

                if (Chamfer4(rx, ry, inner, chamfer, 2f))
                    return 0.15f;
                if (Chamfer4(rx, ry, inner, chamfer, 0f))
                    return 1f;

                float dx = rx < 0f ? -rx : (rx > inner ? rx - inner : 0f);
                float dy = ry < 0f ? -ry : (ry > inner ? ry - inner : 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float g = Mathf.Clamp01(1f - dist / pad);
                return g * g;
            }, new Vector4(20, 20, 20, 20));
        }

        private static Sprite GetTopFlareSprite()
        {
            return BuildSprite("popup_top_flare", 128, 32, (x, y) =>
            {
                float u = (x / 128f - 0.5f) * 2f;
                float v = (y / 32f - 0.5f) * 2f;
                float hFade = Mathf.Clamp01(1f - Mathf.Abs(u));
                float vFade = Mathf.Clamp01(1f - Mathf.Abs(v));
                return hFade * hFade * vFade * vFade;
            }, Vector4.zero, 2);
        }

        private static bool Chamfer4(float x, float y, float size, float chamfer, float inset)
        {
            if (x < inset || y < inset || x > size - inset || y > size - inset) return false;
            float diag = chamfer + inset * 1.41421f;
            if (x + y < diag) return false;
            if (x + (size - y) < diag) return false;
            if ((size - x) + y < diag) return false;
            if ((size - x) + (size - y) < diag) return false;
            return true;
        }

        private static Sprite BuildSprite(string key, int w, int h, Func<float, float, float> coverage, Vector4 border, int ss = 4)
        {
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "PopupUI_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            var px = new Color32[w * h];
            float inv = 1f / ss;
            float norm = 1f / (ss * ss);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            sum += coverage(x + (sx + 0.5f) * inv, y + (sy + 0.5f) * inv);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(sum * norm) * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = key;
            sprite.hideFlags = HideFlags.DontSave;
            SpriteCache[key] = sprite;
            return sprite;
        }
        #endregion
    }
}

