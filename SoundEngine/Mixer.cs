using CatfortSound.SoundEngine.DataTables;
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

using Channel = CatfortSound.SoundEngine.Channels.Channel;
using CatfortSound.SoundEngine.Channels;
using CatfortSound.SoundEngine.Effects;
using Avalonia.Metadata;

namespace CatfortSound.SoundEngine
{
    public class Mixer
    {
        // mixer volumes, independent of channels
        public float[] ChannelMixerVolumes = [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f];

        // dirty filter values
        private const float beta = 0.99f;
        private float gain => (1.0f + beta)/2.0f;

        float prev_output = 0;
        float prev_input = 0;

        // extra details for a dirty low pass, but probably not needed 

        /*
        float alpha => 0.3f;
        float prev_outputLo = 0;
        */

        // pre-allocated samples list
        private float[] samples = new float[5];

        public float GetSample(int index) => samples[index]; 

        public float[] GenerateMixBuffer(int sampleCount, in Channel[] channels)
        {
            float[] mixBuffer = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                //generate samples
                for(int j = 0; j < 5; j++)
                {
                    samples[j] = channels[j].GenerateSample() * ChannelMixerVolumes[j];
                }

                //mix samples
                float pulseOut = MakePusleOut(samples[0], samples[1]);
                float tndOut = MakeTNDOut(samples[2], samples[3], samples[4]);


                //filter mix
                float output = DirtyFilter((pulseOut + tndOut), ref prev_input, ref prev_output);

                //output to buffer
                mixBuffer[i] = output;
                //mixBuffer[i] = (pulseOut + tndOut);
            }

            return mixBuffer;
        }

        private float DirtyFilter(float input, ref float previousInput, ref float previousOutput)
        {
            float output = gain * (input - previousInput) + beta * previousOutput;
            previousInput = input;
            previousOutput = output;

            //extra lo pass? probably don't need
            //float outLo = alpha * output + (1 - alpha) * prev_outputLo;
            //prev_outputLo = outLo;

            return output;
        }

        public void SetChannelMixerVolume(int channel, float newVolume)
        {
            ChannelMixerVolumes[channel] = newVolume;
        }

        public void Reset()
        {
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

            return 95.88f / (8128f / pAdd + 100f);
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
