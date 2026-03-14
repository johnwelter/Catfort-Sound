using CatfortSound.SoundEngine.Effects;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Banks
{
    public class EffectsBank
    {
        public Dictionary<Type, List<byte[]>> Banks;

        #region Default Effects
        public List<byte[]> VolEffects = new()
        {
            //new byte[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0 },
            new byte[] { 15, 14, 13, 12, 9, 5, 0},
            new byte[] { 15, 15, 15, 11, 11, 11, 7, 7, 7, 5, 5, 5 },
            new byte[] { 15, 9, 6, 3, 0 },
            new byte[] { 10, 11, 12, 13, 14, 15, 0, 0, 10, 10, 10, 10, 10, 0, 0, 5, 5, 5, 5, 0, 0, 2, 2, 1, 1, 1, 0},
            new byte[] { 15 },
            new byte[] { 15, 9, 8, 6, 4, 3, 2, 1, 0},
            new byte[] { 0 },
            new byte[] {15, 15, 15, 15, 15, 14, 14, 14, 0},
            new byte[] { 15, 15, 9, 9, 8, 8, 6, 6, 4, 4, 3, 3, 2, 2, 1, 1, 0},
            new byte[] { 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 0},
            new byte[] { 14, 6, 2, 0 },
            new byte[] { 0, 2, 4, 8, 12, 14, 15, 15, 15, 15, 15, 14, 8},
            new byte[] { 0, 4, 5, 5, 5, 5, 5, 4, 4, 4, 4, 4, 2},
        };

        public List<byte[]> ModEffects = new()
        {
            new byte[] { 0x00, ModEffect.LOOP_ALL},
            new byte[] { 0x00, ModEffect.START_DELAY, 0x16, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10},
            new byte[] { 0x20, 0x1C, 0x18, 0x10, 0x0C, 0x08, 0x04, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10},
            new byte[] { 0x00, ModEffect.START_DELAY, 0x10, 0xF0, 0xF0, 0xF0, 0xE0, 0xE0, 0xE0, 0xD0, 0xD0, 0xD0, ModEffect.LOOP_LAST },
            new byte[] { 0x00, ModEffect.START_DELAY, 0x10, 0x10, 0x10, 0x10, 0x20, 0x20, 0x20, 0x30, 0x30, 0x30, ModEffect.LOOP_LAST },
            new byte[] { 0x10, 0x0C, 0x08, 0x04, 0x00, ModEffect.LOOP_LAST},
            new byte[] { 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00, 0x10, 0x20, 0x30, 0x40, 0x40, 0x60, 0x70, ModEffect.LOOP_LAST},
            new byte[] { 0x01, ModEffect.LOOP_ALL},
            new byte[] { 0x02, ModEffect.LOOP_ALL},
            new byte[] { 0xFF, ModEffect.LOOP_ALL},
            new byte[] { 0xFE, ModEffect.LOOP_ALL},
            new byte[] { 0x01, 0x00, ModEffect.START_DELAY, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, ModEffect.LOOP_PART, 0x0C},
            new byte[] { 0x0C, 0x0A, 0x08, 0x06, 0x04, 0x02, 0x00, ModEffect.START_DELAY, 0x0A, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, ModEffect.LOOP_PART, 0x0C }
        };

        public List<byte[]> ArpEffects = new()
        {
            new byte[] { 0x00, 0x03, 0x07, 0x0A, 0xFF},
        };

        public List<byte[]> DutyEffects = new()
        {
            new byte[] { 0 },
            new byte[] { 1 },
            new byte[] { 2 },
            new byte[] { 3 },
        };
        #endregion
        public EffectsBank()
        {
            Banks = [];
            Banks.Add(typeof(VolEffect), VolEffects);
            Banks.Add(typeof(ModEffect), ModEffects);
            Banks.Add(typeof(ArpEffect), ArpEffects);
            Banks.Add(typeof(DutyEffect), DutyEffects);
        }

        public byte[] GetEffectByType(Type type, int idx)
        {
            return Banks[type][idx];
        }
    }
}
