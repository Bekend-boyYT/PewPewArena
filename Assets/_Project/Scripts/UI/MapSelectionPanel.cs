using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SniperGame.UI;

namespace PewPewArena.UI
{
    /// <summary>
    /// Dedicated visual Map Selection Panel opened from CreateGamePanel ("Choose" map button).
    /// Displays visual preview cards for available maps ("Shipment Cargo" and "Sandoria") with
    /// genuine map screenshots, hover/selected visual states, and Select / Back actions.
    /// </summary>
    [DisallowMultipleComponent]
    public class MapSelectionPanel : MonoBehaviour
    {
        private static readonly Color BackdropColor = new Color(0.012f, 0.028f, 0.055f, 0.94f);
        private static readonly Color AtmosphereColor = new Color(0.02f, 0.70f, 0.48f, 0.18f);
        private static readonly Color CyanLaser = new Color(0f, 0.90f, 1f, 0.95f);
        private static readonly Color SubtitleColor = new Color(0.55f, 0.72f, 0.82f, 0.95f);

        private static readonly Color CardBgIdle = new Color(0.024f, 0.055f, 0.095f, 0.94f);
        private static readonly Color CardBgHover = new Color(0.038f, 0.085f, 0.140f, 0.98f);
        private static readonly Color CardBgSelected = new Color(0.032f, 0.095f, 0.155f, 0.98f);

        private static readonly Color CardBorderIdle = new Color(0.08f, 0.55f, 0.72f, 0.52f);
        private static readonly Color CardBorderHover = new Color(0.15f, 0.90f, 1f, 0.92f);
        private static readonly Color CardBorderSelected = new Color(0f, 0.95f, 1f, 1f);

        private static readonly Color SelectBtnFillIdle = new Color(0.025f, 0.44f, 0.64f, 0.90f);
        private static readonly Color SelectBtnFillHover = new Color(0.06f, 0.62f, 0.84f, 0.98f);
        private static readonly Color SelectBtnFillSelected = new Color(0.04f, 0.58f, 0.78f, 0.96f);
        private static readonly Color SelectBtnBorderIdle = new Color(0.25f, 0.90f, 1f, 0.92f);
        private static readonly Color SelectBtnBorderHover = Color.white;

        private static readonly Color BackFillIdle = new Color(0.06f, 0.09f, 0.14f, 0.86f);
        private static readonly Color BackFillHover = new Color(0.11f, 0.16f, 0.22f, 0.95f);
        private static readonly Color BackBorderIdle = new Color(0.30f, 0.40f, 0.48f, 0.62f);
        private static readonly Color BackBorderHover = new Color(0.45f, 0.70f, 0.84f, 0.92f);
        private static readonly Color BackTextIdle = new Color(0.72f, 0.80f, 0.86f, 1f);

        private static readonly Dictionary<string, Sprite> PreviewSpriteCache = new Dictionary<string, Sprite>();

        private class MapCardRefs
        {
            public int Index;
            public RectTransform RootRt;
            public Image CardBg;
            public Image CardBorder;
            public Image CardOuterGlow;
            public Image PreviewImage;
            public AspectRatioFitter PreviewFitter;
            public Image PreviewFrameBorder;
            public Image SelectedBadgeBg;
            public TextMeshProUGUI SelectedBadgeText;
            public TextMeshProUGUI MapTagText;
            public TextMeshProUGUI MapTitleText;
            public Button CardButton;
            public Button SelectButton;
            public Image SelectBtnBg;
            public Image SelectBtnBorder;
            public TextMeshProUGUI SelectBtnLabel;
        }

        private CanvasGroup _rootCanvasGroup;
        private RectTransform _contentRt;
        private TMP_FontAsset _font;
        private TMP_FontAsset _komikaFont;
        private RelayNetworkUI.MapOption[] _maps;
        private Action<int> _onMapSelected;
        private Action _onClose;
        private readonly List<MapCardRefs> _cards = new List<MapCardRefs>();
        private Button _backButton;
        private int _currentSelectedIndex = -1;
        private Coroutine _animCoroutine;
        private bool _built;

        public bool IsVisible => gameObject.activeSelf;
        public Button BackButton => _backButton;
        public int CardCount => _cards.Count;

