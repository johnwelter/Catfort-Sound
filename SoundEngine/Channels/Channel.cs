using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Channels
{
    public class Channel
    {
        protected float m_channelVolume = 15;

        protected float fTimer = 0;
        protected int timer = 0;
        protected float clockOverflow= 0;

        protected float CurrentSample;
        protected float CurrentRawSample;


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

            //find out how many samples we'll need to average
            clockOverflow += clocks - (int)clocks;
            int extraClock = 0;
            if(clockOverflow > 1)
            {
                extraClock = 1;
                clockOverflow -= 1;
            }
            int samples = (int)clocks + extraClock;
            float totalSample = 0;
            int limitTime = lengthTimer + 1;

            for(int i = 0; i < samples; i++)
            {
                timer++;
                if(timer >= limitTime)
                {
                    UpdateCurrentSample(1);
                    timer -= limitTime;
                }
                totalSample += CurrentRawSample;
            }

            CurrentSample = totalSample / samples;
            
            //fTimer += clocks;
            //int truncTimer = (int)fTimer;
            //int ticks = 0;
            //float ramp = fTimer / limitTime;

            //if (truncTimer > limitTime)
            //{
            //    ticks = truncTimer / limitTime;
            //    fTimer = fTimer - (limitTime * ticks);
            //    ramp = fTimer / limitTime;

            //}
            //UpdateCurrentSample(ticks);
        }

        public virtual void UpdateCurrentSample(int updateTicks) { }

        public virtual void FrameUpdate() { }

        public virtual void Reset()
        {
            m_channelVolume = 15;
            fTimer = 0;
            timer = 0;
            CurrentSample = 0;
        }
    }
}
