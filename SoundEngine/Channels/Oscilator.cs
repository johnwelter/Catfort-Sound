using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Notes = CatfortSound.SoundEngine.DataTables.Notes;

namespace CatfortSound.SoundEngine.Channels
{
    class Oscilator : Channel
    {
        protected int m_pitch;

        public EffectStack Effects = new EffectStack();

        public virtual float GetVolume() => Effects.HasEffect(EffectStack.EffectSlots.kVol) ? Effects.GetEffectValue(EffectStack.EffectSlots.kVol) : m_channelVolume;
        public virtual int GetPitch() => Effects.HasEffect(EffectStack.EffectSlots.kArp) ? m_pitch + Effects.GetEffectValue(EffectStack.EffectSlots.kArp) : m_pitch;
        public virtual int GetLengthTimer() => Effects.HasEffect(EffectStack.EffectSlots.kMod) ? (int)NoteConstants.FreqTable[GetPitch()] + (sbyte)Effects.GetEffectValue(EffectStack.EffectSlots.kMod): (int)NoteConstants.FreqTable[GetPitch()];
        //public virtual float GetVolume() => 15f;

        public Oscilator() : base()
        {
            m_pitch = NoteConstants.Rest;
        }

        public virtual void SetPitch(int pitch)
        {
            m_pitch = pitch;
            Effects.ResetEffects();
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            Effects.TickEffects();
        }

        public override float GenerateSample()
        {
            if(m_pitch == -1 || m_pitch == NoteConstants.Rest)
            {
                return 0;
            }
            float sample = GetWaveSample() * GetVolume();
            return sample;
        }

        protected virtual float GetWaveSample()
        {
            if (GetLengthTimer() == 0)
            {
                return 0;
            }
            Clock(APU.APU_CLOCKS_PER_SAMPLE, GetLengthTimer());
            return CurrentSample;
        }

        public override void Reset()
        {
            base.Reset();
            m_pitch = NoteConstants.Rest;
            Effects.ClearAllEffect();
        }

    }
}
