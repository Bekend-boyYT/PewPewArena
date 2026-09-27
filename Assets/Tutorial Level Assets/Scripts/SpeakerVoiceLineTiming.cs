using System.Collections.Generic;
using UnityEngine;

// Shared by SpeakerDialogue (subtitle reveal) and SpeakerVoiceLineGenerator (mumble audio)
// so word timing can never drift out of sync between the two.
public static class SpeakerVoiceLineTiming
{
    public const float BaseSecondsPerWord = 0.16f;
    public const float SecondsPerCharacter = 0.045f;
    public const float MinWordDuration = 0.14f;
    public const float MaxWordDuration = 0.6f;
    public const float GapBetweenWords = 0.09f;
    public const float PunctuationPause = 0.18f;

    public readonly struct WordTiming
    {
        public readonly string Text;
        public readonly float StartTime;
        public readonly float Duration;

        public WordTiming(string text, float startTime, float duration)
        {
            Text = text;
            StartTime = startTime;
            Duration = duration;
        }
    }

    public static List<WordTiming> BuildTimeline(string sentence, out float totalDuration)
    {
        List<WordTiming> timeline = new List<WordTiming>();
        totalDuration = 0f;
        if (string.IsNullOrWhiteSpace(sentence)) return timeline;

        string[] words = sentence.Split(new[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        float cursor = 0f;

        foreach (string word in words)
        {
            float duration = GetWordDuration(word);
            timeline.Add(new WordTiming(word, cursor, duration));
            cursor += duration + GapBetweenWords;
            if (EndsWithPunctuation(word)) cursor += PunctuationPause;
        }

        totalDuration = timeline.Count > 0 ? cursor - GapBetweenWords : 0f;
        return timeline;
    }

    public static float GetWordDuration(string word)
    {
        string letters = word.Trim(',', '.', '!', '?', ';', ':');
        float duration = BaseSecondsPerWord + SecondsPerCharacter * letters.Length;
        return Mathf.Clamp(duration, MinWordDuration, MaxWordDuration);
    }

    private static bool EndsWithPunctuation(string word)
    {
        if (string.IsNullOrEmpty(word)) return false;
        char last = word[word.Length - 1];
        return last == ',' || last == '.' || last == '!' || last == '?';
    }
}
