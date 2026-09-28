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
    [SerializeField, Min(1f)] private float terminalDuration = 14f;
    [SerializeField, Min(0.03f)] private float codeLineInterval = 0.14f;
    [SerializeField, Min(0f)] private float distortionDuration = 3f;
    [SerializeField, Min(0f)] private float eraseDuration = 1.8f;
    [SerializeField, Min(0f)] private float blackHoldDuration = 2f;
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
    private AudioClip tutorialMusicClip;

    private void Awake()
    {
        if (bootCanvas == null) BuildBootUI();
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
        System.Random random = new System.Random();

        while (elapsed < terminalDuration)
        {
            if (elapsed >= nextLineAt)
            {
                recentLines.Add(CreateTerminalLine(random));
                if (recentLines.Count > 18) recentLines.RemoveAt(0);
                terminalText.text = BuildTerminalFrame(elapsed / terminalDuration);
                if (elapsed >= nextTickAt)
                {
                    PlayCue(terminalTickClip, 0.24f);
                    nextTickAt = elapsed + 0.42f;
                }
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
        yield return DistortTerminalText();
        PlayCue(eraseSweepClip, 0.65f);
        yield return EraseTerminalText();
        terminalText.text = string.Empty;
        if (blackHoldDuration > 0f) yield return new WaitForSecondsRealtime(blackHoldDuration);

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

    private IEnumerator DistortTerminalText()
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
                StringBuilder distortedText = new StringBuilder(stableText.Length);
                foreach (char character in stableText)
                {
                    if (char.IsLetterOrDigit(character))
                    {
                        distortedText.Append(replacementCharacters[random.Next(replacementCharacters.Length)]);
                    }
                    else
                    {
                        distortedText.Append(character);
                    }
                }

                terminalText.text = distortedText.ToString();
                if (elapsed >= nextGlitchAt)
                {
                    PlayCue(glitchClip, 0.22f);
                    nextGlitchAt = elapsed + 0.28f;
                }
                nextUpdateAt += 0.045f;
            }

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
            terminalText.text = scrambledText.Substring(0, visibleCharacters);
            if (visibleCharacters > 0) terminalText.text += "_";
            yield return null;
        }
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

    private static string GetPhase(float progress)
    {
        if (progress < 0.18f) return "INITIALIZING CORE";
        if (progress < 0.4f) return "CHECKING MEMORY";
        if (progress < 0.63f) return "LOADING SIMULATION";
        if (progress < 0.84f) return "CALIBRATING VISION";
        return "FINALIZING SYSTEMS";
    }

    private static string CreateTerminalLine(System.Random random)
    {
        const string hex = "0123456789ABCDEF";
        StringBuilder address = new StringBuilder(8);
        for (int index = 0; index < 8; index++) address.Append(hex[random.Next(hex.Length)]);

        string[] status = { "VERIFY", "ALLOC", "SYNC", "LOAD", "READY", "MOUNT", "PATCH", "SCAN", "LINK" };
        string[] result = { "[ OK ]", "[ DONE ]", "[ PASS ]" };
        return "> 0x" + address + "  " + status[random.Next(status.Length)] + "  " + result[random.Next(result.Length)];
    }

    private void FinishSequence()
    {
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
