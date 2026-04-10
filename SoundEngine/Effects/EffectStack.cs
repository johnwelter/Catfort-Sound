using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Effects
{
    public class EffectStack
    {
        public enum EffectSlots
        {
            kVol = 0,
            kDuty = 1,
            kMod = 2,
            kArp = 3,
        }

        private Effect?[] stack = { null, null, null, null };

        public bool HasEffect(EffectSlots slot) => stack[(int)slot] != null;

        internal void SetEffect(EffectSlots slot, Effect effect)
        {
            stack[(int)slot] = effect;
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
            foreach (Effect? e in stack)
            {
                e?.TickEffect();
            }
        }

        public void ResetEffects()
        {
            foreach (Effect? e in stack)
            {
                e?.ResetEffect();
            }
        }

        public int GetEffectValue(EffectSlots slot) => stack[(int)slot]?.CurrentValue ?? 0;

    }

}
