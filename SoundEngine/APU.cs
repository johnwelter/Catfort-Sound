using CatfortSound.SoundEngine.Banks;
using CatfortSound.SoundEngine.Channels;
using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using Ownaudio.Utilities.Extensions;
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
using static CatfortSound.SoundEngine.Effects.EffectStack;

namespace CatfortSound.SoundEngine
{
    public enum ChannelIndexes
    {
        SQUARE_1,
        SQUARE_2,
        TRIANGLE,
        NOISE,
        DPMC,
        //FDS
    }
    public enum Streams
    {
        MUSIC_SQ1,
        MUSIC_SQ2,
        MUSIC_TRI,
        MUSIC_NOI,
        MUSIC_DPMC,
        //MUSIC_FDS
    }

    public class APU
    {
        public static readonly uint SAMPLE_RATE = 48000;
        public static readonly short BITS_PER_SAMPLE = 16;
        public static readonly float BASE_VOLUME = 0.50f;
        public static readonly float AMPLITUDE_MAX = BASE_VOLUME * short.MaxValue;

        public static readonly float CPU_Hz = 1789773f;
        public static readonly float CPU_CLOCKS_PER_SAMPLE = 37.2869375f;
        public static readonly float APU_CLOCKS_PER_SAMPLE = 18.64346875f;

        // Overall master volume settings of APU and application
        private float m_volume = 1.0f;
        private float MasterVolume => m_volume * AMPLITUDE_MAX;

        // Mixer class for generating and mixing channel audio
        public Mixer? Mixer = new();

        // Wave Out device to send audio buffers to speakers
        private static woLib WaveOut = new();

        public Channel[] Channels = [new Square(DutyCycle.k125), new Square(DutyCycle.k125), new Triangle(), new Noise(), new DMC(), new FDS()];

        public EffectsBank? EffectsBank = new();

        public APU()
        {
            WaveOut.InitWODevice(SAMPLE_RATE, 1, (uint)BITS_PER_SAMPLE, false);
            DMCBank.InitDMCBanks();
            FDSBank.InitFDSBanks();
        }

        #region APU Update functions

        // Update NMI based properties, like effects
        public void FrameUpdate()
        {
            foreach (Channel c in Channels)
            {
                c.FrameUpdate();
            }
        }

        // Called every tick, generates audio and outputs it to speakers in real time
        public bool Update(double deltaTime)
        {
            int sampleCount = (int)(deltaTime / 1000f * SAMPLE_RATE);
            if (sampleCount == 0) { return false; }

            short[]? soundBuffer = GenerateSoundBuffer(sampleCount);
            if (soundBuffer is null) { return false; }

            OutputSoundBuffer(soundBuffer);
            return true;
        }

        // Reset the APU
        public void Reset()
        {
            foreach (Channel c in Channels)
            {
                c.Reset();
            }
            Mixer?.Reset();
        }

        public void SetMasterVolume(float volume)
        {
            m_volume = Math.Clamp(volume, 0, 1);
        }

        #endregion

        #region Sound Buffer functions

        // Generate a sound buffer for the desired sample count
        public short[]? GenerateSoundBuffer(int sampleCount)
        {
            // generate mixed float buffer
            float[]? mixBuffer = Mixer?.GenerateMixBuffer(sampleCount, in Channels);
            if (mixBuffer is null)
            {
                return null;
            }

            // make final short buffer with mastered volume   
            short[] outBuffer = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float masteredSample = mixBuffer[i] * MasterVolume;
                outBuffer[i] = Convert.ToInt16(Math.Min(masteredSample, short.MaxValue));
            }

            return outBuffer;
        }

        // Output a given buffer to the speakers
        public void OutputSoundBuffer(in short[] soundBuffer)
        {
            nint outWave = Marshal.AllocHGlobal(soundBuffer.Length * sizeof(short));
            Marshal.Copy(soundBuffer, 0, outWave, soundBuffer.Length);
            WaveOut.SendWODevice(outWave, (uint)(soundBuffer.Length * sizeof(short)));
            Marshal.FreeHGlobal(outWave);
        }

        // Export given buffer to a WAV file
        public void ExportBufferToWAV(short[] outBuffer)
        {
            int sampleCount = outBuffer.Length;
            byte[] binaryWave = new byte[sampleCount * sizeof(short)];

            Buffer.BlockCopy(outBuffer, 0, binaryWave, 0, sampleCount * sizeof(short));

            using (FileStream file = new FileStream("test.wav", FileMode.Create, FileAccess.Write))
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                short blockAlign = (short)(BITS_PER_SAMPLE / 8);
                int subChunkTwoSize = sampleCount * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' });
                binaryWriter.Write(36 + subChunkTwoSize);
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                binaryWriter.Write(16);
                binaryWriter.Write((short)1);
                binaryWriter.Write((short)1);
                binaryWriter.Write(SAMPLE_RATE);
                binaryWriter.Write((int)SAMPLE_RATE * blockAlign);
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

        #endregion

        #region Channel functions

        public void SetChannelVolume(float volume, int channel)
        {
            Channels[channel].SetChannelVolume(volume);
        }

        public void SetOscilatorPitch(int pitch, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if (oscilator is not null)
            {
                oscilator.SetPitch(pitch);
            }
        }

        public void SetOscilatorEffect(EffectSlots slot, Effect effect, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if (oscilator is not null)
            {
                oscilator.Effects.SetEffect(slot, effect);
            }
        }

        public void RemoveOscilatorEffect(EffectStack.EffectSlots slot, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if (oscilator is not null)
            {
                oscilator.Effects.ClearEffect(slot);
            }
        }

        public void TriggerDMC(int sample)
        {
            int dmcIndex = sample >> 4;
            int pitch = sample & 15;

            if (dmcIndex == (int)DMCSamples.kNone)
            {
                return;
            }

            DMC? dmc = Channels[(int)ChannelIndexes.DPMC] as DMC;
            dmc?.SetSample(dmcIndex, pitch);
        }

        #endregion
    }
}
