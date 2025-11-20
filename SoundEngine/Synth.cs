using FFmpeg.AutoGen;
using Ownaudio.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Synth
    {
        public const int SAMPLE_RATE = 44100;
        public const short BITS_PER_SAMPLE = 16;
        public int samplesThisFrame = 0;
        public int offset = 0;
        public double Period(float frequency) => (Math.PI * 2 * frequency) / SAMPLE_RATE;
        public List<short> outputArray = new List<short>();
        private static woLib WaveOut = new woLib();


        public Synth()
        {
            WaveOut.InitWODevice(SAMPLE_RATE, 1, (int)BITS_PER_SAMPLE, false);
        }

        public void Update(double deltaTime, float volume = 1f, float pitch = 440f, bool updateOutput = false)
        {

            samplesThisFrame = (int)(deltaTime * (SAMPLE_RATE));
            short[] wave = new short[samplesThisFrame];
            sine(wave, volume, pitch);
            if(updateOutput)
            {
                outputArray.AddRange(wave);
            }
            else
            {
                outputArray.Clear();
            }
            offset = (offset + samplesThisFrame) % SAMPLE_RATE;
            IntPtr outWave = Marshal.AllocHGlobal(samplesThisFrame * sizeof(short));
            Marshal.Copy(wave, 0, outWave, samplesThisFrame);
            WaveOut.SendWODevice(outWave, (uint)(samplesThisFrame * sizeof(short)));
            Marshal.FreeHGlobal(outWave);

        }

        private void sine(in short[] sampleArray, float volume, float pitch)
        {
            for(int i = 0; i < samplesThisFrame; i++)
            {
                sampleArray[i] = Convert.ToInt16(short.MaxValue * volume * Math.Sign(Math.Sin(Period(pitch) * (i + offset))));
            }
        }

        public void OutputSound()
        {
            short[] outputWave = outputArray.ToArray();
            int sampleCount = outputWave.Length;
            byte[] binaryWave = new byte[sampleCount * sizeof(short)];
            Buffer.BlockCopy(outputWave, 0, binaryWave, 0, sampleCount * sizeof(short));
            using (FileStream file = new FileStream("test.wav", FileMode.Create, System.IO.FileAccess.Write))
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                short blockAlign = BITS_PER_SAMPLE / 8;
                int subChunkTwoSize = sampleCount * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' });
                binaryWriter.Write(36 + subChunkTwoSize);
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                binaryWriter.Write(16);
                binaryWriter.Write((short)1);
                binaryWriter.Write((short)1);
                binaryWriter.Write(SAMPLE_RATE);
                binaryWriter.Write(SAMPLE_RATE * blockAlign);
                binaryWriter.Write(blockAlign);
                binaryWriter.Write(BITS_PER_SAMPLE);
                binaryWriter.Write(new[] { 'd', 'a', 't', 'a' });
                binaryWriter.Write(subChunkTwoSize);
                binaryWriter.Write(binaryWave);
                memoryStream.Position = 0;

                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);
                file.Write(bytes, 0, bytes.Length);
                memoryStream.Close();

            }
        }



    }
}
