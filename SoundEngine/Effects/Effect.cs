using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Effects
{
    public enum LoopTypes : byte
    {
        Last = 0xFF,
        All = 0xFE,
        Part = 0xFD,
        Hold = 0xFC,
        cLast = 0x80,
        cAll = 0x81,
        cPart = 0x82,
        cHold = 0x83
    }

    public class Effect
    {
        protected bool centered = false;
        public static byte LOOP_LAST(bool centered) => centered ? (byte)LoopTypes.cLast : (byte)LoopTypes.Last;
        public static byte LOOP_ALL(bool centered) => centered ? (byte)LoopTypes.cAll : (byte)LoopTypes.All;
        public static byte LOOP_PART(bool centered) => centered ? (byte)LoopTypes.cPart: (byte)LoopTypes.Part;
        public static byte HOLD_CURRENT(bool centered) => centered ? (byte)LoopTypes.cHold : (byte)LoopTypes.Hold;

        public static bool IsLoopType(byte val, bool centered)
        {
            if(Enum.IsDefined(typeof(LoopTypes), val))
            {
                int idx = Array.IndexOf(Enum.GetValues<LoopTypes>(), (LoopTypes)val);
                // enums are indexed by order of value, not definition
                return centered ? idx < 4 : idx > 3;  
            }
            return false;
        }

        private int holdtimer = 0;

        protected int m_ticks = -1;
        protected byte[]? m_effectBytes;
        protected int GetEffectValue(int idx) => (sbyte)(m_effectBytes?[idx] ?? (byte)0);
        protected int effectLength => m_effectBytes?.Length ?? 0;

        public int CurrentValue = 0;

        public void IncTicks(int ticks = 1) => m_ticks = Math.Clamp(m_ticks + ticks, 0, effectLength - 1);
        
        public Effect() { }

        public Effect(byte[] effectBytes, bool centered)
        {
            SetEffectBytes(effectBytes);
            this.centered = centered;   
        }

        public void SetEffectBytes(byte[] effectBytes)
        {
            m_effectBytes = effectBytes;
        }

        public virtual void ResetEffect()
        {
            m_ticks = 0;
            CurrentValue = GetEffectValue(0);
            holdtimer = 0;
        }

        public void SetEffectTicks(int ticks)
        {
            m_ticks = ticks;
        }

        //should be ticked once every 1/60th of a second
        public virtual void TickEffect()
        {
            if (holdtimer != 0)
            {
                holdtimer--;
                return;
            }

            IncTicks();

            bool processedTick = false;
            while (!processedTick)
            {
                byte val = (byte)GetEffectValue(m_ticks);

                if(val == LOOP_LAST(centered))
                {
                    IncTicks(-1);
                }
                else if(val == LOOP_ALL(centered))
                {
                    m_ticks = 0;
                }
                else if(val == LOOP_ALL(centered))
                {
                    IncTicks();
                    m_ticks -= GetEffectValue(m_ticks);
                }
                else if(val == LOOP_ALL(centered))
                {
                    IncTicks();
                    holdtimer = GetEffectValue(m_ticks);
                    processedTick = true;
                }
                else
                {
                    CurrentValue = val;
                    processedTick = true;
                }
            }
        }
    }
}
