using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xaml.Schema;

namespace CatfortSound.SoundEngine
{

    public class APU
    {
        private float m_masterVolume = 1.0f;
        private float Volume() => m_masterVolume * APUConstants.BASE_VOLUME;
        private Mixer? mixer = new Mixer();
        public List<short> outputArray = new List<short>();

        public int samplesThisFrame = 0;
        public int offset = 0;
        public double Period(float frequency) => (Math.PI * 2 * frequency) / APUConstants.SAMPLE_RATE;
        private static woLib WaveOut = new woLib();

        public APU()
        {
            WaveOut.InitWODevice(APUConstants.SAMPLE_RATE, 1, (uint)APUConstants.BITS_PER_SAMPLE, false);
        }
        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Math.Clamp(volume, 0, 1);
        }

        public void SetChannelVolume(float volume, int channel)
        {
            mixer?.SetChannelVolume(volume, channel);
        }

        public void SetOscilatorPitch(int pitch, int channel)
        {
            mixer?.SetOscilatorPitch(pitch, channel);
        }

        public void SetOscilatorEffect(Effect effect, int channel)
        {
            mixer?.SetOscilatorEffect(effect, channel);
        }

        public void RemoveOscilatorEffect(EffectStack.EffectSlots slot, int channel)
        {
            mixer?.RemoveOscilatorEffect(slot, channel);
        }

        public void TriggerDMC(int sample)
        {
            mixer?.TriggerDMC(sample);
        }

        public void FrameTick()
        {
            mixer?.FrameTick();
        }

        public bool Update(double deltaTime, bool accumulateOutput = false)
        {
            samplesThisFrame = (int)((deltaTime/1000f) * (APUConstants.SAMPLE_RATE));

            if(samplesThisFrame == 0)
            {
                return false;
            }

            float[]? mixBuffer = mixer?.GenerateMixBuffer(samplesThisFrame);
            if (mixBuffer is not null)
            {
                //for each sample, we'll want to multiply it by the master volume
                //these should be some number between 0 and 100 (abs)
                short[] outputWave = new short[mixBuffer.Length];
                for(int i = 0; i < mixBuffer.Length; i++)
                {
                    float masteredSample = mixBuffer[i] * m_masterVolume * APUConstants.AMPLITUDE_MAX;
                    outputWave[i] = Convert.ToInt16(masteredSample);
                }
                if(accumulateOutput)
                {
                    outputArray.AddRange(outputWave);
                }
                //output sound to speakers 
                IntPtr outWave = Marshal.AllocHGlobal(samplesThisFrame * sizeof(short));
                Marshal.Copy(outputWave, 0, outWave, samplesThisFrame);
                WaveOut.SendWODevice(outWave, (uint)(samplesThisFrame * sizeof(short)));
                Marshal.FreeHGlobal(outWave);
                return true;
            }
            return false;
        }
        public void OutputSound()
        {
            short[] outputWave = outputArray.ToArray();
            int sampleCount = outputWave.Length;
            Debug.WriteLine($"final sample count: {sampleCount}");
            byte[] binaryWave = new byte[sampleCount * sizeof(short)];
            Buffer.BlockCopy(outputWave, 0, binaryWave, 0, sampleCount * sizeof(short));
            using (FileStream file = new FileStream("test.wav", FileMode.Create, System.IO.FileAccess.Write))
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                short blockAlign = (short)(APUConstants.BITS_PER_SAMPLE / 8);
                int subChunkTwoSize = sampleCount * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' });
                binaryWriter.Write(36 + subChunkTwoSize);
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                binaryWriter.Write(16);
                binaryWriter.Write((short)1);
                binaryWriter.Write((short)1);
                binaryWriter.Write(APUConstants.SAMPLE_RATE);
                binaryWriter.Write((int)APUConstants.SAMPLE_RATE * blockAlign);
                binaryWriter.Write(blockAlign);
                binaryWriter.Write(APUConstants.BITS_PER_SAMPLE);
                binaryWriter.Write(new[] { 'd', 'a', 't', 'a' });
                binaryWriter.Write(subChunkTwoSize);
                binaryWriter.Write(binaryWave);
                memoryStream.Position = 0;

                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);
                file.Write(bytes, 0, bytes.Length);
                memoryStream.Close();

            }
            outputArray.Clear();
        }

        public void UpdateVolume(int channel, float volume)
        {
            if(mixer is not null)
            {
                mixer.ChannelVolumes[channel] = volume;
            }
        }
    }

    class APUConstants
    {
        public static readonly uint SAMPLE_RATE = 44100;
        public static readonly short BITS_PER_SAMPLE = 16;
        public static readonly float BASE_VOLUME = 0.05f;
        public static readonly float AMPLITUDE_MAX = BASE_VOLUME * short.MaxValue;

        public static readonly float CPU_Hz = 1789773f;
        //each CPU cycle is about 40.5 samples long
        public static readonly float CPU_CLOCKS_PER_SAMPLE = 40.58442176870748f;
        //APU runs every other CPU cycle, so we get half the clocks
        public static readonly float APU_CLOCKS_PER_SAMPLE = 20.29221088435374f;
    }
}
