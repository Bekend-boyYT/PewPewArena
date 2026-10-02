using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[RequireComponent(typeof(AudioSource))]
public class SpeakerDialogue : MonoBehaviour
{
    [Header("Line")]
    [SerializeField, TextArea(2, 4)]
    private string subtitleLine = "Welcome to Pew Pew Arena, in this short tutorial we will go over the basics of how to play. Press W A S D to walk";
    [SerializeField] private string voiceClipResourcePath = "Voice/wake_up_greeting";
    [SerializeField, Min(0f)] private float wakeUpDelay = 1f;

    // Fired once the wake-up greeting has fully finished (audio + subtitle fade-out).
    public static event Action OnGreetingFinished;

    [Header("Talk Scale")]
    [SerializeField, Range(0f, 2f)] private float scaleIntensity = 0.35f;
    [SerializeField, Min(1f)] private float springStiffness = 220f;
    [SerializeField, Min(0f)] private float springDamping = 14f;

    [Header("Subtitle UI")]
    [SerializeField] private GameObject subtitleCanvas;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private Text subtitleText;
    [SerializeField, Min(0f)] private float subtitleFadeDuration = 0.3f;

    private AudioSource audioSource;
    private Vector3 baseScale;
    private float scaleMultiplier = 1f;
    private float scaleVelocity;
    private readonly float[] sampleBuffer = new float[256];
    private Coroutine playbackRoutine;

    private void Awake()
    {
        baseScale = transform.localScale;
        audioSource = GetComponent<AudioSource>();
        if (subtitleCanvas == null) BuildSubtitleUI();
    }

    private void OnEnable()
    {
        TutorialBootSequence.OnPlayerWokeUp += HandlePlayerWokeUp;
    }

    private void OnDisable()
    {
        TutorialBootSequence.OnPlayerWokeUp -= HandlePlayerWokeUp;
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        transform.localScale = baseScale;
        scaleMultiplier = 1f;
        scaleVelocity = 0f;
        if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;
    }

    private void HandlePlayerWokeUp()
    {
        if (!Application.isPlaying) return;
        if (playbackRoutine != null) StopCoroutine(playbackRoutine);
        playbackRoutine = StartCoroutine(PlayGreeting());
    }

    [ContextMenu("Preview Greeting")]
    private void PreviewGreeting()
    {
        if (!Application.isPlaying) return;
        HandlePlayerWokeUp();
    }

    // Public entry point for other systems (e.g. HangerDoorController) to make the speaker say an arbitrary line.
    public void SayLine(string text, string clipResourcePath, Action onFinished = null)
    {
        if (!Application.isPlaying) return;
        if (playbackRoutine != null) StopCoroutine(playbackRoutine);
        playbackRoutine = StartCoroutine(PlaySubtitleLine(text, clipResourcePath, onFinished));
    }

    private IEnumerator PlayGreeting()
    {
        if (wakeUpDelay > 0f) yield return new WaitForSeconds(wakeUpDelay);

        yield return PlaySubtitleLine(subtitleLine, voiceClipResourcePath);
        OnGreetingFinished?.Invoke();
    }

    private IEnumerator PlaySubtitleLine(string text, string clipResourcePath, Action onFinished = null)
    {
        AudioClip clip = Resources.Load<AudioClip>(clipResourcePath);
        List<SpeakerVoiceLineTiming.WordTiming> timeline = SpeakerVoiceLineTiming.BuildTimeline(text, out float lineDuration);
        float playDuration = Mathf.Max(clip != null ? clip.length : 0f, lineDuration);

        audioSource.clip = clip;
        audioSource.Play();
        yield return FadeSubtitle(1f);

        float elapsed = 0f;
        int revealedWordCount = 0;
        while (elapsed < playDuration)
        {
            while (revealedWordCount < timeline.Count && timeline[revealedWordCount].StartTime <= elapsed)
            {
                revealedWordCount++;
            }

            UpdateSubtitleText(timeline, revealedWordCount);
            UpdateTalkScale();

            elapsed += Time.deltaTime;
            yield return null;
        }

        UpdateSubtitleText(timeline, timeline.Count);
        yield return FadeSubtitle(0f);

        transform.localScale = baseScale;
        playbackRoutine = null;
        onFinished?.Invoke();
    }

    private void UpdateSubtitleText(List<SpeakerVoiceLineTiming.WordTiming> timeline, int revealedWordCount)
    {
        if (subtitleText == null) return;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int index = 0; index < revealedWordCount; index++)
        {
            if (index > 0) builder.Append(' ');
            builder.Append(timeline[index].Text);
        }

        subtitleText.text = builder.ToString();
    }

    private void UpdateTalkScale()
    {
        audioSource.GetOutputData(sampleBuffer, 0);

        float sumSquares = 0f;
        for (int index = 0; index < sampleBuffer.Length; index++)
        {
            sumSquares += sampleBuffer[index] * sampleBuffer[index];
        }

        float rms = Mathf.Sqrt(sumSquares / sampleBuffer.Length);
        float target = 1f + rms * scaleIntensity;

        // Underdamped spring so loud bursts punch past the target and settle back, instead of a flat lerp.
        float acceleration = (target - scaleMultiplier) * springStiffness - scaleVelocity * springDamping;
        scaleVelocity += acceleration * Time.deltaTime;
        scaleMultiplier += scaleVelocity * Time.deltaTime;
        transform.localScale = baseScale * scaleMultiplier;
    }

    private IEnumerator FadeSubtitle(float targetAlpha)
    {
        if (subtitleCanvasGroup == null) yield break;

        float startAlpha = subtitleCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < subtitleFadeDuration)
        {
            elapsed += Time.deltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / subtitleFadeDuration);
            yield return null;
        }

        subtitleCanvasGroup.alpha = targetAlpha;
    }

    private void BuildSubtitleUI()
    {
        subtitleCanvas = new GameObject("Speaker Subtitles", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        subtitleCanvas.transform.SetParent(transform, false);

        Canvas canvas = subtitleCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = subtitleCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        subtitleCanvasGroup = subtitleCanvas.GetComponent<CanvasGroup>();
        subtitleCanvasGroup.alpha = 0f;
        subtitleCanvasGroup.interactable = false;
        subtitleCanvasGroup.blocksRaycasts = false;

        GameObject textObject = new GameObject("Subtitle Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(subtitleCanvas.transform, false);
        subtitleText = textObject.GetComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 36;
        subtitleText.alignment = TextAnchor.LowerCenter;
        subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
        subtitleText.color = Color.white;
        subtitleText.raycastTarget = false;

        RectTransform textRect = subtitleText.rectTransform;
        textRect.anchorMin = new Vector2(0.1f, 0f);
        textRect.anchorMax = new Vector2(0.9f, 0.25f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
}
