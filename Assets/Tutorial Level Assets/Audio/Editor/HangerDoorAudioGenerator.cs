using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class HangerDoorAudioGenerator
{
    private const int SampleRate = 22050;
    private const string OutputFolder = "Tutorial Level Assets/Audio/Resources/Door";
    private const float ClipDuration = 3f;

    static HangerDoorAudioGenerator()
    {
        EditorApplication.delayCall += GenerateMissingAssets;
    }

    [MenuItem("Tools/Tutorial/Generate Missing Hangar Door Audio")]
    private static void GenerateFromMenu()
    {
        GenerateMissingAssets();
    }

    private static void GenerateMissingAssets()
    {
        string directory = Path.Combine(Application.dataPath, OutputFolder);
        Directory.CreateDirectory(directory);

        string path = Path.Combine(directory, "hangar_door_move.wav");
        if (File.Exists(path)) return;

        int sampleCount = Mathf.CeilToInt(ClipDuration * SampleRate);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(4242);
        float noiseState = 0f;

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)SampleRate;
            float progress = index / (float)(sampleCount - 1);

            // Short fade in/out so the loop doesn't click at the start/end of the descent.
            float envelope = Mathf.Min(1f, index / 1100f) * Mathf.Min(1f, (sampleCount - index) / 1100f);

            // Slow wobble in pitch to sell a labouring servo motor.
            float wobble = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 3.2f * time);
            float baseFrequency = 60f * wobble;
            float sawtoothPhase = (baseFrequency * time) % 1f;
            float sawtooth = 2f * sawtoothPhase - 1f;

            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            noiseState = Mathf.Lerp(noiseState, whiteNoise, 0.12f);

            float motor = 0.22f * sawtooth + 0.08f * Mathf.Sin(2f * Mathf.PI * baseFrequency * 2f * time);
            samples[index] = Mathf.Clamp(envelope * (motor + 0.1f * noiseState), -0.95f, 0.95f);
        }

        WritePcmWave(path, samples);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
