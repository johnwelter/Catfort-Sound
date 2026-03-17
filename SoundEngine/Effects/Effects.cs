using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace CatfortSound.SoundEngine.Effects
{

    public class EffectStack
    {

        public enum EffectSlots
        {
            kVol = 0,
            kMod = 1,
            kArp = 2,
            kDuty = 3
        }

        private Effect?[] stack = { null, null, null, null };
        
        public bool HasEffect(EffectSlots slot) => stack[(int)slot] != null;

        internal void SetEffect(Effect effect)
        {

            int effectSlot = effect switch
            {
                VolEffect => (int)EffectSlots.kVol,
                ModEffect => (int)EffectSlots.kMod,
                ArpEffect => (int)EffectSlots.kArp,
                DutyEffect => (int)EffectSlots.kDuty,
                _ => (int)EffectSlots.kVol

            };

            stack[effectSlot] = effect;
        }
        
        public void ClearEffect(EffectSlots slot)
        {
            stack[(int)slot] = null;
        }

        public void ClearAllEffect()
        {
            for (int i = 0; i < stack.Length; i++)
            {
                stack[i] = null;
            }
        }

        public void TickEffects()
        {
            foreach(Effect? e in stack)
            {
                e?.TickEffect();
            }
        }

        public void ResetEffects()
        {
            foreach(Effect? e in stack)
            {
                e?.ResetEffect();
            }
        }

        public int GetEffectValue(EffectSlots slot) => stack[(int)slot]?.CurrentValue ?? 0;

    }

    public class Effect
    {
        protected int m_ticks = -1;
        protected byte[]? m_effectBytes;
        protected int GetEffectValue(int idx) => (sbyte)(m_effectBytes?[idx] ?? (byte)0);
        protected int effectLength => m_effectBytes?.Length ?? 0;

        public int CurrentValue = 0;

        public void IncTicks(int ticks = 1) => m_ticks = Math.Clamp(m_ticks + ticks, 0, effectLength - 1);
        
        public Effect() { }

        public Effect(byte[] effectBytes)
        {
            SetEffectBytes(effectBytes);
        }

        public void SetEffectBytes(byte[] effectBytes)
        {
            m_effectBytes = effectBytes;
        }

        public virtual void ResetEffect()
        {
            m_ticks = 0;
            CurrentValue = GetEffectValue(0);
        }

        public void SetEffectTicks(int ticks)
        {
            m_ticks = ticks;
        }

        //should be ticked once every 1/60th of a second
        public virtual void TickEffect()
        {
            IncTicks();
        }
    }

    public class VolEffect : Effect
    {
        public VolEffect():base()
        {

        }

        public VolEffect(byte[] effectBytes) : base(effectBytes)
        {

        }

        public override void TickEffect()
        {
            base.TickEffect();
            int val = GetEffectValue(m_ticks);
            if ((byte)val == 0xFF)
            {
                IncTicks(-1);
            }
            CurrentValue = GetEffectValue(m_ticks);
            Debug.WriteLine(CurrentValue);
        }
    }

    public class ArpEffect : Effect
    {
        public ArpEffect():base()
        {

        }
        public ArpEffect(byte[] effectBytes) : base(effectBytes)
        {
        }
        public override void TickEffect()
        {
            base.TickEffect();
            int val = GetEffectValue(m_ticks);
            if ((byte)val == 0x80)
            {
                m_ticks = 0;
            }
            CurrentValue = GetEffectValue(m_ticks);
        }
    }

    public class ModEffect : Effect
    {
        public const int LOOP_LAST = 0x80;
        public const int LOOP_ALL = 0x81;
        public const int LOOP_PART = 0x82;
        public const int HOLD_CURRENT = 0x83;

        private int delayTimer = 0;

        public ModEffect():base()
        {

        }
        public ModEffect(byte[] effectBytes) : base(effectBytes)
        {
        }

        public override void TickEffect()
        {
            if (delayTimer != 0)
            {
                delayTimer--;
                return;
            }

            base.TickEffect();

            bool processedTick = false;
            while (!processedTick)
            {
                int val = GetEffectValue(m_ticks);

                switch ((byte)val)
                {
                    case LOOP_LAST:
                        IncTicks(-1);
                        break;
                    case LOOP_ALL:
                        m_ticks = 0;
                        break;
                    case LOOP_PART:
                        IncTicks();
                        m_ticks -= GetEffectValue(m_ticks);
                        break;
                    case HOLD_CURRENT:
                        IncTicks();
                        delayTimer = GetEffectValue(m_ticks);
                        processedTick = true;
                        break;
                    default:
                        CurrentValue = val;
                        processedTick = true;
                        break;

                }
            }
        }

        public override void ResetEffect()
        {
            base.ResetEffect();
            delayTimer = 0;
        }
    }

    public class DutyEffect : Effect
    {
        public DutyEffect():base()
        {

        }
        public DutyEffect(byte[] effectBytes) : base(effectBytes)
        {
        }
        public override void TickEffect()
        {
            base.TickEffect();
            int val = GetEffectValue(m_ticks);
            if ((byte)val == 0xFF)
            {
                IncTicks(-1);
            }
            CurrentValue = GetEffectValue(m_ticks);
        }
    }
}
