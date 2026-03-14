using CatfortSound.SoundEngine.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Channels
{
    class Noise : Oscilator
    {
        int ShiftRegister = 1;
        bool ModeFlag = false;
        public override int GetLengthTimer() => Effects.HasEffect(EffectStack.EffectSlots.kMod) ? timerLut[(GetPitch() + (sbyte)Effects.GetEffectValue(EffectStack.EffectSlots.kMod))%16] : timerLut[GetPitch()%16];

        public Noise() : base()
        {
            m_pitch = -1;
        }

        public override void SetPitch(int pitch)
        {
            base.SetPitch(pitch);
            if(pitch >= 17)
            {
                m_pitch = -1;
            }
        }

        //originals are in CPU cycles - the noise oscilator runs on APU cycles, so we'll need to /2 
        private static readonly int[] timerLut =
        {
            //4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
              2, 4, 8, 16, 32, 48, 64, 80, 101, 126, 190, 254, 381, 508, 1017, 2034
        };

        public override void UpdateCurrentSample(int updateTicks)
        {
            for (int i = 0; i < updateTicks; i++)
            {
                int b0 = ShiftRegister & 1;
                int bXOR = ModeFlag ? ShiftRegister >> 6 & 1 : ShiftRegister >> 1 & 1;
                int feedback = (b0 ^ bXOR) << 14;
                ShiftRegister >>= 1;
                ShiftRegister |= feedback;
            }


            CurrentRawSample =  (ShiftRegister & 1) == 0 ? 0 : 1f;
        }
        public void SetModeFlag(bool newModeFlag)
        {
            ModeFlag = newModeFlag;
        }

        public override void Reset()
        {
            base.Reset();
            m_pitch = -1;
            ShiftRegister = 1;
            ModeFlag = false;
        }
    }
}
