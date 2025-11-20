using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ownaudio;
using Ownaudio.Sources;

namespace CatfortSound.SoundEngine
{
    class SineGenerator
    {
        private static SourceSound? realtimeSource;
        private static bool isGenerating = false;
        private static float frequency = 440f; // A4
        private static float amplitude = 0.3f;
        private static int sampleRate = 44100;

        private SourceManager? sourceManager;

        public void InitGenerator()
        {
            OwnAudio.Initialize(@"Z:\Programming\ExtLibs\AudioLibraries");
            sourceManager = SourceManager.Instance;

            realtimeSource = sourceManager.AddRealTimeSource(1.0f, 2, "SineWave");
            Console.WriteLine("🎵 Real-Time Sine Wave Generator");
            Console.WriteLine($"Initial frequency: {frequency}Hz");

            StartGeneration();
            sourceManager.Play();
        }

        public void StopGenerator()
        {
            StopGeneration();
            OwnAudio.Free();
        }

        public void Update()
        {
            while (isGenerating)
            {
                Console.WriteLine("\n=== Sine Wave Controls ===");
                Console.WriteLine($"Frequency: {frequency:F1}Hz | Amplitude: {amplitude:F2}");
                Console.WriteLine("W/S: Frequency ±50Hz | A/D: Frequency ±10Hz");
                Console.WriteLine("Q/E: Volume ±0.1 | Space: Musical notes | X: Exit");

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W: frequency += 50f; break;
                    case ConsoleKey.S: frequency -= 50f; break;
                    case ConsoleKey.A: frequency -= 10f; break;
                    case ConsoleKey.D: frequency += 10f; break;
                    case ConsoleKey.Q: amplitude = Math.Max(0.0f, amplitude - 0.1f); break;
                    case ConsoleKey.E: amplitude = Math.Min(1.0f, amplitude + 0.1f); break;
                    //case ConsoleKey.Spacebar: await PlayMusicalNotes(); break;
                }

                // Clamp frequency to reasonable range
                frequency = Math.Clamp(frequency, 20f, 20000f);

                Console.WriteLine($"Updated - Freq: {frequency:F1}Hz, Vol: {amplitude:F2}");
            }
        }

        static void StartGeneration()
        {
            isGenerating = true;

            Task.Run(async () =>
            {
                double phase = 0;
                int bufferSize = 1024;

                while (isGenerating && realtimeSource?.State != SourceState.Idle)
                {
                    float[] buffer = GenerateSineWave(bufferSize, ref phase);
                    realtimeSource?.SubmitSamples(buffer);

                    // Control timing for smooth playback
                    await Task.Delay(10);
                }
            });
        }

        static float[] GenerateSineWave(int samples, ref double phase)
        {
            float[] buffer = new float[samples * 2]; // Stereo
            double phaseIncrement = 2.0 * Math.PI * frequency / sampleRate;

            for (int i = 0; i < samples; i++)
            {
                float sample = (float)(Math.Sin(phase) * amplitude);

                buffer[i * 2] = sample;     // Left channel
                buffer[i * 2 + 1] = sample; // Right channel

                phase += phaseIncrement;
                if (phase >= 2.0 * Math.PI)
                    phase -= 2.0 * Math.PI;
            }

            return buffer;
        }

        static async Task PlayMusicalNotes()
        {
            float[] notes = { 261.63f, 293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 493.88f, 523.25f }; // C-C octave
            string[] noteNames = { "C4", "D4", "E4", "F4", "G4", "A4", "B4", "C5" };

            Console.WriteLine("\n🎼 Playing musical scale...");

            for (int i = 0; i < notes.Length; i++)
            {
                frequency = notes[i];
                Console.WriteLine($"♪ {noteNames[i]} ({frequency:F1}Hz)");
                await Task.Delay(500);
            }

            frequency = 440f; // Reset to A4
            Console.WriteLine("Scale complete!");
        }

        static void StopGeneration()
        {
            isGenerating = false;
        }
    }
}
