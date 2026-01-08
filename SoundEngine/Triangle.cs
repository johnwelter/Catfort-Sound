using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{

    class Triangle : Oscilator
    {
        //We'll make a pseudo filter for the triangle to keep popping to a minumum when cutting off the channel - 
        //Once I understand fourier then I might be able to make some real filters for the whole system

        //filter accel rate per sample gen
        float PseudoFilterStep = 0.01f;
        float PseudoFilterMult = 0.0f;


        //a bit of a magic number, but 15 wasn't cutting it.
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
            if (m_pitch == NoteClass.rest || GetVolume() == 0)
            {
                PseudoFilterMult = Math.Max(PseudoFilterMult - PseudoFilterStep, 0);
                return CurrentSample * PseudoFilterMult;
            }
            Clock(APUConstants.CPU_CLOCKS_PER_SAMPLE, GetLengthTimer());

            PseudoFilterMult = Math.Min(PseudoFilterMult + PseudoFilterStep, 1);
            return CurrentSample * PseudoFilterMult;
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
