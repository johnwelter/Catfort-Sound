using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Oscilator : Channel
    {
        protected int m_pitch = Notes.rest;

        public EffectStack Effects = new EffectStack();

        public virtual float GetVolume() => Effects.HasEffect(EffectStack.EffectSlots.kVol) ? Effects.GetEffectValue(EffectStack.EffectSlots.kVol) : m_channelVolume;
        public virtual int GetPitch() => Effects.HasEffect(EffectStack.EffectSlots.kArp) ? m_pitch + Effects.GetEffectValue(EffectStack.EffectSlots.kArp) : m_pitch;
        public virtual int GetLengthTimer() => Effects.HasEffect(EffectStack.EffectSlots.kMod) ? (int)NoteTables.NoteTable[GetPitch()] + (sbyte)Effects.GetEffectValue(EffectStack.EffectSlots.kMod): (int)NoteTables.NoteTable[GetPitch()];
        //public virtual float GetVolume() => 15f;

        public virtual void SetPitch(int pitch)
        {
            m_pitch = pitch;
            Effects.ResetEffects();
        }

        public override void FrameTick()
        {
            base.FrameTick();
            Effects.TickEffects();
        }

        public override float GenerateSample()
        {
            float sample = GetWaveSample() * GetVolume();
            return sample;
        }

        protected virtual float GetWaveSample()
        {
            if (GetLengthTimer() == 0)
            {
                return 0;
            }
            Clock(APUConstants.APU_CLOCKS_PER_SAMPLE, GetLengthTimer());
            return CurrentSample;
        }

    }
}
