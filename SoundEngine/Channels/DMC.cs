using CatfortSound.SoundEngine.Banks;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Channels
{
    public class DMC : Channel
    {

        int LoadedSample = -1;

        private int byteIndex = -1;
        private int bitIndex = 0;

        private int m_samplePitch = 0xB;

        public readonly int[] timerLut =
        {
            428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
        };

        public DMC()
        {
        }
        public virtual void SetSample(int sample, int pitch)
        {
            byteIndex = 0;
            bitIndex = 0;
            LoadedSample = sample;
            m_samplePitch = pitch;
            CurrentSample = 0;
            CurrentRawSample = 63;
        }

        public override float GenerateSample()
        {
            if(byteIndex != -1 && LoadedSample != -1)
            {
                Clock(APU.CPU_CLOCKS_PER_SAMPLE, timerLut[m_samplePitch]);
            }
            return CurrentSample;
        }

        public override void UpdateCurrentSample(int updateTicks)
        {
            if(updateTicks == 0)
            {
                return;
            }

            byte[] dmc = DMCBank.SampleList[LoadedSample];

            int step = (dmc[byteIndex] >> bitIndex & 1) == 1 ? 2 : -2;

            bitIndex += updateTicks;
            if (bitIndex >= 8)
            {
                bitIndex %= 8;
                byteIndex++;
                if (byteIndex >= dmc.Length)
                {
                    byteIndex = -1;
                }
            }

            float NewSample = CurrentSample + step;
            CurrentRawSample = NewSample < 0  || NewSample > 127 ? CurrentSample : NewSample; 
        }

        public override void Reset()
        {
            base.Reset();
            LoadedSample = -1;
            byteIndex = -1;
            bitIndex = 0;
            m_samplePitch = 0xB;
        }
    }
}
