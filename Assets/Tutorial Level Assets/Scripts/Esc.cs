using System.Collections;
using ParkourFPS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PewPewArena.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Tutorial-scene pause menu controller.
/// Styled to match the AAA tactical Cyber Gold & Obsidian sci-fi standard across Pew Pew Arena:
/// - Full-screen tactical dimmer with ethereal edge atmosphere glows.
/// - Centered command card container with chamfered wireframe frame and cyan laser accents.
/// - Crisp typography: category sub-header, hero "PAUSED" title, and cyan laser divider.
/// - Signature buttons with rounded steel-cyan outline (ButtonBorder), dark tactical fill,
///   hover/click scaling, and dual flanking hover arrows (MainMenuButtonEffect).
/// - Unscaled time execution so all hover/click animations operate smoothly when paused.
/// </summary>
[ExecuteAlways]
public class Esc : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";

    [Header("UI References")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;
    private PlayerControllerScript playerController;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (pauseCanvas == null)
        {
            BuildPauseUI();
        }
        else
        {
            ApplyPauseMenuTheme();
        }

        if (!Application.isPlaying) return;

        playerController = FindAnyObjectByType<PlayerControllerScript>();

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        ApplyPauseMenuTheme();

        // Non-persistent listeners don't survive domain reloads, so re-wire them every enable.
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitToMainMenu);
            quitButton.onClick.AddListener(QuitToMainMenu);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (!TutorialBootSequence.IsRunning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pauseCanvas != null)
        {
            ApplyPauseMenuTheme();
            pauseCanvas.SetActive(true);

            // Ensure button arrows instantly reset to idle state when opening the pause menu
            var effects = pauseCanvas.GetComponentsInChildren<MainMenuButtonEffect>(true);
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null) effects[i].ApplyInstantIdleState();
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            if (gameObject.activeInHierarchy)
            {
                _fadeRoutine = StartCoroutine(FadePauseMenuRoutine(true));
            }
        }

        if (playerController != null) playerController.InputEnabled = false;
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (playerController != null) playerController.InputEnabled = true;
    }

    private void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator FadePauseMenuRoutine(bool show)
    {
        if (pauseCanvas == null) yield break;

        var cg = pauseCanvas.GetComponent<CanvasGroup>();
        if (cg == null) cg = pauseCanvas.AddComponent<CanvasGroup>();

        var bg = pauseCanvas.transform.Find("Background");
        var cardBg = bg != null ? bg.Find("PauseCardBg") as RectTransform : null;
        var laserDivider = bg != null ? bg.Find("PauseLaserDivider") as RectTransform : null;

        float duration = show ? 0.20f : 0.12f;
        float elapsed = 0f;
        float startAlpha = cg.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector3 cardStartScale = show ? (Vector3.one * 0.95f) : Vector3.one;
        Vector3 cardTargetScale = show ? Vector3.one : (Vector3.one * 0.95f);

        if (show)
        {
            cg.alpha = 0f;
            if (cardBg != null) cardBg.localScale = cardStartScale;
            if (laserDivider != null) laserDivider.localScale = new Vector3(0f, 1f, 1f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.SmoothStep(0f, 1f, progress);

            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            if (cardBg != null)
                cardBg.localScale = Vector3.LerpUnclamped(cardStartScale, cardTargetScale, ease);

            if (show && laserDivider != null)
            {
                laserDivider.localScale = new Vector3(ease, 1f, 1f);
            }

            yield return null;
        }

        cg.alpha = targetAlpha;
        if (cardBg != null) cardBg.localScale = cardTargetScale;
        if (laserDivider != null) laserDivider.localScale = Vector3.one;

        cg.interactable = show;
        cg.blocksRaycasts = show;
        _fadeRoutine = null;
    }

    [ContextMenu("Apply Pause Menu Theme")]
    public void ApplyPauseMenuTheme()
    {
        if (pauseCanvas == null) return;

        // 1. Canvas & Scaler configuration
        var canvas = pauseCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        var scaler = pauseCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var cg = pauseCanvas.GetComponent<CanvasGroup>();
        if (cg == null) cg = pauseCanvas.AddComponent<CanvasGroup>();

        // 2. Full-screen Tactical Dimmer Background
        var bgImg = GetOrCreateImage(pauseCanvas.transform, "Background", Vector2.zero, Vector2.zero);
        StretchFull(bgImg.rectTransform);
        bgImg.sprite = null;
        bgImg.color = new Color(0.024f, 0.035f, 0.059f, 0.78f);
        bgImg.raycastTarget = true;
        var bgTransform = bgImg.transform;

        // 3. Ethereal Edge Atmosphere Glows
        var leftAtmo = GetOrCreateImage(bgTransform, "LeftAtmosphere", new Vector2(200f, 0f), new Vector2(500f, 850f));
        leftAtmo.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        leftAtmo.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        leftAtmo.sprite = JoinUISprites.AtmosphereGlow;
        leftAtmo.color = new Color(0.02f, 0.70f, 0.45f, 0.16f);
        leftAtmo.raycastTarget = false;
        leftAtmo.type = Image.Type.Simple;
        leftAtmo.transform.SetSiblingIndex(0);

        var rightAtmo = GetOrCreateImage(bgTransform, "RightAtmosphere", new Vector2(-200f, 0f), new Vector2(500f, 850f));
        rightAtmo.rectTransform.anchorMin = new Vector2(1f, 0.5f);
        rightAtmo.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        rightAtmo.sprite = JoinUISprites.AtmosphereGlow;
        rightAtmo.color = new Color(0.02f, 0.70f, 0.45f, 0.16f);
        rightAtmo.raycastTarget = false;
        rightAtmo.type = Image.Type.Simple;
        rightAtmo.transform.SetSiblingIndex(1);

        // 4. Centered Command Card Container (PauseCardBg 460x480)
        var cardBgImg = GetOrCreateImage(bgTransform, "PauseCardBg", Vector2.zero, new Vector2(460f, 480f));
        cardBgImg.sprite = JoinUISprites.CardFill;
        cardBgImg.type = Image.Type.Sliced;
        cardBgImg.color = new Color(0.043f, 0.059f, 0.090f, 0.95f);
        cardBgImg.raycastTarget = true;
        cardBgImg.transform.SetSiblingIndex(2);

        // Card wireframe border
        var cardBorderImg = GetOrCreateImage(cardBgImg.transform, "CardBorder", Vector2.zero, Vector2.zero);
        StretchFull(cardBorderImg.rectTransform);
        cardBorderImg.sprite = JoinUISprites.CardFrame;
        cardBorderImg.type = Image.Type.Sliced;
        cardBorderImg.color = new Color(0.149f, 0.231f, 0.329f, 0.85f);
        cardBorderImg.raycastTarget = false;
        cardBorderImg.transform.SetSiblingIndex(0);

        // Top laser accent
        var topAccent = GetOrCreateImage(cardBgImg.transform, "TopLaserAccent", new Vector2(0f, 238.5f), new Vector2(460f, 3f));
        topAccent.sprite = null;
        topAccent.color = new Color(0f, 0.898f, 1f, 0.90f);
        topAccent.raycastTarget = false;

        // Bottom laser accent
        var bottomAccent = GetOrCreateImage(cardBgImg.transform, "BottomLaserAccent", new Vector2(0f, -239f), new Vector2(160f, 2f));
        bottomAccent.sprite = null;
        bottomAccent.color = new Color(0f, 0.898f, 1f, 0.45f);
        bottomAccent.raycastTarget = false;

        // 5. Header Typography
        // Grab primary font from Title or Oswald Bold SDF
        var existingTitleTmp = bgTransform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset fontAsset = existingTitleTmp != null ? existingTitleTmp.font : null;

        // Subtitle ("goodboy lil bro")
        var subtitleTmp = GetOrCreateText(bgTransform, "PauseSubtitle", new Vector2(0f, 175f), new Vector2(380f, 24f));
        subtitleTmp.text = "goodboy lil bro";
        subtitleTmp.color = new Color(0f, 0.898f, 1f, 0.85f);
        subtitleTmp.fontSize = 14f;
        subtitleTmp.characterSpacing = 4f;
        subtitleTmp.fontStyle = FontStyles.Bold;
        subtitleTmp.alignment = TextAlignmentOptions.Center;
        subtitleTmp.raycastTarget = false;
        if (fontAsset != null) subtitleTmp.font = fontAsset;

        // Hero Title ("PAUSED")
        var titleTmp = GetOrCreateText(bgTransform, "Title", new Vector2(0f, 130f), new Vector2(400f, 54f));
        titleTmp.text = "PAUSED";
        titleTmp.color = Color.white;
        titleTmp.fontSize = 52f;
        titleTmp.characterSpacing = 5f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;
        if (fontAsset != null) titleTmp.font = fontAsset;

        // Laser Divider (320x2)
        var laserDividerImg = GetOrCreateImage(bgTransform, "PauseLaserDivider", new Vector2(0f, 85f), new Vector2(320f, 2f));
        laserDividerImg.sprite = null;
        laserDividerImg.color = new Color(0f, 0.898f, 1f, 0.55f);
        laserDividerImg.raycastTarget = false;

        // 6. Styled Buttons with Outline and Flanking Hover Arrows
        if (resumeButton == null)
        {
            var rTr = bgTransform.Find("ResumeButton");
            if (rTr != null) resumeButton = rTr.GetComponent<Button>();
        }
        if (quitButton == null)
        {
            var qTr = bgTransform.Find("QuitButton");
            if (qTr != null) quitButton = qTr.GetComponent<Button>();
        }

        Color textIdleSilver = new Color(0.85f, 0.90f, 0.93f, 1f);
        Color textHoverWhite = Color.white;
        Color textClickCyan = new Color(0.70f, 1.0f, 0.90f, 1f);

        StyleMenuButton(resumeButton, new Vector2(0f, 18f), "RESUME", textIdleSilver, textHoverWhite, textClickCyan, fontAsset);
        StyleMenuButton(quitButton, new Vector2(0f, -48f), "QUIT TO MAIN MENU", textIdleSilver, textHoverWhite, textClickCyan, fontAsset);

        // 7. Tactical Footer Prompt
        var footerHintTmp = GetOrCreateText(bgTransform, "PauseFooterHint", new Vector2(0f, -170f), new Vector2(360f, 28f));
        footerHintTmp.text = "[ ESC ]  RETURN TO GAME";
        footerHintTmp.color = new Color(0.424f, 0.510f, 0.600f, 0.85f);
        footerHintTmp.fontSize = 13f;
        footerHintTmp.characterSpacing = 3f;
        footerHintTmp.fontStyle = FontStyles.Bold;
        footerHintTmp.alignment = TextAlignmentOptions.Center;
        footerHintTmp.raycastTarget = false;
        if (fontAsset != null) footerHintTmp.font = fontAsset;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void StyleMenuButton(Button btn, Vector2 pos, string label, Color textNormal, Color textHover, Color textClick, TMP_FontAsset font)
    {
        if (btn == null) return;

        var rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = label.Length > 12 ? new Vector2(320f, 52f) : new Vector2(300f, 52f);
            rt.localScale = Vector3.one;
        }

        btn.transition = Selectable.Transition.None;

        // Button background fill
        var btnImg = btn.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.sprite = JoinUISprites.ButtonFill;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.035f, 0.07f, 0.11f, 0.70f);
            btnImg.raycastTarget = true;
        }

        // Button outline border (ButtonBorder)
        var borderTr = btn.transform.Find("ButtonBorder");
        Image borderImg;
        if (borderTr == null)
        {
            var borderGo = new GameObject("ButtonBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderGo.layer = btn.gameObject.layer;
            borderGo.transform.SetParent(btn.transform, false);
            borderImg = borderGo.GetComponent<Image>();
        }
        else
        {
            borderImg = borderTr.GetComponent<Image>();
            if (borderImg == null) borderImg = borderTr.gameObject.AddComponent<Image>();
        }

        var bRt = borderImg.rectTransform;
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = Vector2.zero;
        bRt.offsetMax = Vector2.zero;
        borderImg.sprite = JoinUISprites.ButtonFrame;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = new Color(0.25f, 0.42f, 0.52f, 0.60f);
        borderImg.raycastTarget = false;
        borderImg.transform.SetSiblingIndex(0);

        // Explicit Left Arrow (HoverArrow)
        Transform leftArrowTr = btn.transform.Find("HoverArrow");
        if (leftArrowTr == null)
        {
            leftArrowTr = btn.transform.Find("Arrow") ?? btn.transform.Find("Arrow (2)");
            if (leftArrowTr != null)
            {
                leftArrowTr.name = "HoverArrow";
            }
            else
            {
                var go = new GameObject("HoverArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                go.layer = btn.gameObject.layer;
                go.transform.SetParent(btn.transform, false);
                leftArrowTr = go.transform;
            }
        }
        leftArrowTr.gameObject.SetActive(true);
        var leftRt = leftArrowTr.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0.5f, 0.5f);
        leftRt.anchorMax = new Vector2(0.5f, 0.5f);
        leftRt.pivot = new Vector2(0.5f, 0.5f);
        leftRt.sizeDelta = new Vector2(40f, 34f);
        leftRt.localScale = Vector3.one;

        var leftImg = leftArrowTr.GetComponent<Image>();
        if (leftImg == null) leftImg = leftArrowTr.gameObject.AddComponent<Image>();
        leftImg.sprite = MainMenuButtonEffect.GetDefaultLeftArrow();
        leftImg.color = new Color(0.92f, 0.98f, 1f, 1f);
        leftImg.raycastTarget = false;
        leftImg.preserveAspect = true;

        var leftCg = leftArrowTr.GetComponent<CanvasGroup>();
        if (leftCg == null) leftCg = leftArrowTr.gameObject.AddComponent<CanvasGroup>();
        leftCg.alpha = 0f;
        leftCg.interactable = false;
        leftCg.blocksRaycasts = false;

        // Explicit Right Arrow (HoverArrowRight)
        Transform rightArrowTr = btn.transform.Find("HoverArrowRight");
        if (rightArrowTr == null)
        {
            rightArrowTr = btn.transform.Find("Arrow (1)") ?? btn.transform.Find("Arrow (3)") ?? btn.transform.Find("RightHoverArrow");
            if (rightArrowTr != null)
            {
                rightArrowTr.name = "HoverArrowRight";
            }
            else
            {
                var go = new GameObject("HoverArrowRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                go.layer = btn.gameObject.layer;
                go.transform.SetParent(btn.transform, false);
                rightArrowTr = go.transform;
            }
        }
        rightArrowTr.gameObject.SetActive(true);
        var rightRt = rightArrowTr.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.5f, 0.5f);
        rightRt.anchorMax = new Vector2(0.5f, 0.5f);
        rightRt.pivot = new Vector2(0.5f, 0.5f);
        rightRt.sizeDelta = new Vector2(40f, 34f);
        rightRt.localScale = Vector3.one;

        var rightImg = rightArrowTr.GetComponent<Image>();
        if (rightImg == null) rightImg = rightArrowTr.gameObject.AddComponent<Image>();
        rightImg.sprite = MainMenuButtonEffect.GetDefaultRightArrow();
        rightImg.color = new Color(0.92f, 0.98f, 1f, 1f);
        rightImg.raycastTarget = false;
        rightImg.preserveAspect = true;

        var rightCg = rightArrowTr.GetComponent<CanvasGroup>();
        if (rightCg == null) rightCg = rightArrowTr.gameObject.AddComponent<CanvasGroup>();
        rightCg.alpha = 0f;
        rightCg.interactable = false;
        rightCg.blocksRaycasts = false;

        // Clean up any remaining legacy static Arrow children
        for (int i = btn.transform.childCount - 1; i >= 0; i--)
        {
            var child = btn.transform.GetChild(i);
            if (child != leftArrowTr && child != rightArrowTr && child.name.StartsWith("Arrow"))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        // Button Text
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = label;
            tmp.color = textNormal;
            tmp.fontSize = label.Length > 12 ? 18f : 24f;
            tmp.characterSpacing = 3f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;
        }

        // Attach and configure MainMenuButtonEffect
        var effect = btn.GetComponent<MainMenuButtonEffect>();
        if (effect == null) effect = btn.gameObject.AddComponent<MainMenuButtonEffect>();

        effect.ArrowSprite = MainMenuButtonEffect.GetDefaultLeftArrow();
        effect.RightArrowSprite = MainMenuButtonEffect.GetDefaultRightArrow();
        effect.ConfigureArrows(new Vector2(40f, 34f), 12f, 6f);
        effect.ConfigureColors(
            new Color(0.035f, 0.07f, 0.11f, 0.70f),
            new Color(0.05f, 0.10f, 0.16f, 0.85f),
            new Color(0.25f, 0.42f, 0.52f, 0.60f),
            new Color(0.38f, 0.65f, 0.80f, 0.90f),
            textNormal,
            textHover,
            textClick,
            new Color(0.92f, 0.98f, 1f, 1f)
        );

        effect.InitializeComponents();
        effect.FindRightArrow();
        effect.CalculateArrowPositions();
        effect.ApplyInstantIdleState();
    }

    private void BuildPauseUI()
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        pauseCanvas = new GameObject("PauseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        pauseCanvas.transform.SetParent(transform, false);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(pauseCanvas.transform, false);

        var rGo = new GameObject("ResumeButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        rGo.transform.SetParent(bg.transform, false);
        resumeButton = rGo.GetComponent<Button>();
        var rLabel = new GameObject("ResumeButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        rLabel.transform.SetParent(rGo.transform, false);
        StretchFull(rLabel.GetComponent<RectTransform>());

        var qGo = new GameObject("QuitButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        qGo.transform.SetParent(bg.transform, false);
        quitButton = qGo.GetComponent<Button>();
        var qLabel = new GameObject("QuitButtonLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        qLabel.transform.SetParent(qGo.transform, false);
        StretchFull(qLabel.GetComponent<RectTransform>());

        ApplyPauseMenuTheme();
        pauseCanvas.SetActive(false);

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private static Image GetOrCreateImage(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var tr = parent.Find(name);
        Image img;
        if (tr == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            img = go.GetComponent<Image>();
        }
        else
        {
            tr.gameObject.SetActive(true);
            img = tr.GetComponent<Image>();
            if (img == null) img = tr.gameObject.AddComponent<Image>();
        }

        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        return img;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var tr = parent.Find(name);
        TextMeshProUGUI tmp;
        if (tr == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            tmp = go.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            tr.gameObject.SetActive(true);
            tmp = tr.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = tr.gameObject.AddComponent<TextMeshProUGUI>();
        }

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        return tmp;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
