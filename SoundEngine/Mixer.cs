using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Mixer
    {
        public const int SQUARE_1 = 0;
        public const int SQUARE_2 = 1;
        public const int TRIANGLE = 2;
        public const int NOISE = 3;
        public const int DMC = 4;
        public const int FDS = 5;

        public Channel[] Channels = [new Square(DutyCycle.k25), new Square(DutyCycle.k50), new Triangle(), new Noise(), new DMC()];
        public float[] ChannelVolumes = [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f];

        public void SetChannelVolume(float volume, int channel)
        {
            Channels[channel].SetChannelVolume(volume);
        }

        public void SetOscilatorPitch(int pitch, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if(oscilator is not null)
            {
                oscilator.SetPitch(pitch);
            }
        }

        public void SetOscilatorEffect(Effect effect, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if (oscilator is not null)
            {
                oscilator.Effects.SetEffect(effect);
            }
        }

        public void RemoveOscilatorEffect(EffectStack.EffectSlots slot, int channel)
        {
            Oscilator? oscilator = Channels[channel] as Oscilator;
            if (oscilator is not null)
            {
                oscilator.Effects.ClearEffect(slot);
            }
        }

        //called every 1/60th of a second
        public void FrameTick()
        {
            foreach(Channel c in Channels)
            {
                c.FrameTick();
            }
        }

        public float[] GenerateMixBuffer(int samplesThisFrame)
        {

            float[] mixBuffer = new float[samplesThisFrame];

            for (int i = 0; i < samplesThisFrame; i++)
            {
                float square1 = Channels[SQUARE_1].GenerateSample() * ChannelVolumes[SQUARE_1];
                float square2 = Channels[SQUARE_2].GenerateSample() * ChannelVolumes[SQUARE_2];
                float triangle = Channels[TRIANGLE].GenerateSample() * ChannelVolumes[TRIANGLE];
                float noise = Channels[NOISE].GenerateSample() * ChannelVolumes[NOISE];
                float dmc = Channels[DMC].GenerateSample() * ChannelVolumes[DMC];
                //float fds = Channels[FDS].GenerateSample() * ChannelVolumes[FDS];

                float pulseOut = 0.00752f * (square1 + square2);

                float tndOut = 0.00851f * triangle + 0.00494f * noise + 0.00335f * dmc;

                mixBuffer[i] = pulseOut + tndOut;
            }

            return mixBuffer;
        }

        internal void TriggerDMC(int sample)
        {
            int dmcIndex = sample >> 4;
            int pitch = sample & 15;

            DMC? dmc = Channels[DMC] as DMC;
            dmc?.SetSample(dmcIndex, pitch);
        }
    }
}
