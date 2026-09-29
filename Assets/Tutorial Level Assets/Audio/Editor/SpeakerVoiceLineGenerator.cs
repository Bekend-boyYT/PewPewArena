using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class SpeakerVoiceLineGenerator
{
    private const int SampleRate = 22050;
    private const string OutputFolder = "Tutorial Level Assets/Audio/Resources/Voice";

    private static readonly (string FileName, string Text)[] Lines =
    {
        ("wake_up_greeting.wav", "Welcome to Pew Pew Arena, in this short tutorial we will go over the basics of how to play. Press W A S D to walk"),
        ("run_jump_confirmation.wav", "Good job, you can walk, now lets see if you can run AND jump at the same time. Use SPACE to jump."),
    };

    static SpeakerVoiceLineGenerator()
    {
        EditorApplication.delayCall += GenerateMissingAssets;
    }

    [MenuItem("Tools/Tutorial/Generate Missing Speaker Voice Lines")]
    private static void GenerateFromMenu()
    {
        GenerateMissingAssets();
    }

    private static void GenerateMissingAssets()
    {
        string directory = Path.Combine(Application.dataPath, OutputFolder);
        Directory.CreateDirectory(directory);

        bool generatedAny = false;
        foreach ((string fileName, string text) in Lines)
        {
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path)) continue;

            List<SpeakerVoiceLineTiming.WordTiming> timeline = SpeakerVoiceLineTiming.BuildTimeline(text, out float totalDuration);
            int sampleCount = Mathf.CeilToInt(totalDuration * SampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new System.Random(12345);

            foreach (SpeakerVoiceLineTiming.WordTiming word in timeline)
            {
                WriteMumbleBurst(samples, word, random);
            }

            WritePcmWave(path, samples);
            generatedAny = true;
        }

        if (generatedAny) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static void WriteMumbleBurst(float[] samples, SpeakerVoiceLineTiming.WordTiming word, System.Random random)
    {
        int startSample = Mathf.RoundToInt(word.StartTime * SampleRate);
        int burstLength = Mathf.RoundToInt(word.Duration * SampleRate);

        // Neutral, medium-pitch mumble: a wandering tone plus filtered noise, shaped per word.
        float baseFrequency = 170f + (float)random.NextDouble() * 90f;
        float vibratoRate = 7f + (float)random.NextDouble() * 4f;
        float noiseState = 0f;

        for (int index = 0; index < burstLength; index++)
        {
            int sampleIndex = startSample + index;
            if (sampleIndex < 0 || sampleIndex >= samples.Length) continue;

            float progress = index / (float)Mathf.Max(1, burstLength - 1);
            float time = index / (float)SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * progress);

            float vibrato = 1f + 0.06f * Mathf.Sin(2f * Mathf.PI * vibratoRate * time);
            float phase = 2f * Mathf.PI * baseFrequency * vibrato * time;
            float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f);

            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            noiseState = Mathf.Lerp(noiseState, whiteNoise, 0.35f);

            samples[sampleIndex] += 0.16f * envelope * (0.75f * tone + 0.25f * noiseState);
        }
    }

    private static void WritePcmWave(string path, float[] samples)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        int dataLength = samples.Length * channels * bitsPerSample / 8;

        using (FileStream stream = File.Create(path))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);

            foreach (float sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -0.95f, 0.95f) * short.MaxValue));
            }
        }
    }
}
