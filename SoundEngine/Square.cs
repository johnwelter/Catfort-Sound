using Ownaudio.Sources.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace CatfortSound.SoundEngine
{
    public enum DutyCycle
    {
        k125 = 0,
        k25 = 1,
        k50 = 2,
        k75 = 3
    }
    class Square : Oscilator
    {

        public static readonly float duty125 = 0.125f;
        public static readonly float duty25 = 0.25f;
        public static readonly float duty50 = 0.25f;
        public static readonly float duty75 = 0.75f;

        private int lutIndex = 0;

        private static readonly int[,] dutyLut =
        {
            {0, 0, 0, 0, 0, 0 ,0, 1 },
            {0, 0, 0, 0, 0, 0, 1, 1 },
            {0, 0, 0, 0, 1, 1, 1, 1 },
            {1, 1, 1, 1, 1, 1, 0, 0 }
        };

        public DutyCycle cycle = SoundEngine.DutyCycle.k50;
        public int GetDutyCycle() => Effects.HasEffect(EffectStack.EffectSlots.kDuty)? Effects.GetEffectValue(EffectStack.EffectSlots.kDuty) : (int)cycle;
       
        public Square(DutyCycle cycle)
        {
            this.cycle = cycle;
        }

        public override void UpdateCurrentSample(int updateTicks, float ramp)
        {
            lutIndex = betterMod(lutIndex - updateTicks, 8);
            CurrentSample = dutyLut[GetDutyCycle(), lutIndex];
        }

        int betterMod(int val, int mod)
        {
            return (val % mod + mod) % mod;
        }

        public override void Reset()
        {
            base.Reset();
            lutIndex = 0;
        }

    }
}
