using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ParkourFPS;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class TutorialBootSequence : MonoBehaviour
{
    [Header("Boot Timing")]
    [SerializeField, Min(1f)] private float terminalDuration = 24f;
    [SerializeField, Min(0.03f)] private float codeLineInterval = 0.6f;
    [SerializeField, Min(0f)] private float distortionDuration = 4.5f;
    [SerializeField, Min(0f)] private float eraseDuration = 2.2f;
    [SerializeField, Min(0f)] private float blackHoldDuration = 4f;
    [SerializeField, Min(0f)] private float overlayFadeDuration = 5f;
    [SerializeField, Min(0f)] private float focusRecoveryDuration = 5f;
    [SerializeField, Range(0.5f, 1.5f)] private float initialBlurRadius = 1.5f;

    [Header("Startup Audio")]
    [SerializeField, Range(0f, 1f)] private float startupVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
    [SerializeField, Min(0f)] private float musicFadeInDuration = 3f;

    [Header("Scene UI")]
    [SerializeField] private GameObject bootCanvas;
    [SerializeField] private CanvasGroup bootCanvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private Text terminalText;
    [SerializeField, Min(0f)] private float codeVisionHoldDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float codeVisionOpacity = 0.85f;
    [SerializeField, Min(0.1f)] private float codeRainSpeed = 90f;
    [SerializeField, Range(8, 64)] private int revealColumns = 24;
    [SerializeField, Range(5, 36)] private int revealRows = 14;
    [SerializeField, Min(0.1f)] private float pixelRevealDuration = 4.5f;
    [SerializeField, Range(0f, 1f)] private float worldFractureStrength = 0.65f;
    [SerializeField, Min(0f)] private float matrixBloomThreshold = 0.35f;
    [SerializeField, Min(0f)] private float matrixBloomIntensity = 2.6f;
    [SerializeField, Range(0f, 1f)] private float matrixBloomScatter = 0.88f;

    private static bool isRunning;
    public static bool IsRunning => isRunning;

    // Fired once control is handed back to the player (the "wake up" moment).
    public static event Action OnPlayerWokeUp;

    private readonly List<string> recentLines = new List<string>();
    private PlayerControllerScript playerController;
    private Volume tutorialVolume;
    private VolumeProfile runtimeProfile;
    private DepthOfField depthOfField;
    private bool addedDepthOfField;
    private bool previousInputEnabled;
    private bool previousDepthOfFieldActive;
    private bool previousModeOverride;
    private DepthOfFieldMode previousDepthOfFieldMode;
    private bool previousStartOverride;
    private float previousGaussianStart;
    private bool previousEndOverride;
    private float previousGaussianEnd;
    private bool previousRadiusOverride;
    private float previousGaussianRadius;
    private float previousTimeScale;
    private Bloom bloom;
    private bool addedBloom;
    private bool previousBloomActive;
    private bool previousBloomThresholdOverride;
    private float previousBloomThreshold;
    private bool previousBloomIntensityOverride;
    private float previousBloomIntensity;
    private bool previousBloomScatterOverride;
    private float previousBloomScatter;
    private bool sequenceStarted;
    private bool worldVisionUsed;
    private AudioSource ambienceSource;
    private AudioSource cueSource;
    private AudioSource musicSource;
    private AudioClip powerUpClip;
    private AudioClip bootHumClip;
    private AudioClip terminalTickClip;
    private AudioClip keyConfirmClip;
    private AudioClip glitchClip;
    private AudioClip eraseSweepClip;
    private AudioClip revealChimeClip;
    private AudioClip diagnosticLockClip;
    private AudioClip tutorialMusicClip;
    private readonly List<CodeColumn> codeColumns = new List<CodeColumn>();
    private readonly List<RevealCell> revealCells = new List<RevealCell>();
    private GameObject codeVisionRoot;
    private CanvasGroup codeVisionGroup;
    private Image codeVeil;
    private Camera fractureCamera;
    private Vector3 previousCameraLocalPosition;
    private Quaternion previousCameraLocalRotation;
    private bool cameraTransformCaptured;

    private static readonly string[] DiagnosticLines =
    {
        "> KERNEL HANDSHAKE ................. ACCEPTED",
        "> MOTOR CORTEX / CALIBRATION ....... NOMINAL",
        "> OPTICAL FEED / FRAME LOCK ........ ACQUIRING",
        "> SIMULATION INTEGRITY ............. 99.8%",
        "> OPERATOR ID ....................... UNRESOLVED",
        "> MEMORY PARTITION / DELTA .......... MOUNTED",
        "> PHYSICS LAYER / CONSISTENCY ....... PASS",
        "> EXTERNAL SIGNAL ................... NOT FOUND",
        "> WORLD STATE / AUTHORITY ........... CONTESTED",
        "> WAKE PROTOCOL / OVERRIDE .......... PENDING",
        "> PRIME DIRECTIVE / PROTECT ........ ACTIVE",
        "> REALITY CHECK / DO NOT TRUST ...... FLAGGED"
    };

    private sealed class CodeColumn
    {
        public RectTransform rect;
        public Text text;
        public float speed;
        public float resetY;
        public float baseAlpha;
    }

    private sealed class RevealCell
    {
        public Image image;
        public float order;
        public float phase;
    }

    private void Awake()
    {
        if (bootCanvas == null) BuildBootUI();
        EnsureCodeVisionOverlay();
        if (!Application.isPlaying) return;

        BeginSequence();
    }

    private void BeginSequence()
    {
        sequenceStarted = true;
        isRunning = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        playerController = FindFirstObjectByType<PlayerControllerScript>();
        if (playerController != null)
        {
            previousInputEnabled = playerController.InputEnabled;
            playerController.InputEnabled = false;
            tutorialVolume = playerController.GetComponent<Volume>();
        }

        if (tutorialVolume == null) tutorialVolume = FindFirstObjectByType<Volume>();
        PrepareDepthOfField();
        CaptureFractureCamera();
        PrepareAudio();
        if (bootCanvas != null) bootCanvas.SetActive(true);
        StartCoroutine(PlaySequence());
    }

    private void PrepareAudio()
    {
        powerUpClip = Resources.Load<AudioClip>("Startup/power_up");
        bootHumClip = Resources.Load<AudioClip>("Startup/boot_hum");
        terminalTickClip = Resources.Load<AudioClip>("Startup/terminal_tick");
        keyConfirmClip = Resources.Load<AudioClip>("Startup/key_confirm");
        glitchClip = Resources.Load<AudioClip>("Startup/glitch");
        eraseSweepClip = Resources.Load<AudioClip>("Startup/erase_sweep");
        revealChimeClip = Resources.Load<AudioClip>("Startup/reveal_chime");
        diagnosticLockClip = Resources.Load<AudioClip>("Startup/diagnostic_lock");
        tutorialMusicClip = Resources.Load<AudioClip>("Music/TutorialLevelMusic");

        ambienceSource = CreateAudioSource("Boot Ambience");
        cueSource = CreateAudioSource("Boot Cues");
        musicSource = CreateAudioSource("Tutorial Music");
        PlayCue(powerUpClip, 0.75f);

        if (bootHumClip != null)
        {
            ambienceSource.clip = bootHumClip;
            ambienceSource.loop = true;
            ambienceSource.volume = startupVolume * 0.2f;
            ambienceSource.Play();
        }
    }

    private AudioSource CreateAudioSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.ignoreListenerPause = true;
        source.volume = 1f;
        return source;
    }

    private void PlayCue(AudioClip clip, float relativeVolume = 1f)
    {
        if (clip != null && cueSource != null)
        {
            cueSource.PlayOneShot(clip, startupVolume * relativeVolume);
        }
    }

    private void PrepareDepthOfField()
    {
        if (tutorialVolume == null) return;

        runtimeProfile = tutorialVolume.profile;
        if (runtimeProfile == null) return;

        if (!runtimeProfile.TryGet(out depthOfField))
        {
            depthOfField = runtimeProfile.Add<DepthOfField>(true);
            addedDepthOfField = true;
        }

        previousDepthOfFieldActive = depthOfField.active;
        previousDepthOfFieldMode = depthOfField.mode.value;
        previousModeOverride = depthOfField.mode.overrideState;
        previousGaussianStart = depthOfField.gaussianStart.value;
        previousStartOverride = depthOfField.gaussianStart.overrideState;
        previousGaussianEnd = depthOfField.gaussianEnd.value;
        previousEndOverride = depthOfField.gaussianEnd.overrideState;
        previousGaussianRadius = depthOfField.gaussianMaxRadius.value;
        previousRadiusOverride = depthOfField.gaussianMaxRadius.overrideState;

        depthOfField.active = true;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Gaussian;
        depthOfField.gaussianStart.overrideState = true;
        depthOfField.gaussianStart.value = 0.1f;
        depthOfField.gaussianEnd.overrideState = true;
        depthOfField.gaussianEnd.value = 0.2f;
        depthOfField.gaussianMaxRadius.overrideState = true;
        depthOfField.gaussianMaxRadius.value = initialBlurRadius;
    }

    private void BuildBootUI()
    {
        bootCanvas = new GameObject("Tutorial Boot Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        bootCanvas.transform.SetParent(transform, false);

        Canvas canvas = bootCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = bootCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        bootCanvasGroup = bootCanvas.GetComponent<CanvasGroup>();
        background = CreateImage("Black Background", bootCanvas.transform, Color.black);
        StretchFull(background.rectTransform);
        background.raycastTarget = false;

        terminalText = CreateText("Terminal Output", bootCanvas.transform);
        terminalText.rectTransform.anchorMin = new Vector2(0f, 1f);
        terminalText.rectTransform.anchorMax = new Vector2(0f, 1f);
        terminalText.rectTransform.pivot = new Vector2(0f, 1f);
        terminalText.rectTransform.anchoredPosition = new Vector2(90f, -72f);
        terminalText.rectTransform.sizeDelta = new Vector2(1740f, 940f);
        terminalText.fontSize = 25;
        terminalText.fontStyle = FontStyle.Bold;
        terminalText.alignment = TextAnchor.UpperLeft;
        terminalText.horizontalOverflow = HorizontalWrapMode.Wrap;
        terminalText.verticalOverflow = VerticalWrapMode.Overflow;
        terminalText.color = new Color(0.15f, 1f, 0.28f);
        terminalText.raycastTarget = false;

        bootCanvas.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private IEnumerator PlaySequence()
    {
        float elapsed = 0f;
        float nextLineAt = 0f;
        float nextTickAt = 0f;
        int lineIndex = 0;

        while (elapsed < terminalDuration)
        {
            if (elapsed >= nextLineAt)
            {
                recentLines.Add(CreateDiagnosticLine(lineIndex++));
                if (recentLines.Count > 18) recentLines.RemoveAt(0);
                terminalText.text = BuildTerminalFrame(elapsed / terminalDuration);
                if (elapsed >= nextTickAt)
                {
                    PlayCue(terminalTickClip, 0.24f);
                    nextTickAt = elapsed + 0.42f;
                }
                if (lineIndex % 5 == 0) PlayCue(diagnosticLockClip, 0.38f);
                nextLineAt += codeLineInterval;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        string readyMessage = BuildTerminalFrame(1f) + "\n\n> BOOT COMPLETE\n> TUTORIAL SYSTEM ONLINE\n> WELCOME, OPERATOR_\n\n> PRESS ANY KEY TO INITIATE TUTORIAL SEQUENCE";
        terminalText.text = readyMessage;
        while (Keyboard.current == null || !Keyboard.current.anyKey.wasPressedThisFrame)
        {
            terminalText.text = readyMessage + (Time.unscaledTime % 1f < 0.5f ? "_" : " ");
            yield return null;
        }

        PlayCue(keyConfirmClip, 0.8f);
        if (ambienceSource != null) ambienceSource.Stop();
        yield return FractureReality();
        PlayCue(eraseSweepClip, 0.65f);
        yield return EraseTerminalText();
        terminalText.text = string.Empty;
        if (blackHoldDuration > 0f) yield return new WaitForSecondsRealtime(blackHoldDuration);
        yield return ShowCodeVision();
        MatrixCodeVisionFeature.SetVision(false, 1f, codeRainSpeed / 90f);
        RestoreMatrixBloom();
        if (codeVisionRoot != null) codeVisionRoot.SetActive(false);

        PlayCue(revealChimeClip, 0.7f);
        HandControlBackToPlayer();
        elapsed = 0f;
        float revealDuration = Mathf.Max(overlayFadeDuration, focusRecoveryDuration);
        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float overlayProgress = overlayFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / overlayFadeDuration);
            bootCanvasGroup.alpha = worldVisionUsed ? 0f : 1f - overlayProgress;

            if (depthOfField != null)
            {
                float focusProgress = focusRecoveryDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / focusRecoveryDuration);
                depthOfField.gaussianStart.value = Mathf.Lerp(0.1f, previousGaussianStart, focusProgress);
                depthOfField.gaussianEnd.value = Mathf.Lerp(0.2f, previousGaussianEnd, focusProgress);
                depthOfField.gaussianMaxRadius.value = Mathf.Lerp(initialBlurRadius, previousGaussianRadius, focusProgress);
            }

            yield return null;
        }

        bootCanvas.SetActive(false);
        FinishSequence();
    }

    private IEnumerator FractureReality()
    {
        const string replacementCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#$%&@";
        string stableText = terminalText.text;
        System.Random random = new System.Random();
        float elapsed = 0f;
        float nextUpdateAt = 0f;
        float nextGlitchAt = 0f;

        while (elapsed < distortionDuration)
        {
            if (elapsed >= nextUpdateAt)
            {
                terminalText.text = BuildFracturedText(stableText, replacementCharacters, random, elapsed / distortionDuration);
                if (elapsed >= nextGlitchAt)
                {
                    PlayCue(glitchClip, 0.22f);
                    nextGlitchAt = elapsed + 0.28f;
                }
                nextUpdateAt += 0.08f;
            }

            ApplyCameraFracture(elapsed, distortionDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator EraseTerminalText()
    {
        string scrambledText = terminalText.text;
        int textLength = scrambledText.Length;
        float elapsed = 0f;

        while (elapsed < eraseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = eraseDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / eraseDuration);
            int visibleCharacters = Mathf.RoundToInt(textLength * (1f - progress));
            terminalText.text = BuildTearingText(scrambledText, visibleCharacters, progress);
            ApplyCameraFracture(distortionDuration + elapsed, distortionDuration + eraseDuration);
            yield return null;
        }
    }

    private IEnumerator ShowCodeVision()
    {
        if (MatrixCodeVisionFeature.SetVision(true, 0f, codeRainSpeed / 90f))
        {
            worldVisionUsed = true;
            PrepareMatrixBloom();
            bootCanvasGroup.alpha = 0f;
            codeVisionRoot.SetActive(false);
            float elapsed = 0f;
            while (elapsed < codeVisionHoldDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return RevealMatrixWorld();
            yield break;
        }

        Debug.LogError("MatrixCodeVisionFeature is missing from the active URP renderer. Using the UI fallback.", this);
        EnsureCodeVisionOverlay();
        codeVisionRoot.SetActive(true);
        codeVisionGroup.alpha = 1f;
        codeVeil.color = new Color(0f, 0.08f, 0.025f, codeVisionOpacity);
        background.color = new Color(0f, 0f, 0f, 0.2f);
        SetRevealCellsVisible(true);

        float fallbackElapsed = 0f;
        float visionDuration = Mathf.Max(0.1f, codeVisionHoldDuration);

        while (fallbackElapsed < visionDuration)
        {
            fallbackElapsed += Time.unscaledDeltaTime;
            UpdateCodeVision(fallbackElapsed);
            yield return null;
        }

        yield return RevealNormalView();
    }

    private IEnumerator RevealMatrixWorld()
    {
        float elapsed = 0f;
        while (elapsed < pixelRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            MatrixCodeVisionFeature.SetVision(true, Mathf.Clamp01(elapsed / pixelRevealDuration), codeRainSpeed / 90f);
            yield return null;
        }

        MatrixCodeVisionFeature.SetVision(false, 1f, codeRainSpeed / 90f);
        RestoreFractureCamera();
    }

    private void PrepareMatrixBloom()
    {
        if (runtimeProfile == null)
        {
            tutorialVolume = FindFirstObjectByType<Volume>();
            runtimeProfile = tutorialVolume != null ? tutorialVolume.profile : null;
        }
        if (runtimeProfile == null) return;

        if (!runtimeProfile.TryGet(out bloom))
        {
            bloom = runtimeProfile.Add<Bloom>(true);
            addedBloom = true;
        }

        previousBloomActive = bloom.active;
        previousBloomThreshold = bloom.threshold.value;
        previousBloomThresholdOverride = bloom.threshold.overrideState;
        previousBloomIntensity = bloom.intensity.value;
        previousBloomIntensityOverride = bloom.intensity.overrideState;
        previousBloomScatter = bloom.scatter.value;
        previousBloomScatterOverride = bloom.scatter.overrideState;

        bloom.active = true;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = matrixBloomThreshold;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = matrixBloomIntensity;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = matrixBloomScatter;
    }

    private void RestoreMatrixBloom()
    {
        if (bloom == null) return;
        if (addedBloom)
        {
            if (runtimeProfile != null) runtimeProfile.Remove<Bloom>();
        }
        else
        {
            bloom.active = previousBloomActive;
            bloom.threshold.value = previousBloomThreshold;
            bloom.threshold.overrideState = previousBloomThresholdOverride;
            bloom.intensity.value = previousBloomIntensity;
            bloom.intensity.overrideState = previousBloomIntensityOverride;
            bloom.scatter.value = previousBloomScatter;
            bloom.scatter.overrideState = previousBloomScatterOverride;
        }

        bloom = null;
        addedBloom = false;
    }

    private IEnumerator RevealNormalView()
    {
        float elapsed = 0f;
        while (elapsed < pixelRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / pixelRevealDuration);
            for (int index = 0; index < revealCells.Count; index++)
            {
                RevealCell cell = revealCells[index];
                float cellProgress = Mathf.Clamp01((progress - cell.order * 0.82f) / 0.18f);
                float flicker = 0.88f + 0.12f * Mathf.Sin(elapsed * 28f + cell.phase);
                cell.image.color = new Color(0f, 0.015f, 0.006f, (1f - cellProgress) * flicker);
            }

            codeVisionGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, progress));
            background.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.2f, 0f, progress));
            UpdateCodeVision(elapsed + codeVisionHoldDuration);
            yield return null;
        }

        SetRevealCellsVisible(false);
        codeVisionRoot.SetActive(false);
        RestoreFractureCamera();
    }

    private string BuildTerminalFrame(float progress)
    {
        string phase = GetPhase(progress);
        int percentage = Mathf.RoundToInt(progress * 100f);
        int filledBlocks = Mathf.RoundToInt(progress * 28f);
        string progressBar = "[" + new string('#', filledBlocks) + new string('-', 28 - filledBlocks) + "]";
        StringBuilder output = new StringBuilder();
        output.Append("Initiating tutorial.....\n");
        output.Append("NEURAL INTERFACE // STARTUP SEQUENCE 07-A\n");
        output.Append("----------------------------------------\n");
        output.Append("PHASE: ").Append(phase).Append("\n");
        output.Append("PROGRESS: ").Append(progressBar).Append(' ').Append(percentage).Append("%\n\n");

        foreach (string line in recentLines) output.Append(line).Append('\n');
        output.Append("\n> ").Append(phase).Append("... ").Append((Time.unscaledTime % 1f < 0.5f) ? "_" : " ");
        return output.ToString();
    }

    private string BuildFracturedText(string stableText, string replacementCharacters, System.Random random, float progress)
    {
        StringBuilder fractured = new StringBuilder(stableText.Length + 32);
        string[] lines = stableText.Split('\n');
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            if (lineIndex > 0) fractured.Append('\n');

            int shear = lineIndex > 4 && progress > 0.2f ? Mathf.RoundToInt(Mathf.Sin(lineIndex * 2.7f + progress * 18f) * progress * 5f) : 0;
            if (shear > 0) fractured.Append(new string(' ', shear));

            for (int characterIndex = 0; characterIndex < line.Length; characterIndex++)
            {
                char character = line[characterIndex];
                bool mutate = char.IsLetterOrDigit(character) && random.NextDouble() < Mathf.Lerp(0.08f, 0.72f, progress);
                fractured.Append(mutate ? replacementCharacters[random.Next(replacementCharacters.Length)] : character);

                if (progress > 0.48f && random.NextDouble() < progress * 0.08f) fractured.Append(character);
            }
        }

        return fractured.ToString();
    }

    private string BuildTearingText(string source, int visibleCharacters, float progress)
    {
        if (visibleCharacters <= 0) return progress < 0.92f ? "_" : string.Empty;

        int safeCount = Mathf.Clamp(visibleCharacters, 0, source.Length);
        StringBuilder tearing = new StringBuilder(source.Substring(0, safeCount));
        if (progress > 0.2f && tearing.Length > 12)
        {
            int tearStart = Mathf.Clamp(Mathf.RoundToInt(tearing.Length * (0.35f + progress * 0.3f)), 0, tearing.Length - 1);
            int tearLength = Mathf.Min(Mathf.RoundToInt(3f + progress * 13f), tearing.Length - tearStart);
            tearing.Remove(tearStart, tearLength);
            tearing.Insert(tearStart, new string(' ', tearLength));
        }

        tearing.Append(progress < 0.94f ? "_" : " ");
        return tearing.ToString();
    }

    private static string CreateDiagnosticLine(int index)
    {
        string line = DiagnosticLines[index % DiagnosticLines.Length];
        string result = index % 5 == 4 ? " [HOLD]" : index % 3 == 0 ? " [LOCK]" : string.Empty;
        return line + result;
    }

    private void EnsureCodeVisionOverlay()
    {
        if (bootCanvas == null || codeVisionRoot != null) return;

        codeVisionRoot = new GameObject("Code Vision", typeof(RectTransform), typeof(CanvasGroup));
        codeVisionRoot.transform.SetParent(bootCanvas.transform, false);
        RectTransform codeVisionRect = codeVisionRoot.GetComponent<RectTransform>();
        StretchFull(codeVisionRect);
        codeVisionGroup = codeVisionRoot.GetComponent<CanvasGroup>();
        codeVisionGroup.interactable = false;
        codeVisionGroup.blocksRaycasts = false;

        codeVeil = CreateImage("Code Vision Veil", codeVisionRoot.transform, new Color(0f, 0.08f, 0.025f, 0f));
        StretchFull(codeVeil.rectTransform);
        codeVeil.raycastTarget = false;

        CreateCodeColumns(0.18f, 13, 0.55f, 0.85f, 0.7f);
        CreateCodeColumns(0.38f, 18, 0.8f, 1.15f, 0.52f);
        CreateCodeColumns(0.7f, 12, 1.2f, 1.6f, 0.36f);
        CreateRevealCells();
        SetRevealCellsVisible(false);
        codeVisionRoot.SetActive(false);
    }

    private void CreateCodeColumns(float depth, int count, float minSpeed, float maxSpeed, float alpha)
    {
        System.Random random = new System.Random(Mathf.RoundToInt(depth * 1000f) + count);
        for (int index = 0; index < count; index++)
        {
            GameObject columnObject = new GameObject("Code Stream", typeof(RectTransform), typeof(Text));
            columnObject.transform.SetParent(codeVisionRoot.transform, false);
            Text text = columnObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.RoundToInt(Mathf.Lerp(14f, 27f, depth));
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.08f, 1f, 0.25f, alpha);
            text.raycastTarget = false;

            StringBuilder glyphs = new StringBuilder();
            const string alphabet = "01ABCDEFGHIJKLMNOPQRSTUVWXYZ<>[]{}|+-*/";
            int glyphCount = Mathf.RoundToInt(Mathf.Lerp(18f, 32f, depth));
            for (int glyphIndex = 0; glyphIndex < glyphCount; glyphIndex++)
            {
                glyphs.Append(alphabet[random.Next(alphabet.Length)]).Append('\n');
            }
            text.text = glyphs.ToString();

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(-880f + index * (1760f / Mathf.Max(1, count - 1)), random.Next(-900, 80));
            rect.sizeDelta = new Vector2(Mathf.Lerp(25f, 44f, depth), 1000f);
            codeColumns.Add(new CodeColumn { rect = rect, text = text, speed = codeRainSpeed * Mathf.Lerp(minSpeed, maxSpeed, (float)random.NextDouble()), resetY = -980f, baseAlpha = alpha });
        }
    }

    private void CreateRevealCells()
    {
        System.Random random = new System.Random(7331);
        for (int row = 0; row < revealRows; row++)
        {
            for (int column = 0; column < revealColumns; column++)
            {
                Image cell = CreateImage("Code Reveal Cell", codeVisionRoot.transform, new Color(0f, 0.015f, 0.006f, 0f));
                RectTransform rect = cell.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(column * (1920f / revealColumns) - 960f, row * (1080f / revealRows) - 540f);
                rect.sizeDelta = new Vector2(1920f / revealColumns + 1f, 1080f / revealRows + 1f);
                cell.raycastTarget = false;
                revealCells.Add(new RevealCell { image = cell, order = Mathf.Clamp01((float)random.NextDouble()), phase = (float)random.NextDouble() * 6.28f });
            }
        }
    }

    private void SetRevealCellsVisible(bool visible)
    {
        for (int index = 0; index < revealCells.Count; index++)
        {
            if (revealCells[index].image == null) continue;
            revealCells[index].image.gameObject.SetActive(visible);
            Color color = revealCells[index].image.color;
            revealCells[index].image.color = new Color(color.r, color.g, color.b, visible ? 1f : 0f);
        }
    }

    private void UpdateCodeVision(float elapsed)
    {
        if (codeVisionRoot == null) return;

        for (int index = 0; index < codeColumns.Count; index++)
        {
            CodeColumn column = codeColumns[index];
            Vector2 position = column.rect.anchoredPosition;
            position.y -= column.speed * Time.unscaledDeltaTime;
            if (position.y < column.resetY) position.y = 80f + index * 13f;
            column.rect.anchoredPosition = position;

            Color color = column.text.color;
            float depthFade = 0.7f + 0.3f * Mathf.Sin(elapsed * 2.2f + index * 0.77f);
            column.text.color = new Color(color.r, color.g, color.b, column.baseAlpha * depthFade);
        }
    }

    private void CaptureFractureCamera()
    {
        fractureCamera = Camera.main;
        if (fractureCamera == null) return;

        previousCameraLocalPosition = fractureCamera.transform.localPosition;
        previousCameraLocalRotation = fractureCamera.transform.localRotation;
        cameraTransformCaptured = true;
    }

    private void ApplyCameraFracture(float elapsed, float totalDuration)
    {
        if (!cameraTransformCaptured || fractureCamera == null) return;

        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, totalDuration));
        float envelope = Mathf.Sin(progress * Mathf.PI);
        float noiseX = Mathf.Sin(elapsed * 41f) + 0.4f * Mathf.Sin(elapsed * 83f);
        float noiseY = Mathf.Sin(elapsed * 53f + 1.7f) + 0.35f * Mathf.Sin(elapsed * 97f);
        float strength = worldFractureStrength * envelope;
        Transform cameraTransform = fractureCamera.transform;
        cameraTransform.localPosition = previousCameraLocalPosition + new Vector3(noiseX * 0.012f, noiseY * 0.009f, 0f) * strength;
        cameraTransform.localRotation = previousCameraLocalRotation * Quaternion.Euler(noiseY * 0.65f * strength, noiseX * 0.45f * strength, noiseX * 0.8f * strength);
    }

    private void RestoreFractureCamera()
    {
        if (!cameraTransformCaptured || fractureCamera == null) return;

        fractureCamera.transform.localPosition = previousCameraLocalPosition;
        fractureCamera.transform.localRotation = previousCameraLocalRotation;
        cameraTransformCaptured = false;
        fractureCamera = null;
    }

    private static string GetPhase(float progress)
    {
        if (progress < 0.18f) return "INITIALIZING CORE";
        if (progress < 0.4f) return "CHECKING MEMORY";
        if (progress < 0.63f) return "LOADING SIMULATION";
        if (progress < 0.84f) return "CALIBRATING VISION";
        return "FINALIZING SYSTEMS";
    }

    private void FinishSequence()
    {
        MatrixCodeVisionFeature.SetVision(false, 1f, codeRainSpeed / 90f);
        RestoreMatrixBloom();
        SetRevealCellsVisible(false);
        if (codeVisionRoot != null) codeVisionRoot.SetActive(false);
        RestoreFractureCamera();
        RestoreDepthOfField();
        StopAndReleaseAudio();
        isRunning = false;
        sequenceStarted = false;
    }

    private void HandControlBackToPlayer()
    {
        Time.timeScale = previousTimeScale;
        if (playerController != null) playerController.InputEnabled = previousInputEnabled;
        OnPlayerWokeUp?.Invoke();
        StartCoroutine(FadeInTutorialMusic());
    }

    private IEnumerator FadeInTutorialMusic()
    {
        if (musicSource == null || tutorialMusicClip == null) yield break;

        musicSource.clip = tutorialMusicClip;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            musicSource.volume = musicVolume * Mathf.Clamp01(elapsed / musicFadeInDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    private void RestoreDepthOfField()
    {
        if (depthOfField == null) return;

        if (addedDepthOfField)
        {
            if (runtimeProfile != null) runtimeProfile.Remove<DepthOfField>();
        }
        else
        {
            depthOfField.active = previousDepthOfFieldActive;
            depthOfField.mode.value = previousDepthOfFieldMode;
            depthOfField.mode.overrideState = previousModeOverride;
            depthOfField.gaussianStart.value = previousGaussianStart;
            depthOfField.gaussianStart.overrideState = previousStartOverride;
            depthOfField.gaussianEnd.value = previousGaussianEnd;
            depthOfField.gaussianEnd.overrideState = previousEndOverride;
            depthOfField.gaussianMaxRadius.value = previousGaussianRadius;
            depthOfField.gaussianMaxRadius.overrideState = previousRadiusOverride;
        }

        depthOfField = null;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying || !sequenceStarted) return;

        StopAllCoroutines();
        MatrixCodeVisionFeature.SetVision(false, 1f, codeRainSpeed / 90f);
        RestoreMatrixBloom();
        SetRevealCellsVisible(false);
        if (codeVisionRoot != null) codeVisionRoot.SetActive(false);
        RestoreFractureCamera();
        RestoreDepthOfField();
        Time.timeScale = previousTimeScale;
        if (playerController != null) playerController.InputEnabled = previousInputEnabled;
        StopAndReleaseAudio();
        isRunning = false;
    }

    private void StopAndReleaseAudio()
    {
        if (ambienceSource != null)
        {
            ambienceSource.Stop();
            Destroy(ambienceSource.gameObject);
            ambienceSource = null;
        }

        if (cueSource != null)
        {
            cueSource.Stop();
            Destroy(cueSource.gameObject);
            cueSource = null;
        }
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "Initiating tutorial.....";
        return text;
    }

    private static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
