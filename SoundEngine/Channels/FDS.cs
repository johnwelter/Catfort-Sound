using CatfortSound.SoundEngine.Banks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Channels
{
    class FDS : Channel
    {
        FDSSamples LoadedSample = FDSSamples.kNNMJ_Shamisen;

        private int byteIndex = 0;


        public FDS()
        {
        }
        public virtual void SetSample(FDSSamples sample)
        {
            //m_pitch = pitch;
            //Effects.ResetEffects();
            //timer = 0;
            byteIndex = 0;
            LoadedSample = sample;
            CurrentSample = 0;
        }

        public override float GenerateSample()
        {

            //for a given sample - get the current bit from the current byte
            //inc sample if 1, dec if 0
            //clamp between 127 and -127
            if (byteIndex == -1)
            {
                return 0;
            }
            Clock(APU.CPU_CLOCKS_PER_SAMPLE, 0x080);
            return CurrentSample * 10f;
        }

        public override void UpdateCurrentSample(int updateTicks)
        {
            byte[] dmc = FDSBank.SampleList[(int)LoadedSample];

            byteIndex += updateTicks;
            if (byteIndex >= dmc.Length)
            {
                byteIndex = byteIndex % dmc.Length + 1;
            }

            CurrentRawSample = dmc[byteIndex];


        }

    }
}
