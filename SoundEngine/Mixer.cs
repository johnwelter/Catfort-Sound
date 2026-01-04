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

        public Channel[] Channels = [new Square(DutyCycle.k25), new Square(DutyCycle.k50), new Triangle(), new Noise(), new DMC(), new FDS()];
        public float[] ChannelVolumes = [1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f];

        private float prevSq1;
        private float prevSq2;
        private float prevTri;
        private float prevNoi;
        private float prevDMC;

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

                float avgP1 = (prevSq1 + square1) / 2f;
                float avgp2 = (prevSq2 + square2) / 2f;
                float avgTri = (prevTri + triangle) / 2f;
                float avgNoi = (prevNoi + noise) / 2f;
                float avgDMC = (prevDMC + dmc) / 2f;

                float pulseOut = MakePusleOut(avgP1, avgp2);
                float tndOut = MakeTNDOut(avgTri, avgNoi, avgDMC);

                mixBuffer[i] = pulseOut + tndOut;

                prevSq1 = square1;
                prevSq2 = square2;
                prevTri = triangle;
                prevNoi = noise;
                prevDMC = dmc;
            }

            return mixBuffer;
        }

        internal void TriggerDMC(int sample)
        {
            int dmcIndex = sample >> 4;
            int pitch = sample & 15;

            if((byte)sample == 0xFF)
            {
                dmcIndex = -1;
            }

            DMC? dmc = Channels[DMC] as DMC;
            dmc?.SetSample(dmcIndex, pitch);
        }

        public void ResetChannels()
        {
            foreach(Channel c in Channels)
            {
                c.Reset();
            }
            prevSq1 = 0;
            prevSq2 = 0;
            prevTri = 0;
            prevNoi = 0;
            prevDMC = 0;
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
