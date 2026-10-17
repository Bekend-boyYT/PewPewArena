using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PewPewArena.UI
{
    /// <summary>
    /// Persistent full-screen sci-fi Loading Screen matching the reference design:
    /// - "LOADING GAME" bold italic header flanked by glowing cyan laser lines.
    /// - "PREPARING MATCH..." / "PREPARING TUTORIAL..." subtitle.
    /// - Dynamic percentage ("74%") and stage readout ("LOADING RESOURCES...").
    /// - Chamfered progress bar track with cyan-emerald-bright-cyan gradient fill and leading glow.
    /// - Left metadata badge with location pin icon + "MAP: SHIPMENT CARGO" / "MAP: SANDORIA" / "MAP: TUTORIAL".
    /// - Right metadata label "STATUS: SYNCHRONIZING PLAYERS..." / "STATUS: TUTORIAL MAP LOADING...".
    /// - Bottom chamfered tip card ("TIP: CHOOSE YOUR WEAPONS IN THE LOADOUT MENU BEFORE THE MATCH STARTS.").
    /// </summary>
    [DisallowMultipleComponent]
    public class LoadingScreenController : MonoBehaviour
    {
        private static LoadingScreenController _instance;
        public static LoadingScreenController Instance
        {
            get
            {
                if (_instance == null)
                {
                    EnsureInstance();
                }
                return _instance;
            }
        }

        private static readonly Color BaseBgColor = new Color(0.025f, 0.045f, 0.085f, 1f);
        private static readonly Color BgSpriteTint = new Color(0.22f, 0.30f, 0.42f, 0.55f);
        private static readonly Color EmeraldGlowColor = new Color(0.04f, 0.82f, 0.44f, 0.28f);
        private static readonly Color CyanGlowColor = new Color(0.02f, 0.72f, 0.92f, 0.20f);
        private static readonly Color CyanLaserColor = new Color(0.05f, 0.90f, 1f, 0.95f);
        private static readonly Color CyanAccentText = new Color(0.22f, 0.80f, 0.92f, 1f);
        private static readonly Color MetadataValueColor = new Color(0.76f, 0.84f, 0.90f, 1f);

        private static readonly Color TrackBgColor = new Color(0.02f, 0.06f, 0.095f, 0.92f);
        private static readonly Color TrackOuterBorderColor = new Color(0.12f, 0.74f, 0.86f, 0.75f);
        private static readonly Color TrackInnerBorderColor = new Color(0.08f, 0.45f, 0.56f, 0.45f);

        private static readonly Color BadgeBgColor = new Color(0.02f, 0.13f, 0.20f, 0.95f);
        private static readonly Color BadgeBorderColor = new Color(0.08f, 0.76f, 0.95f, 0.72f);
        private static readonly Color IconCyanColor = new Color(0.10f, 0.92f, 1f, 1f);

        private static readonly Color TipCardBgColor = new Color(0.022f, 0.075f, 0.115f, 0.86f);
        private static readonly Color TipCardBorderColor = new Color(0.10f, 0.65f, 0.80f, 0.62f);

        private const float BarFullWidth = 684f;
        private const float BarHeight = 40f;

        private static Sprite _progressFillSprite;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _bgTextureImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _subtitleText;
        private TextMeshProUGUI _percentText;
        private TextMeshProUGUI _stageText;
        private RectTransform _progressFillRt;
        private RectTransform _progressLeadGlowRt;
        private TextMeshProUGUI _mapValueText;
        private TextMeshProUGUI _statusValueText;
        private TextMeshProUGUI _tipText;
        private TMP_FontAsset _font;
        private Coroutine _activeLoadCoroutine;
        private bool _built;

        public bool IsShowing => gameObject.activeSelf && _canvasGroup != null && _canvasGroup.alpha > 0.01f;

        public static LoadingScreenController EnsureInstance(TMP_FontAsset fallbackFont = null, Sprite backgroundSprite = null)
        {
            if (_instance != null)
            {
                _instance.ConfigureAssets(fallbackFont, backgroundSprite);
                return _instance;
            }

            var existing = FindAnyObjectByType<LoadingScreenController>();
            if (existing != null)
            {
                _instance = existing;
                _instance.BuildIfNeeded();
                _instance.ConfigureAssets(fallbackFont, backgroundSprite);
                return _instance;
            }

            var rootGo = new GameObject("LoadingScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(LoadingScreenController));
            DontDestroyOnLoad(rootGo);
            _instance = rootGo.GetComponent<LoadingScreenController>();
            _instance.BuildIfNeeded();
            _instance.ConfigureAssets(fallbackFont, backgroundSprite);
            rootGo.SetActive(false);
            return _instance;
        }

        public static string ResolveMapDisplayName(string mapNameOrScene)
        {
            if (string.IsNullOrWhiteSpace(mapNameOrScene))
                return "SHIPMENT CARGO";

            string trimmed = mapNameOrScene.Trim();
            if (trimmed.Equals("Maintestgameplay", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("ARENA", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("ARENA 1", StringComparison.OrdinalIgnoreCase) ||
                trimmed.IndexOf("Shipment", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "SHIPMENT CARGO";
            }

            if (trimmed.Equals("Map2Test", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("ARENA 2", StringComparison.OrdinalIgnoreCase) ||
                trimmed.IndexOf("Sandoria", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "SANDORIA";
            }

            if (trimmed.IndexOf("Tutorial", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "TUTORIAL";
            }

            return trimmed.ToUpperInvariant();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            BuildIfNeeded();
        }

        public void ConfigureAssets(TMP_FontAsset fallbackFont, Sprite backgroundSprite)
        {
            BuildIfNeeded();

            if (_font == null && fallbackFont != null)
            {
                _font = fallbackFont;
            }

            if (backgroundSprite != null && _bgTextureImage != null)
            {
                _bgTextureImage.sprite = backgroundSprite;
                _bgTextureImage.color = BgSpriteTint;
                _bgTextureImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Displays the loading screen for the Tutorial scene and loads the scene asynchronously.
        /// Shows "MAP: TUTORIAL" and "STATUS: TUTORIAL MAP LOADING...".
        /// </summary>
        public void LoadTutorialScene(string sceneName, TMP_FontAsset fallbackFont = null, Sprite backgroundSprite = null)
        {
            ConfigureAssets(fallbackFont, backgroundSprite);

            if (_activeLoadCoroutine != null)
            {
                StopCoroutine(_activeLoadCoroutine);
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _activeLoadCoroutine = StartCoroutine(LoadStandardSceneRoutine(
                sceneName: sceneName,
                mapDisplayName: "TUTORIAL",
                subtitle: "TUTORIAL MAP LOADING...",
                statusText: "TUTORIAL MAP LOADING..."
            ));
        }

        /// <summary>
        /// Displays the loading screen for a Multiplayer Match (Host or Client) with the selected map name
        /// (e.g., "SHIPMENT CARGO" or "SANDORIA").
        /// </summary>
        public void LoadNetworkMatchScene(string selectedMapName, string targetSceneName, bool initiateServerLoad, TMP_FontAsset fallbackFont = null, Sprite backgroundSprite = null)
        {
            ConfigureAssets(fallbackFont, backgroundSprite);

            if (_activeLoadCoroutine != null)
            {
                StopCoroutine(_activeLoadCoroutine);
            }

            string resolvedMap = ResolveMapDisplayName(!string.IsNullOrEmpty(selectedMapName) ? selectedMapName : targetSceneName);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _activeLoadCoroutine = StartCoroutine(LoadNetworkSceneRoutine(
                targetSceneName: targetSceneName,
                mapDisplayName: resolvedMap,
                initiateServerLoad: initiateServerLoad
            ));
        }

        private IEnumerator LoadStandardSceneRoutine(string sceneName, string mapDisplayName, string subtitle, string statusText)
        {
            SetupLabels(mapDisplayName, subtitle, statusText);
            SetProgressVisuals(0f, "INITIALIZING MAP...");

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            yield return null;

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }

            const float minDisplayDuration = 1.25f;
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float timeRatio = Mathf.Clamp01(elapsed / minDisplayDuration);
                float opProgress = asyncOp != null ? Mathf.Clamp01(asyncOp.progress / 0.9f) : 1f;
                float displayProgress = Mathf.Min(timeRatio, opProgress) * 0.92f;

                string stage = displayProgress < 0.30f
                    ? "INITIALIZING MAP..."
                    : (displayProgress < 0.78f ? "LOADING RESOURCES..." : "PREPARING ENVIRONMENT...");
                SetProgressVisuals(displayProgress, stage);

                if (timeRatio >= 1f && opProgress >= 0.99f)
                {
                    break;
                }
                yield return null;
            }

            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
                while (!asyncOp.isDone)
                {
                    SetProgressVisuals(0.96f, "FINALIZING SCENE...");
                    yield return null;
                }
            }

            SetProgressVisuals(1f, "READY!");
            yield return new WaitForSecondsRealtime(0.18f);

            yield return FadeOutAndHide();
        }

        private IEnumerator LoadNetworkSceneRoutine(string targetSceneName, string mapDisplayName, bool initiateServerLoad)
        {
            SetupLabels(mapDisplayName, "PREPARING MATCH...", "SYNCHRONIZING PLAYERS...");
            SetProgressVisuals(0f, "INITIALIZING MATCH...");

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            // Wait one frame so the loading screen renders before Netcode starts scene loading
            yield return null;

            if (initiateServerLoad && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
            }

            const float minDisplayDuration = 1.35f;
            const float maxTimeout = 10f;
            float elapsed = 0f;

            while (elapsed < maxTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                float timeRatio = Mathf.Clamp01(elapsed / minDisplayDuration);
                bool sceneLoaded = SceneManager.GetActiveScene().name.Equals(targetSceneName, StringComparison.OrdinalIgnoreCase);

                float targetCap = sceneLoaded ? 1f : 0.88f;
                float displayProgress = Mathf.Min(timeRatio, targetCap);

                string stage = displayProgress < 0.30f
                    ? "INITIALIZING MATCH..."
                    : (displayProgress < 0.78f ? "LOADING RESOURCES..." : "SYNCHRONIZING ARENA...");
                SetProgressVisuals(displayProgress, stage);

                if (sceneLoaded && timeRatio >= 1f)
                {
                    break;
                }

                yield return null;
            }

            SetProgressVisuals(1f, "READY!");
            yield return new WaitForSecondsRealtime(0.18f);

            yield return FadeOutAndHide();
        }

        private IEnumerator FadeOutAndHide()
        {
            const float fadeDuration = 0.24f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f - t;
                }
                yield return null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            _activeLoadCoroutine = null;
            gameObject.SetActive(false);
        }

        private void SetupLabels(string mapDisplayName, string subtitle, string statusText)
        {
            if (_titleText != null)
            {
                _titleText.text = "LOADING GAME";
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = subtitle;
            }

            if (_mapValueText != null)
            {
                _mapValueText.text = $"<color=#1DC8EE>MAP:</color> {mapDisplayName}";
            }

            if (_statusValueText != null)
            {
                _statusValueText.text = $"<color=#1DC8EE>STATUS:</color> {statusText}";
            }

            if (_tipText != null)
            {
                _tipText.text = "<color=#1DC8EE>TIP:</color> CHOOSE YOUR WEAPONS IN THE LOADOUT MENU BEFORE THE MATCH STARTS.";
            }
        }

        private void SetProgressVisuals(float normalizedProgress, string stageLabel)
        {
            float clamped = Mathf.Clamp01(normalizedProgress);
            int pct = Mathf.RoundToInt(clamped * 100f);

            if (_percentText != null)
            {
                _percentText.text = $"{pct}%";
            }

            if (_stageText != null && !string.IsNullOrEmpty(stageLabel))
            {
                _stageText.text = stageLabel;
            }

            if (_progressFillRt != null)
            {
                float fillW = Mathf.Max(18f, BarFullWidth * clamped);
                _progressFillRt.sizeDelta = new Vector2(clamped <= 0.005f ? 0f : fillW, BarHeight);

                if (_progressLeadGlowRt != null)
                {
                    _progressLeadGlowRt.gameObject.SetActive(clamped > 0.02f);
                    _progressLeadGlowRt.anchoredPosition = new Vector2(fillW, 0f);
                }
            }
        }

        private void BuildIfNeeded()
        {
            if (_built) return;
            _built = true;

            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Oswald Bold SDF");

            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            // 1. Deep Dark Navy Base Background
            var baseBg = CreateImage(transform, "BaseBackground", JoinUISprites.White, BaseBgColor, Image.Type.Simple);
            baseBg.raycastTarget = true;
            StretchFull(baseBg.rectTransform);

            // Optional darkened menu background image for geometric depth matching the reference screenshot
            _bgTextureImage = CreateImage(transform, "MenuBackdropImage", JoinUISprites.White, BgSpriteTint, Image.Type.Simple);
            StretchFull(_bgTextureImage.rectTransform);
            _bgTextureImage.gameObject.SetActive(false);

            // 2. Emerald & Cyan Atmosphere Glows matching the reference mockup
            var leftEmeraldGlow = CreateImage(transform, "LeftEmeraldGlow", JoinUISprites.AtmosphereGlow, EmeraldGlowColor, Image.Type.Simple);
            SetRect(leftEmeraldGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-640f, -60f), new Vector2(1050f, 950f));

            var rightEmeraldGlow = CreateImage(transform, "RightEmeraldGlow", JoinUISprites.AtmosphereGlow, EmeraldGlowColor, Image.Type.Simple);
            SetRect(rightEmeraldGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(540f, 220f), new Vector2(980f, 920f));

            var centerCyanGlow = CreateImage(transform, "CenterCyanGlow", JoinUISprites.AtmosphereGlow, CyanGlowColor, Image.Type.Simple);
            SetRect(centerCyanGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(920f, 540f));

            // 3. Top Header: "LOADING GAME" + Flanking Cyan Laser Lines + "PREPARING MATCH..." Subtitle
            _titleText = CreateText(transform, "LoadingTitle", "LOADING GAME", 62f, 4.5f, Color.white);
            _titleText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            SetRect(_titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 340f), new Vector2(540f, 76f));

            var leftLaser = CreateImage(_titleText.transform, "LeftLaserLine", JoinUISprites.LaserLine, CyanLaserColor, Image.Type.Simple);
            SetRect(leftLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-315f, 0f), new Vector2(150f, 3f));
            leftLaser.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var rightLaser = CreateImage(_titleText.transform, "RightLaserLine", JoinUISprites.LaserLine, CyanLaserColor, Image.Type.Simple);
            SetRect(rightLaser.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(315f, 0f), new Vector2(150f, 3f));

            _subtitleText = CreateText(transform, "LoadingSubtitle", "PREPARING MATCH...", 21f, 4f, CyanAccentText);
            _subtitleText.fontStyle = FontStyles.Normal;
            SetRect(_subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(480f, 34f));

            // 4. Center Progress Section (Percentage + Stage Label + Chamfered Progress Bar)
            _percentText = CreateText(transform, "PercentageText", "0%", 22f, 2f, Color.white);
            _percentText.fontStyle = FontStyles.Bold;
            SetRect(_percentText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 78f), new Vector2(160f, 30f));

            _stageText = CreateText(transform, "StageText", "LOADING RESOURCES...", 16.5f, 3f, CyanAccentText);
            _stageText.fontStyle = FontStyles.Normal;
            SetRect(_stageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(440f, 28f));

            // Outer Chamfered Frame around the Progress Bar
            var outerBarFrame = CreateImage(transform, "ProgressOuterFrame", JoinUISprites.ChamferBoxFrame, TrackInnerBorderColor, Image.Type.Sliced);
            SetRect(outerBarFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(704f, 58f));

            // Main Chamfered Progress Track
            var barTrack = CreateImage(transform, "ProgressTrack", JoinUISprites.ChamferBoxFill, TrackBgColor, Image.Type.Sliced);
            SetRect(barTrack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(696f, 50f));

            var barTrackBorder = CreateImage(barTrack.transform, "ProgressTrackBorder", JoinUISprites.ChamferBoxFrame, TrackOuterBorderColor, Image.Type.Sliced);
            StretchFull(barTrackBorder.rectTransform);

            // Inner Fill Container (left-anchored so width scales smoothly from 0 to BarFullWidth)
            var fillContainerGo = new GameObject("ProgressFillContainer", typeof(RectTransform));
            fillContainerGo.layer = gameObject.layer;
            fillContainerGo.transform.SetParent(barTrack.transform, false);
            var fillContainerRt = fillContainerGo.GetComponent<RectTransform>();
            SetRect(fillContainerRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BarFullWidth, BarHeight));

            var fillImg = CreateImage(fillContainerRt, "ProgressFill", GetProgressFillSprite(), Color.white, Image.Type.Sliced);
            _progressFillRt = fillImg.rectTransform;
            _progressFillRt.anchorMin = new Vector2(0f, 0.5f);
            _progressFillRt.anchorMax = new Vector2(0f, 0.5f);
            _progressFillRt.pivot = new Vector2(0f, 0.5f);
            _progressFillRt.anchoredPosition = Vector2.zero;
            _progressFillRt.sizeDelta = new Vector2(0f, BarHeight);

            // Soft bright cyan bloom at the leading right edge of the fill bar
            var leadGlow = CreateImage(_progressFillRt, "LeadingEdgeGlow", JoinUISprites.AtmosphereGlow, new Color(0.45f, 0.98f, 1f, 0.75f), Image.Type.Simple);
            _progressLeadGlowRt = leadGlow.rectTransform;
            _progressLeadGlowRt.anchorMin = new Vector2(0f, 0.5f);
            _progressLeadGlowRt.anchorMax = new Vector2(0f, 0.5f);
            _progressLeadGlowRt.pivot = new Vector2(0.5f, 0.5f);
            _progressLeadGlowRt.anchoredPosition = Vector2.zero;
            _progressLeadGlowRt.sizeDelta = new Vector2(54f, 68f);

            // 5. Metadata Row Directly Below Progress Bar (Left: Pin Badge + MAP, Right: STATUS)
            var metaRowGo = new GameObject("MetadataRow", typeof(RectTransform));
            metaRowGo.layer = gameObject.layer;
            metaRowGo.transform.SetParent(transform, false);
            var metaRowRt = metaRowGo.GetComponent<RectTransform>();
            SetRect(metaRowRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(696f, 44f));

            // Chamfered Location Pin Icon Badge on the Left
            var pinBadgeFill = CreateImage(metaRowRt, "MapPinBadge", JoinUISprites.IconBadgeFill, BadgeBgColor, Image.Type.Sliced);
            SetRect(pinBadgeFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(21f, 0f), new Vector2(38f, 38f));

            var pinBadgeBorder = CreateImage(pinBadgeFill.transform, "BadgeBorder", JoinUISprites.IconBadgeFrame, BadgeBorderColor, Image.Type.Sliced);
            StretchFull(pinBadgeBorder.rectTransform);

            var pinIcon = CreateImage(pinBadgeFill.transform, "PinIcon", JoinUISprites.MapPinIcon, IconCyanColor, Image.Type.Simple);
            pinIcon.preserveAspect = true;
            SetRect(pinIcon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));

            // "MAP: SHIPMENT CARGO" / "MAP: TUTORIAL"
            _mapValueText = CreateText(metaRowRt, "MapMetadataText", "<color=#1DC8EE>MAP:</color> SHIPMENT CARGO", 17.5f, 2f, MetadataValueColor);
            _mapValueText.alignment = TextAlignmentOptions.MidlineLeft;
            _mapValueText.richText = true;
            _mapValueText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _mapValueText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            _mapValueText.rectTransform.pivot = new Vector2(0f, 0.5f);
            _mapValueText.rectTransform.anchoredPosition = new Vector2(52f, 0f);
            _mapValueText.rectTransform.sizeDelta = new Vector2(320f, 36f);

            // "STATUS: SYNCHRONIZING PLAYERS..."
            _statusValueText = CreateText(metaRowRt, "StatusMetadataText", "<color=#1DC8EE>STATUS:</color> SYNCHRONIZING PLAYERS...", 17.5f, 2f, MetadataValueColor);
            _statusValueText.alignment = TextAlignmentOptions.MidlineRight;
            _statusValueText.richText = true;
            _statusValueText.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            _statusValueText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _statusValueText.rectTransform.pivot = new Vector2(1f, 0.5f);
            _statusValueText.rectTransform.anchoredPosition = new Vector2(-4f, 0f);
            _statusValueText.rectTransform.sizeDelta = new Vector2(360f, 36f);

            // 6. Bottom Chamfered Tip Box
            var tipCard = CreateImage(transform, "TipBoxCard", JoinUISprites.CardFill, TipCardBgColor, Image.Type.Sliced);
            SetRect(tipCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -315f), new Vector2(1040f, 92f));

            var tipBorder = CreateImage(tipCard.transform, "TipBoxBorder", JoinUISprites.CardFrame, TipCardBorderColor, Image.Type.Sliced);
            StretchFull(tipBorder.rectTransform);

            _tipText = CreateText(tipCard.transform, "TipText",
                "<color=#1DC8EE>TIP:</color> CHOOSE YOUR WEAPONS IN THE LOADOUT MENU BEFORE THE MATCH STARTS.",
                18f, 2.2f, MetadataValueColor);
            _tipText.richText = true;
            StretchFull(_tipText.rectTransform);
        }

        private static Sprite GetProgressFillSprite()
        {
            if (_progressFillSprite != null) return _progressFillSprite;

            const int w = 256;
            const int h = 48;
            const float chamfer = 9f;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "LoadingScreen_ProgressFill",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color leftCol = new Color(0.05f, 0.52f, 0.72f, 1f);
            Color midCol = new Color(0.10f, 0.88f, 0.68f, 1f);
            Color rightCol = new Color(0.42f, 0.96f, 1.00f, 1f);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    Color grad = u < 0.58f
                        ? Color.Lerp(leftCol, midCol, u / 0.58f)
                        : Color.Lerp(midCol, rightCol, (u - 0.58f) / 0.42f);

                    // Subtle vertical highlight toward center
                    float v = Mathf.Abs((y / (float)(h - 1)) - 0.5f) * 2f;
                    grad = Color.Lerp(grad, Color.white, (1f - v * v) * 0.12f);

                    // Anti-aliased 4-corner chamfer mask
                    float cov = 0f;
                    const int ss = 4;
                    const float inv = 1f / ss;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float sxPos = x + (sx + 0.5f) * inv;
                            float syPos = y + (sy + 0.5f) * inv;
                            if (ChamferRect(sxPos, syPos, w, h, chamfer))
                                cov += 1f;
                        }
                    }
                    cov /= (ss * ss);

                    grad.a = cov;
                    px[y * w + x] = grad;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            _progressFillSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
            _progressFillSprite.hideFlags = HideFlags.DontSave;
            return _progressFillSprite;
        }

        private static bool ChamferRect(float x, float y, float w, float h, float chamfer)
        {
            if (x < 0f || y < 0f || x > w || y > h) return false;
            if (x + y < chamfer) return false;
            if (x + (h - y) < chamfer) return false;
            if ((w - x) + y < chamfer) return false;
            if ((w - x) + (h - y) < chamfer) return false;
            return true;
        }

        #region UI Helpers
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
}
