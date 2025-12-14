using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Triangle : Oscilator
    {
        double currentSample = 0;
        double ampStepDirection = 1;

        //a bit of a magic number, but 15 wasn't cutting it.
        public override float GetVolume()
        {
            float mult = 1f;
            if(Effects.HasEffect(EffectStack.EffectSlots.kVol))
            {
                mult = Effects.GetEffectValue(EffectStack.EffectSlots.kVol) > 0 ? 1 : 0; 
            }
            return 45 * mult;
        }

        int lutIndex = 0;
        private static readonly float[] triangleLut = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
        protected override float GetWaveSample()
        {
            if (GetLengthTimer() == 0)
            {
                return CurrentSample;
            }
            Clock(APUConstants.CPU_CLOCKS_PER_SAMPLE, GetLengthTimer());
            return CurrentSample;
        }

        public override void UpdateCurrentSample(int updateTicks)
        {
            lutIndex = (lutIndex + updateTicks) % 32;
            CurrentSample = ((triangleLut[lutIndex] / 15f) - 0.5f) * 3f;
        }
    }

}