        public void Initialize(
            TMP_FontAsset fallbackFont,
            TMP_FontAsset komikaFont,
            RelayNetworkUI.MapOption[] maps,
            Action<int> onMapSelected,
            Action onClose)
        {
            if (_font == null)
            {
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF") ?? fallbackFont;
            }
            if (_komikaFont == null)
            {
                _komikaFont = komikaFont;
            }

            _maps = maps;
            _onMapSelected = onMapSelected;
            _onClose = onClose;

            BuildIfNeeded();
            RefreshCardsContent();
        }

        public void Show(int currentSelectedIndex)
        {
            BuildIfNeeded();
            _currentSelectedIndex = currentSelectedIndex;
            RefreshCardsContent();
            UpdateSelectionVisuals(_currentSelectedIndex);

            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
            }
            _animCoroutine = StartCoroutine(AnimateOpen());
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

        public void SelectMapFromCard(int index)
        {
            _currentSelectedIndex = index;
            UpdateSelectionVisuals(_currentSelectedIndex);
            _onMapSelected?.Invoke(index);
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

            // 1. Fullscreen dark backdrop (clicking backdrop closes panel without changing selection)
            var backdrop = CreateImage(transform, "Backdrop", JoinUISprites.White, BackdropColor, Image.Type.Simple);
            backdrop.raycastTarget = true;
            StretchFull(backdrop.rectTransform);
            var backdropBtn = backdrop.gameObject.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(() => _onClose?.Invoke());

            // 2. Ambient atmosphere glows
            var leftGlow = CreateImage(transform, "LeftAtmosphereGlow", JoinUISprites.AtmosphereGlow, AtmosphereColor, Image.Type.Simple);
            SetRect(leftGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(760f, 760f));

            var rightGlow = CreateImage(transform, "RightAtmosphereGlow", JoinUISprites.AtmosphereGlow, AtmosphereColor, Image.Type.Simple);
            SetRect(rightGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(420f, 0f), new Vector2(760f, 760f));

            // 3. Content container
            var contentGo = new GameObject("MapSelectionContent", typeof(RectTransform));
            contentGo.layer = gameObject.layer;
            contentGo.transform.SetParent(transform, false);
            _contentRt = contentGo.GetComponent<RectTransform>();
            SetRect(_contentRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1140f, 760f));

            // 4. Header: "SELECT MAP" + flanking cyan laser lines + subtitle
            var headerText = CreateText(_contentRt, "SelectMapHeader", "SELECT MAP", 52f, 4f, Color.white);
            headerText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            SetRect(headerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 326f), new Vector2(460f, 68f));

