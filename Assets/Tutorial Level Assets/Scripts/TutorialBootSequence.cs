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
    [SerializeField, Range(0f, 1f)] private float scarOpacity = 0.8f;
    [SerializeField, Range(0f, 1f)] private float worldFractureStrength = 0.65f;

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
    private bool sequenceStarted;
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
    private AudioClip scarTearClip;
    private AudioClip tutorialMusicClip;
    private readonly List<RealityScarPiece> realityScars = new List<RealityScarPiece>();
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

    private sealed class RealityScarPiece
    {
        public Image glow;
        public Image core;
        public float decayStart;
        public float phase;
    }

    private void Awake()
    {
        if (bootCanvas == null) BuildBootUI();
        EnsureScarOverlay();
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
        scarTearClip = Resources.Load<AudioClip>("Startup/scar_tear");
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
        yield return DecayRealityScars();

        PlayCue(revealChimeClip, 0.7f);
        HandControlBackToPlayer();
        elapsed = 0f;
        float revealDuration = Mathf.Max(overlayFadeDuration, focusRecoveryDuration);
        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float overlayProgress = overlayFadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / overlayFadeDuration);
            bootCanvasGroup.alpha = 1f - overlayProgress;

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

    private IEnumerator DecayRealityScars()
    {
        EnsureScarOverlay();
        SetScarsVisible(true);
        PlayCue(scarTearClip, 0.7f);
        float elapsed = 0f;
        float scarDuration = Mathf.Max(0.1f, blackHoldDuration);

        while (elapsed < scarDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / scarDuration);
            float pulse = 0.72f + 0.28f * Mathf.Sin(elapsed * 11f);
            float alpha = scarOpacity * (1f - progress) * pulse;
            for (int index = 0; index < realityScars.Count; index++)
            {
                RealityScarPiece scar = realityScars[index];
                if (scar == null || scar.core == null || scar.glow == null) continue;

                float destruction = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(scar.decayStart, 1f, progress));
                float flicker = 0.72f + 0.28f * Mathf.Sin(elapsed * 18f + scar.phase);
                float pieceAlpha = alpha * (1f - destruction) * flicker;
                scar.core.color = new Color(0.7f, 0.98f, 1f, pieceAlpha);
                scar.glow.color = new Color(0.05f, 0.48f, 1f, pieceAlpha * 0.24f);
                scar.core.rectTransform.localScale = new Vector3(1f - destruction * 0.9f, 1f, 1f);
                scar.glow.rectTransform.localScale = new Vector3(1f - destruction * 0.82f, 1f, 1f);
            }

            ApplyCameraFracture(distortionDuration + eraseDuration + elapsed, distortionDuration + eraseDuration + scarDuration);
            yield return null;
        }

        SetScarsVisible(false);
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

    private void EnsureScarOverlay()
    {
        if (bootCanvas == null || realityScars.Count > 0) return;

        Vector2[][] scarPaths =
        {
            new[] { new Vector2(-40f, 20f), new Vector2(-180f, 120f), new Vector2(-310f, 180f), new Vector2(-510f, 330f) },
            new[] { new Vector2(10f, 10f), new Vector2(150f, 95f), new Vector2(290f, 170f), new Vector2(520f, 250f) },
            new[] { new Vector2(0f, 0f), new Vector2(-20f, -130f), new Vector2(-60f, -260f), new Vector2(-120f, -430f) },
            new[] { new Vector2(30f, -20f), new Vector2(170f, -105f), new Vector2(300f, -200f), new Vector2(470f, -330f) }
        };

        for (int pathIndex = 0; pathIndex < scarPaths.Length; pathIndex++)
        {
            AddScarPath(scarPaths[pathIndex], pathIndex);
        }

        AddScarPath(new[] { new Vector2(-180f, 120f), new Vector2(-270f, 45f), new Vector2(-390f, 20f) }, 7);
        AddScarPath(new[] { new Vector2(150f, 95f), new Vector2(210f, -10f), new Vector2(350f, -80f) }, 9);
        AddScarPath(new[] { new Vector2(-20f, -130f), new Vector2(80f, -220f), new Vector2(180f, -275f) }, 11);
        SetScarsVisible(false);
    }

    private void AddScarPath(Vector2[] points, int pathIndex)
    {
        int segmentCount = Mathf.Max(1, points.Length - 1);
        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            Vector2 start = points[segmentIndex];
            Vector2 end = points[segmentIndex + 1];
            float progress = segmentIndex / (float)segmentCount;
            float width = 2.2f + ((pathIndex + segmentIndex) % 3) * 0.8f;
            RealityScarPiece scar = new RealityScarPiece
            {
                decayStart = Mathf.Clamp01(0.8f - progress * 0.65f + pathIndex * 0.012f),
                phase = pathIndex * 1.7f + segmentIndex * 0.9f,
                glow = CreateScarSegment("Reality Scar Glow", start, end, width * 4.5f, new Color(0.05f, 0.48f, 1f, 0f)),
                core = CreateScarSegment("Reality Scar Core", start, end, width, new Color(0.7f, 0.98f, 1f, 0f))
            };
            realityScars.Add(scar);
        }
    }

    private Image CreateScarSegment(string objectName, Vector2 start, Vector2 end, float width, Color color)
    {
        Image segment = CreateImage(objectName, bootCanvas.transform, color);
        RectTransform rect = segment.rectTransform;
        Vector2 direction = end - start;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (start + end) * 0.5f;
        rect.sizeDelta = new Vector2(direction.magnitude, width);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        rect.localScale = Vector3.one;
        segment.raycastTarget = false;
        return segment;
    }

    private void SetScarsVisible(bool visible)
    {
        for (int index = 0; index < realityScars.Count; index++)
        {
            RealityScarPiece scar = realityScars[index];
            if (scar == null) continue;
            if (scar.glow != null) scar.glow.gameObject.SetActive(visible);
            if (scar.core != null) scar.core.gameObject.SetActive(visible);
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
        SetScarsVisible(false);
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
        SetScarsVisible(false);
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
