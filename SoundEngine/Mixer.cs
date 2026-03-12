using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    class Mixer
    {
        public Channel[] Channels = [new Square(DutyCycle.k25), new Square(DutyCycle.k50), new Triangle(), new Noise(), new DMC(), new FDS()];
        public float[] ChannelVolumes = [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f];


        float beta => 0.99f;
        float gain => (1.0f + beta)/2.0f;

        float alpha => 0.3f;

        float prev_outputLo = 0;
        float prev_output = 0;
        float prev_input = 0;


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
                //we'll want to recenter these channel by channel
                float square1 = Channels[(int)ChannelIndexes.SQUARE_1].GenerateSample() * ChannelVolumes[(int)ChannelIndexes.SQUARE_1];
                float square2 = Channels[(int)ChannelIndexes.SQUARE_2].GenerateSample() * ChannelVolumes[(int)ChannelIndexes.SQUARE_2];
                float triangle = Channels[(int)ChannelIndexes.TRIANGLE].GenerateSample() * ChannelVolumes[(int)ChannelIndexes.TRIANGLE];
                float noise = Channels[(int)ChannelIndexes.NOISE].GenerateSample() * ChannelVolumes[(int)ChannelIndexes.NOISE];
                float dmc = Channels[(int)ChannelIndexes.DMC].GenerateSample() * ChannelVolumes[(int)ChannelIndexes.DMC];
                //float fds = Channels[FDS].GenerateSample() * ChannelVolumes[FDS];

                float pulseOut = MakePusleOut(square1, square2);
                float tndOut = MakeTNDOut(triangle, noise, dmc);

                //value sould be some number between 0 and 1 - so we can recenter it

                float mix = pulseOut + tndOut;

                float output = gain * (mix - prev_input) + beta * prev_output;

                prev_input = mix;
                prev_output = output;

                float outLo = alpha * output + (1 - alpha) * prev_outputLo;
                prev_outputLo = outLo;

                mixBuffer[i] = output;
                //mixBuffer[i] = mix;
            }

            return mixBuffer;
        }



        internal void TriggerDMC(int sample)
        {
            int dmcIndex = sample >> 4;
            int pitch = sample & 15;

            if((byte)sample == 0xFF)
            {
                //dmcIndex = -1;
                return;
            }

            DMC? dmc = Channels[(int)ChannelIndexes.DMC] as DMC;
            dmc?.SetSample(dmcIndex, pitch);
        }

        public void ResetChannels()
        {
            foreach(Channel c in Channels)
            {
                c.Reset();
            }
            prev_input = 0;
            prev_output = 0;
        }

        public float MakePusleOut(float p1, float p2)
        {
            float pAdd = p1 + p2;
            if(pAdd == 0)
            {
                return 0;
            }

            return 95.88f / ((8128f / pAdd) + 100f);
        }

        public float MakeTNDOut(float t, float n, float d)
        {
            if(t == 0 && n == 0 && d == 0)
            {
                return 0;
            }

            float tA = t / 8227f;
            float nA = n / 12241f;
            float dA = d / 22638f;
            float tndAtten = 1f / (tA + nA + dA);

            return 159.79f / (tndAtten + 100f);

        }

    }
}
