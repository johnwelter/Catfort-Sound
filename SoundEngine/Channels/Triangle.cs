using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Channels
{

    class Triangle : Oscilator
    {
        public override float GetVolume()
        {
            float mult = 1f;
            if(Effects.HasEffect(EffectStack.EffectSlots.kVol))
            {
                mult = Effects.GetEffectValue(EffectStack.EffectSlots.kVol) > 0 ? 1 : 0; 
            }
            return mult;
        }

        int lutIndex = 0;
        private static readonly float[] triangleLut = { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
        protected override float GetWaveSample()
        {
            if (m_pitch != NoteConstants.Rest && GetVolume() != 0)
            {
                Clock(APU.CPU_CLOCKS_PER_SAMPLE, GetLengthTimer());
            }
            return CurrentSample;
        }

        public override float GenerateSample()
        {
            float sample = GetWaveSample();
            return sample;
        }

        public override void UpdateCurrentSample(int updateTicks)
        {
            lutIndex = (lutIndex + updateTicks) % 32;
            CurrentRawSample = triangleLut[lutIndex];
        }

        public override void Reset()
        {
            base.Reset();
            lutIndex = 0;
        }
    }

}
