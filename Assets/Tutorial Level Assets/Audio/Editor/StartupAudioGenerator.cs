using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class StartupAudioGenerator
{
    private const int SampleRate = 22050;
    private const string OutputFolder = "Tutorial Level Assets/Audio/Resources/Startup";

    static StartupAudioGenerator()
    {
        EditorApplication.delayCall += GenerateMissingAssets;
    }

    [MenuItem("Tools/Tutorial/Generate Missing Startup Audio")]
    private static void GenerateFromMenu()
    {
        GenerateMissingAssets();
    }

    private static void GenerateMissingAssets()
    {
        string directory = Path.Combine(Application.dataPath, OutputFolder);
        Directory.CreateDirectory(directory);

        bool createdAny = false;
        createdAny |= CreateIfMissing(directory, "power_up.wav", 0.9f, CreatePowerUp);
        createdAny |= CreateIfMissing(directory, "boot_hum.wav", 2f, CreateBootHum);
        createdAny |= CreateIfMissing(directory, "terminal_tick.wav", 0.075f, CreateTerminalTick);
        createdAny |= CreateIfMissing(directory, "key_confirm.wav", 0.65f, CreateKeyConfirm);
        createdAny |= CreateIfMissing(directory, "glitch.wav", 0.16f, CreateGlitch);
        createdAny |= CreateIfMissing(directory, "erase_sweep.wav", 0.9f, CreateEraseSweep);
        createdAny |= CreateIfMissing(directory, "reveal_chime.wav", 1.15f, CreateRevealChime);
        createdAny |= CreateIfMissing(directory, "diagnostic_lock.wav", 0.32f, CreateDiagnosticLock);
        createdAny |= CreateIfMissing(directory, "scar_tear.wav", 0.75f, CreateScarTear);

        if (createdAny) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static bool CreateIfMissing(string directory, string fileName, float duration, Func<int, int, float> sample)
    {
        string path = Path.Combine(directory, fileName);
        if (File.Exists(path)) return false;

        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        for (int index = 0; index < sampleCount; index++)
        {
            samples[index] = Mathf.Clamp(sample(index, sampleCount), -0.95f, 0.95f);
        }

        WritePcmWave(path, samples);
        return true;
    }

    private static float CreatePowerUp(int index, int count)
    {
        float time = index / (float)SampleRate;
        float envelope = Mathf.Sin(Mathf.PI * index / count);
        float phase = 2f * Mathf.PI * (125f * time + 215f * time * time);
        return 0.24f * envelope * (Mathf.Sin(phase) + 0.2f * Mathf.Sin(phase * 2f));
    }

    private static float CreateBootHum(int index, int count)
    {
        float time = index / (float)SampleRate;
        float envelope = Mathf.Min(1f, index / 900f) * Mathf.Min(1f, (count - index) / 900f);
        return envelope * (0.065f * Mathf.Sin(2f * Mathf.PI * 55f * time) + 0.02f * Mathf.Sin(2f * Mathf.PI * 110f * time));
    }

    private static float CreateTerminalTick(int index, int count)
    {
        float time = index / (float)SampleRate;
        float envelope = Mathf.Exp(-58f * time);
        return 0.16f * envelope * (Mathf.Sin(2f * Mathf.PI * 1480f * time) + 0.2f * Mathf.Sin(2f * Mathf.PI * 2200f * time));
    }

    private static float CreateKeyConfirm(int index, int count)
    {
        float time = index / (float)SampleRate;
        float envelope = Mathf.Exp(-4f * time) * Mathf.Clamp01(index / 180f);
        float phase = 2f * Mathf.PI * time;
        return 0.14f * envelope * (Mathf.Sin(880f * phase) + 0.55f * Mathf.Sin(1320f * phase));
    }

    private static float CreateGlitch(int index, int count)
    {
        float time = index / (float)SampleRate;
        float random = Mathf.PerlinNoise(index * 0.071f, 0.37f) * 2f - 1f;
        float envelope = Mathf.Exp(-24f * time) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 38f * time));
        return 0.16f * envelope * (random * 0.65f + Mathf.Sin(2f * Mathf.PI * 1800f * time) * 0.35f);
    }

    private static float CreateEraseSweep(int index, int count)
    {
        float progress = index / (float)count;
        float time = index / (float)SampleRate;
        float frequency = Mathf.Lerp(2100f, 260f, progress);
        float phase = 2f * Mathf.PI * (2100f * time - 920f * time * time / 0.9f);
        float envelope = Mathf.Sin(Mathf.PI * progress);
        return 0.18f * envelope * Mathf.Sin(phase) + 0.04f * envelope * Mathf.Sin(phase * 0.5f + frequency * 0.001f);
    }

    private static float CreateRevealChime(int index, int count)
    {
        float time = index / (float)SampleRate;
        float attack = Mathf.Clamp01(index / 400f);
        float envelope = attack * Mathf.Exp(-2.8f * time);
        return 0.13f * envelope * (Mathf.Sin(2f * Mathf.PI * 660f * time) + 0.55f * Mathf.Sin(2f * Mathf.PI * 990f * time) + 0.25f * Mathf.Sin(2f * Mathf.PI * 1320f * time));
    }

    private static float CreateDiagnosticLock(int index, int count)
    {
        float time = index / (float)SampleRate;
        float envelope = Mathf.Exp(-12f * time) * Mathf.Clamp01(index / 80f);
        float mechanical = Mathf.Sin(2f * Mathf.PI * 96f * time) * 0.35f;
        float confirmation = Mathf.Sin(2f * Mathf.PI * 740f * time) + 0.45f * Mathf.Sin(2f * Mathf.PI * 1110f * time);
        return 0.18f * envelope * (mechanical + confirmation);
    }

    private static float CreateScarTear(int index, int count)
    {
        float time = index / (float)SampleRate;
        float progress = index / (float)count;
        float envelope = Mathf.Sin(Mathf.PI * progress);
        float noise = Mathf.PerlinNoise(index * 0.19f, 0.61f) * 2f - 1f;
        float scrape = Mathf.Sin(2f * Mathf.PI * (160f + progress * 1300f) * time);
        return 0.2f * envelope * (noise * 0.7f + scrape * 0.3f);
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
                writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
            }
        }
    }
}