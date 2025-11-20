using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Noise : Oscilator
    {
        int ShiftRegister = 1;
        bool ModeFlag = false;

        public override void UpdateCurrentSample(int updateTicks)
        {
            for (int i = 0; i < updateTicks; i++)
            {
                int b0 = ShiftRegister & 1;
                int bXOR = ModeFlag ? (ShiftRegister >> 6) & 1 : (ShiftRegister >> 1) & 1;
                int feedback = (b0 ^ bXOR) << 14;
                ShiftRegister >>= 1;
                ShiftRegister |= feedback;
            }

            CurrentSample = ((((float)ShiftRegister) / short.MaxValue) - 0.5f) * 2.5f;
        }
    }



}
