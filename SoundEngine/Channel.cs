using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    public class Channel
    {
        protected float m_channelVolume = 15;

        protected float fTimer = 0;

        protected float CurrentSample;


        public virtual float GenerateSample() 
        {
            return 0;
        }

        public void SetChannelVolume(float newVolume)
        {
            m_channelVolume = Math.Clamp(newVolume, 0, 15);
        }

        public virtual void Clock(float clocks, int lengthTimer)
        {
            fTimer += clocks;
            int truncTimer = (int)fTimer;
            int limitTime = lengthTimer + 1;
            int ticks = 0;
            float ramp = fTimer / limitTime;

            if (truncTimer > limitTime)
            {
                ticks = truncTimer / limitTime;
                fTimer = fTimer - (limitTime * ticks);
                ramp = fTimer / limitTime;

            }
            UpdateCurrentSample(ticks, ramp);
        }

        public virtual void UpdateCurrentSample(int updateTicks, float ramp) { }

        public virtual void FrameTick() { }

        public virtual void Reset()
        {
            m_channelVolume = 15;
            fTimer = 0;
            CurrentSample = 0;
        }
    }
}