            var leftLaser = CreateImage(headerText.transform, "LeftLaserLine", JoinUISprites.LaserLine, CyanLaser, Image.Type.Simple);
            SetRect(leftLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-245f, 0f), new Vector2(140f, 2.8f));
            leftLaser.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var rightLaser = CreateImage(headerText.transform, "RightLaserLine", JoinUISprites.LaserLine, CyanLaser, Image.Type.Simple);
            SetRect(rightLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(245f, 0f), new Vector2(140f, 2.8f));

            var subtitleText = CreateText(_contentRt, "SelectMapSubtitle", "CHOOSE YOUR ARENA DEPLOYMENT ZONE", 16f, 5f, SubtitleColor);
            subtitleText.fontStyle = FontStyles.Bold;
            SetRect(subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 276f), new Vector2(560f, 28f));

            // 5. Cards Container
            var cardsContainerGo = new GameObject("MapCardsContainer", typeof(RectTransform));
            cardsContainerGo.layer = gameObject.layer;
            cardsContainerGo.transform.SetParent(_contentRt, false);
            var cardsContainerRt = cardsContainerGo.GetComponent<RectTransform>();
            SetRect(cardsContainerRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(1040f, 470f));

            _cards.Clear();
            _cards.Add(BuildMapCard(cardsContainerRt, 0, new Vector2(-262f, 0f)));
            _cards.Add(BuildMapCard(cardsContainerRt, 1, new Vector2(262f, 0f)));

            // 6. Bottom BACK / CLOSE button
            _backButton = BuildBackButton(_contentRt, new Vector2(0f, -304f), new Vector2(280f, 52f));
        }

        private MapCardRefs BuildMapCard(Transform parent, int index, Vector2 anchoredPos)
        {
            var cardRefs = new MapCardRefs { Index = index };
            const float cardW = 484f;
            const float cardH = 456f;

            var cardGo = new GameObject($"MapCard_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cardGo.layer = gameObject.layer;
            cardGo.transform.SetParent(parent, false);

            cardRefs.RootRt = cardGo.GetComponent<RectTransform>();
            SetRect(cardRefs.RootRt, new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(cardW, cardH));

            // Outer glow for selected/hover state
            cardRefs.CardOuterGlow = CreateImage(cardGo.transform, "CardOuterGlow", JoinUISprites.ChamferGlowFrame,
                new Color(0f, 0.88f, 1f, 0f), Image.Type.Sliced);
            cardRefs.CardOuterGlow.rectTransform.anchorMin = Vector2.zero;
            cardRefs.CardOuterGlow.rectTransform.anchorMax = Vector2.one;
            cardRefs.CardOuterGlow.rectTransform.offsetMin = new Vector2(-9f, -9f);
            cardRefs.CardOuterGlow.rectTransform.offsetMax = new Vector2(9f, 9f);
            cardRefs.CardOuterGlow.transform.SetSiblingIndex(0);

            // Chamfered Card Background
            cardRefs.CardBg = cardGo.GetComponent<Image>();
            cardRefs.CardBg.sprite = JoinUISprites.CardFill;
            cardRefs.CardBg.type = Image.Type.Sliced;
            cardRefs.CardBg.color = CardBgIdle;
            cardRefs.CardBg.raycastTarget = true;

            // Chamfered Card Frame Border
            cardRefs.CardBorder = CreateImage(cardGo.transform, "CardBorder", JoinUISprites.CardFrame, CardBorderIdle, Image.Type.Sliced);
            StretchFull(cardRefs.CardBorder.rectTransform);

            // Preview Viewport with RectMask2D so the map screenshot fills the frame cleanly without distortion
            var viewportGo = new GameObject("PreviewViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportGo.layer = gameObject.layer;
            viewportGo.transform.SetParent(cardGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            SetRect(viewportRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 68f), new Vector2(444f, 268f));

            var viewportBg = viewportGo.GetComponent<Image>();
            viewportBg.sprite = JoinUISprites.White;
            viewportBg.color = new Color(0.01f, 0.025f, 0.045f, 1f);
            viewportBg.raycastTarget = false;

            // Map Preview Image inside masked viewport
            var previewGo = new GameObject("MapPreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            previewGo.layer = gameObject.layer;
            previewGo.transform.SetParent(viewportRt, false);
            cardRefs.PreviewImage = previewGo.GetComponent<Image>();
            cardRefs.PreviewImage.color = Color.white;
            cardRefs.PreviewImage.preserveAspect = true;
            cardRefs.PreviewImage.raycastTarget = false;

            cardRefs.PreviewFitter = previewGo.GetComponent<AspectRatioFitter>();
            cardRefs.PreviewFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            cardRefs.PreviewFitter.aspectRatio = 16f / 9f;

            // Subtle bottom vignette inside viewport for tactical depth
            var vignette = CreateImage(viewportRt, "BottomVignette", JoinUISprites.White, new Color(0.015f, 0.04f, 0.075f, 0.42f), Image.Type.Simple);
            SetRect(vignette.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(444f, 48f));

            // Crisp frame around preview viewport
            cardRefs.PreviewFrameBorder = CreateImage(cardGo.transform, "PreviewFrameBorder", JoinUISprites.ChamferBoxFrame,
                new Color(0.10f, 0.72f, 0.90f, 0.65f), Image.Type.Sliced);
            SetRect(cardRefs.PreviewFrameBorder.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 68f), new Vector2(446f, 270f));

            // "SELECTED" status badge in top-right of preview viewport
            cardRefs.SelectedBadgeBg = CreateImage(cardGo.transform, "SelectedBadge", JoinUISprites.ChamferBoxFill,
                new Color(0.02f, 0.52f, 0.74f, 0.96f), Image.Type.Sliced);
            SetRect(cardRefs.SelectedBadgeBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(142f, 174f), new Vector2(132f, 32f));

            var badgeBorder = CreateImage(cardRefs.SelectedBadgeBg.transform, "BadgeBorder", JoinUISprites.ChamferBoxFrame,
                new Color(0.45f, 0.98f, 1f, 1f), Image.Type.Sliced);
            StretchFull(badgeBorder.rectTransform);

            cardRefs.SelectedBadgeText = CreateText(cardRefs.SelectedBadgeBg.transform, "BadgeText", "SELECTED", 13.5f, 2.5f, Color.white);
            cardRefs.SelectedBadgeText.fontStyle = FontStyles.Bold;
            StretchFull(cardRefs.SelectedBadgeText.rectTransform);

            // Tactical Environment Subtitle Tag
            string envTag = index == 0 ? "/// MARITIME CONTAINER DOCKS" : "/// ARID DESERT OUTPOST";
            cardRefs.MapTagText = CreateText(cardGo.transform, "MapTagText", envTag, 13f, 2.5f, new Color(0.15f, 0.82f, 0.96f, 0.90f));
            cardRefs.MapTagText.alignment = TextAlignmentOptions.Center;
            cardRefs.MapTagText.fontStyle = FontStyles.Bold;
            SetRect(cardRefs.MapTagText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(420f, 22f));

            // Map Display Name ("Shipment Cargo" / "Sandoria")
            string defaultName = index == 0 ? "Shipment Cargo" : "Sandoria";
            cardRefs.MapTitleText = CreateText(cardGo.transform, "MapTitleText", defaultName, 29f, 2f, Color.white);
            if (_komikaFont != null) cardRefs.MapTitleText.font = _komikaFont;
            cardRefs.MapTitleText.fontStyle = FontStyles.Bold;
            SetRect(cardRefs.MapTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -126f), new Vector2(430f, 40f));

            // Select Button on the card
            var selectBtnGo = new GameObject("SelectMapButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            selectBtnGo.layer = gameObject.layer;
            selectBtnGo.transform.SetParent(cardGo.transform, false);
            var selectBtnRt = selectBtnGo.GetComponent<RectTransform>();
            SetRect(selectBtnRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -182f), new Vector2(260f, 46f));

            cardRefs.SelectBtnBg = selectBtnGo.GetComponent<Image>();
            cardRefs.SelectBtnBg.sprite = JoinUISprites.ChamferBoxFill;
            cardRefs.SelectBtnBg.type = Image.Type.Sliced;
            cardRefs.SelectBtnBg.color = SelectBtnFillIdle;
            cardRefs.SelectBtnBg.raycastTarget = true;

            cardRefs.SelectBtnBorder = CreateImage(selectBtnGo.transform, "ButtonBorder", JoinUISprites.ChamferBoxFrame,
                SelectBtnBorderIdle, Image.Type.Sliced);
            StretchFull(cardRefs.SelectBtnBorder.rectTransform);

            cardRefs.SelectBtnLabel = CreateText(selectBtnGo.transform, "Label", "SELECT", 18.5f, 3f, Color.white);
            cardRefs.SelectBtnLabel.fontStyle = FontStyles.Bold;
            StretchFull(cardRefs.SelectBtnLabel.rectTransform);

            int capturedIndex = index;

            cardRefs.SelectButton = selectBtnGo.GetComponent<Button>();
            cardRefs.SelectButton.transition = Selectable.Transition.None;
            cardRefs.SelectButton.targetGraphic = cardRefs.SelectBtnBg;
            cardRefs.SelectButton.onClick.AddListener(() => SelectMapFromCard(capturedIndex));

            cardRefs.CardButton = cardGo.GetComponent<Button>();
            cardRefs.CardButton.transition = Selectable.Transition.None;
            cardRefs.CardButton.targetGraphic = cardRefs.CardBg;
            cardRefs.CardButton.onClick.AddListener(() => SelectMapFromCard(capturedIndex));

            var hoverFx = cardGo.AddComponent<MapCardHoverEffect>();
            hoverFx.Init(cardRefs.RootRt, cardRefs.CardBg, cardRefs.CardBorder, cardRefs.CardOuterGlow,
                cardRefs.SelectBtnBg, cardRefs.SelectBtnBorder, () => _currentSelectedIndex == capturedIndex);

            return cardRefs;
        }

        private Button BuildBackButton(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var btnGo = new GameObject("MapPanelBackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.layer = gameObject.layer;
            btnGo.transform.SetParent(parent, false);

            var rt = btnGo.GetComponent<RectTransform>();
            SetRect(rt, new Vector2(0.5f, 0.5f), anchoredPos, size);

            var fillImg = btnGo.GetComponent<Image>();
            fillImg.sprite = JoinUISprites.ChamferBoxFill;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = BackFillIdle;
            fillImg.raycastTarget = true;

            var borderImg = CreateImage(btnGo.transform, "ButtonBorder", JoinUISprites.ChamferBoxFrame, BackBorderIdle, Image.Type.Sliced);
            StretchFull(borderImg.rectTransform);

            var label = CreateText(btnGo.transform, "Label", "BACK", 21f, 3.5f, BackTextIdle);
            label.fontStyle = FontStyles.Bold;
            StretchFull(label.rectTransform);

            var btn = btnGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = fillImg;
            btn.onClick.AddListener(() => _onClose?.Invoke());

            var effect = btnGo.AddComponent<MainMenuButtonEffect>();
            effect.ConfigureColors(
                fillIdle: BackFillIdle,
                fillHover: BackFillHover,
                borderIdle: BackBorderIdle,
                borderHover: BackBorderHover,
                textIdle: BackTextIdle,
                textHover: Color.white
            );
            effect.InitializeComponents();
            effect.CalculateArrowPositions();
            effect.ApplyInstantIdleState();

            return btn;
        }

        private void RefreshCardsContent()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                string displayName = (i == 0) ? "Shipment Cargo" : "Sandoria";
                if (_maps != null && i < _maps.Length && !string.IsNullOrWhiteSpace(_maps[i].displayName))
                {
                    displayName = _maps[i].displayName;
                }

                if (card.MapTitleText != null)
                {
                    if (_komikaFont != null) card.MapTitleText.font = _komikaFont;
                    card.MapTitleText.text = displayName;
                }

                Sprite previewSprite = LoadMapPreviewSprite(i, displayName);
                if (card.PreviewImage != null && previewSprite != null)
                {
                    card.PreviewImage.sprite = previewSprite;
                    if (card.PreviewFitter != null && previewSprite.texture != null && previewSprite.texture.height > 0)
                    {
                        card.PreviewFitter.aspectRatio = (float)previewSprite.texture.width / previewSprite.texture.height;
                    }
                }
            }
        }

        private void UpdateSelectionVisuals(int selectedIndex)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                bool isSelected = (i == selectedIndex);

                if (card.CardBg != null)
                    card.CardBg.color = isSelected ? CardBgSelected : CardBgIdle;
                if (card.CardBorder != null)
                    card.CardBorder.color = isSelected ? CardBorderSelected : CardBorderIdle;
                if (card.CardOuterGlow != null)
                    card.CardOuterGlow.color = isSelected ? new Color(0f, 0.90f, 1f, 0.78f) : new Color(0f, 0.88f, 1f, 0f);
                if (card.PreviewFrameBorder != null)
                    card.PreviewFrameBorder.color = isSelected ? new Color(0.25f, 0.96f, 1f, 0.95f) : new Color(0.10f, 0.72f, 0.90f, 0.65f);

                if (card.SelectedBadgeBg != null)
                    card.SelectedBadgeBg.gameObject.SetActive(isSelected);

                if (card.SelectBtnBg != null)
                    card.SelectBtnBg.color = isSelected ? SelectBtnFillSelected : SelectBtnFillIdle;
                if (card.SelectBtnBorder != null)
                    card.SelectBtnBorder.color = isSelected ? Color.white : SelectBtnBorderIdle;
                if (card.SelectBtnLabel != null)
                    card.SelectBtnLabel.text = isSelected ? "SELECTED" : "SELECT";
            }
        }

        private static Sprite LoadMapPreviewSprite(int index, string displayName)
        {
            string resourceKey = index == 0 ? "MapPreviews/ShipmentCargo" : "MapPreviews/Sandoria";
            if ( displayName != null && displayName.IndexOf("Sandoria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                resourceKey = "MapPreviews/Sandoria";
            }
            else if (displayName != null && displayName.IndexOf("Shipment", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                resourceKey = "MapPreviews/ShipmentCargo";
            }

            if (PreviewSpriteCache.TryGetValue(resourceKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite loadedSprite = Resources.Load<Sprite>(resourceKey);
            if (loadedSprite != null)
            {
                PreviewSpriteCache[resourceKey] = loadedSprite;
                return loadedSprite;
            }

            Texture2D loadedTex = Resources.Load<Texture2D>(resourceKey);
            if (loadedTex != null)
            {
                loadedSprite = CreateSpriteFromTexture(loadedTex, resourceKey);
                PreviewSpriteCache[resourceKey] = loadedSprite;
                return loadedSprite;
            }

#if UNITY_EDITOR
            string assetPath = $"Assets/Resources/{resourceKey}.jpg";
            loadedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (loadedSprite != null)
            {
                PreviewSpriteCache[resourceKey] = loadedSprite;
                return loadedSprite;
            }
            loadedTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (loadedTex != null)
            {
                loadedSprite = CreateSpriteFromTexture(loadedTex, resourceKey);
                PreviewSpriteCache[resourceKey] = loadedSprite;
                return loadedSprite;
            }
#endif

            string diskPath = Path.Combine(Application.dataPath, "Resources", resourceKey + ".jpg");
            if (File.Exists(diskPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(diskPath);
                    var rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        name = "MapPreview_" + resourceKey,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    if (rawTex.LoadImage(bytes, false))
                    {
                        loadedSprite = CreateSpriteFromTexture(rawTex, resourceKey);
                        PreviewSpriteCache[resourceKey] = loadedSprite;
                        return loadedSprite;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[MapSelectionPanel] Failed to read map preview from disk ({diskPath}): {e.Message}");
                }
            }

            return JoinUISprites.White;
        }

        private static Sprite CreateSpriteFromTexture(Texture2D tex, string name)
        {
            var sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = name;
            return sprite;
        }

        private IEnumerator AnimateOpen()
        {
            const float duration = 0.18f;
            float elapsed = 0f;

            if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = 0f;
            if (_contentRt != null) _contentRt.localScale = new Vector3(0.95f, 0.95f, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = ease;
                if (_contentRt != null) _contentRt.localScale = Vector3.LerpUnclamped(new Vector3(0.95f, 0.95f, 1f), Vector3.one, ease);
                yield return null;
            }

            if (_rootCanvasGroup != null) _rootCanvasGroup.alpha = 1f;
            if (_contentRt != null) _contentRt.localScale = Vector3.one;
            _animCoroutine = null;
        }

        #region Helpers
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
        #endregion
    }

    /// <summary>
    /// Interactive hover/press feedback for map preview cards inside MapSelectionPanel.
    /// </summary>
    public class MapCardHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _cardRt;
        private Image _cardBg;
        private Image _cardBorder;
        private Image _cardGlow;
        private Image _btnBg;
        private Image _btnBorder;
        private Func<bool> _isSelectedFunc;
        private bool _hovered;
        private bool _pressed;

        private static readonly Color BgIdle = new Color(0.024f, 0.055f, 0.095f, 0.94f);
        private static readonly Color BgHover = new Color(0.038f, 0.085f, 0.140f, 0.98f);
        private static readonly Color BgSelected = new Color(0.032f, 0.095f, 0.155f, 0.98f);

        private static readonly Color BorderIdle = new Color(0.08f, 0.55f, 0.72f, 0.52f);
        private static readonly Color BorderHover = new Color(0.18f, 0.92f, 1f, 0.95f);
        private static readonly Color BorderSelected = new Color(0f, 0.95f, 1f, 1f);

        private static readonly Color BtnFillIdle = new Color(0.025f, 0.44f, 0.64f, 0.90f);
        private static readonly Color BtnFillHover = new Color(0.06f, 0.62f, 0.84f, 0.98f);
        private static readonly Color BtnBorderIdle = new Color(0.25f, 0.90f, 1f, 0.92f);

        public void Init(
            RectTransform cardRt,
            Image cardBg,
            Image cardBorder,
            Image cardGlow,
            Image btnBg,
            Image btnBorder,
            Func<bool> isSelectedFunc)
        {
            _cardRt = cardRt;
            _cardBg = cardBg;
            _cardBorder = cardBorder;
            _cardGlow = cardGlow;
            _btnBg = btnBg;
            _btnBorder = btnBorder;
            _isSelectedFunc = isSelectedFunc;
        }

        private void OnDisable()
        {
            _hovered = false;
            _pressed = false;
            if (_cardRt != null) _cardRt.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            ApplyVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            bool isSelected = _isSelectedFunc != null && _isSelectedFunc();

            if (_cardRt != null)
            {
                float scale = _pressed ? 0.985f : (_hovered ? 1.02f : 1f);
                _cardRt.localScale = new Vector3(scale, scale, 1f);
            }

            if (_cardBg != null)
            {
                _cardBg.color = _hovered ? BgHover : (isSelected ? BgSelected : BgIdle);
            }

            if (_cardBorder != null)
            {
                _cardBorder.color = isSelected ? BorderSelected : (_hovered ? BorderHover : BorderIdle);
            }

            if (_cardGlow != null)
            {
                float alpha = isSelected ? 0.78f : (_hovered ? 0.45f : 0f);
                _cardGlow.color = new Color(0f, 0.90f, 1f, alpha);
            }

            if (_btnBg != null)
            {
                _btnBg.color = _hovered || isSelected ? BtnFillHover : BtnFillIdle;
            }

            if (_btnBorder != null)
            {
                _btnBorder.color = _hovered || isSelected ? Color.white : BtnBorderIdle;
            }
        }
    }
}
