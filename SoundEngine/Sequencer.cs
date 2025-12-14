using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine
{
    public static class Len
    {
        public const byte l32 = 0x80;
        public const byte l16 = 0x81;
        public const byte l8 = 0x82;
        public const byte l4 = 0x83;
        public const byte l2 = 0x84;
        public const byte l1 = 0x85;
        public const byte d16 = 0x86;
        public const byte d8 = 0x87;
        public const byte d4 = 0x88;
        public const byte d2 = 0x89;
        public const byte d1 = 0x8A;

        public static byte GetTicks(byte idx) => LenTable[idx - 0x80];

        public static readonly byte[] LenTable =
        {
            1, 2, 4, 8, 16, 32, (1 + 2), (2 + 4), (4 + 8), (8 + 16), (16 + 32)
        };
    }

    static class Ins
    {
        //notes = 00 - 7F
        //tick lenghts = 80 - 9F (maybe we should adjust to extend this? we're kinda limited)
        //instructions = A0 - AF (maybe even B0 - FF, but we only really have up to AA ... AB when we implement duty effects in the engine

        public const byte End = 0xA0;
        public const byte Loop = 0xA1;
        public const byte VolEffect = 0xA2; //in engine, effects are technically always on - we just have single byte "none" options!
        public const byte DutyEffect = 0xA3; // for full channel duty shifts, not effects *in engine* - will fix later, but will program it as effects in here
        public const byte SetLoop1_Counter = 0xA4;
        public const byte Loop1 = 0xA5;
        public const byte SetNoteOffset = 0xA7;
        public const byte Transpose = 0xA8;
        public const byte ModEffect = 0xA9;
        public const byte ArpEffect = 0xAA;
    }

    public class Sequencer
    {
        //effect table
        public List<byte[]> VolEffects = new()
        {
            new byte[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0 },
            new byte[] { 15, 15, 15, 11, 11, 11, 7, 7, 7, 5, 5, 5 },
            new byte[] { 15, 6, 2, 0 },
            new byte[] { 10, 11, 12, 13, 14, 15, 0, 0, 10, 10, 10, 10, 10, 0, 0, 5, 5, 5, 5, 0, 0, 2, 2, 1, 1, 1, 0},
            new byte[] { 15 },
            new byte[] { 15, 9, 8, 6, 4, 3, 2, 1, 0}
        };

        public List<byte[]> ModEffects = new()
        {
            new byte[] { 0x00, ModEffect.LOOP_ALL},
            new byte[] { 0x00, ModEffect.START_DELAY, 0x16, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10},
            new byte[] { 0x20, 0x1C, 0x18, 0x10, 0x0C, 0x08, 0x04, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10},
            new byte[] { 0x00, ModEffect.START_DELAY, 0x10, 0xF0, 0xF0, 0xF0, 0xE0, 0xE0, 0xE0, 0xD0, 0xD0, 0xD0, ModEffect.LOOP_LAST },
            new byte[] { 0x00, ModEffect.START_DELAY, 0x10, 0x10, 0x10, 0x10, 0x20, 0x20, 0x20, 0x30, 0x30, 0x30, ModEffect.LOOP_LAST },
            new byte[] { 0x10, 0x0C, 0x08, 0x04, 0x00, ModEffect.LOOP_LAST},
            new byte[] { 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00, 0x10, 0x20, 0x30, 0x40, 0x40, 0x60, 0x70, ModEffect.LOOP_LAST}

        };

        public List<byte[]> ArpEffects = new()
        {
            new byte[] { 0x00, 0x03, 0x07, 0x0A, 0xFF},
        };

        public List<byte[]> DutyEffects = new()
        {
            new byte[] { 2 },
            new byte[] { 0 },
            new byte[] { 0, 0, 0, 0, 0, 1, 1, 1, 2 },
        };

        public readonly byte[] SqTest = { Ins.VolEffect, 0x03, Ins.ModEffect, 0x00, Ins.DutyEffect, 0x00,
                                                    Len.l8, Notes.A2, Notes.E4, Notes.G3, Notes.Fs3, Notes.D4, Notes.G3,
                                                    Notes.D3, Notes.E4, Notes.G3, Notes.Fs3, Notes.D4, Notes.G3,
                                                    Notes.C3, Notes.B3, Len.l4, Notes.G3, Notes.B2,
                                                    Len.l8, Notes.A2, Notes.E4, Notes.G3, Notes.Fs3, Notes.D4, Notes.G3,
                                                    Notes.A2, Notes.E4, Notes.A3, Notes.E3, Notes.B3, Notes.A3,
                                                    Notes.A2, Notes.E4,
                                                    Ins.VolEffect, 0x00, Len.l4, Ins.ModEffect, 0x05, Notes.G4, Ins.ModEffect, 0x01, Notes.B4, Len.d2, Notes.C5, 
                                                    Len.l4, Ins.ModEffect, 0x05, Notes.C5, Ins.ModEffect, 0x01, Notes.D5, Notes.Fs4,
                                                    Len.d2, Notes.Gs4, Ins.ModEffect, 0x00, Notes.rest, Notes.rest, Notes.rest,
                                                    Ins.Loop
        };

        public readonly byte[] Sq2Test = { Ins.VolEffect, 0x00, Ins.ModEffect, 0x01, Ins.DutyEffect, 0x02,
                                                    Len.l2, Notes.A4, Len.l4, Ins.ModEffect, 0x03, Notes.A4, Len.l8, Ins.ModEffect, 0x01, Notes.E5, Len.d4, Notes.E5, Len.l4, Ins.ModEffect, 0x04, Notes.E5,
                                                    Ins.VolEffect, 0x04, Ins.ModEffect, 0x05, Notes.A4, Ins.ModEffect, 0x00, Notes.B4, Notes.G4, Ins.VolEffect, 0x00, Ins.ModEffect, 0x01, Len.d2, Notes.A4,
                                                    Len.d2, Ins.ModEffect, 0x00, Notes.rest,
                                                    Len.l4, Notes.rest, Ins.ModEffect, 0x05, Notes.E5, Ins.ModEffect, 0x01, Notes.G5, Len.l2, Notes.A5, Len.l4, Notes.G5, 
                                                    Ins.ModEffect, 0x05, Notes.E5, Ins.ModEffect, 0x01, Notes.Fs5, Notes.D5,
                                                    Len.d2, Notes.E5, Ins.ModEffect, 0x00, Notes.rest, Notes.rest, Notes.rest,

                                                    Ins.Loop 
        
        };

        public readonly byte[] TriTest = { Ins.VolEffect, 0x05, 
                                            Len.d4, Notes.A3, Len.l4, Notes.A3, Len.l8, Notes.A3, Len.l4, Notes.E3, Notes.G3, Notes.B3,
                                            Notes.F3, Notes.E3, Notes.B3,
                                            Len.d4, Notes.A3, Len.l4, Notes.A3, Len.l8, Notes.A3, Len.l4, Notes.E3, Notes.G3, Notes.B3,

                                            Len.d4, Notes.A3, Len.l4, Notes.A3, Len.l8, Notes.A3, Len.l4, Notes.C4, Notes.B3, Notes.F3,
                                            Notes.G4, Notes.E4, Notes.C4,
                                            Len.d4, Notes.B3, Len.l4, Notes.B3, Len.l8, Notes.B3, Len.l4, Notes.F3, Notes.A3, Notes.C4,
                                            Len.d4, Notes.B3, Len.l4, Notes.B3, Len.l8, Notes.B3, Len.l4, Notes.F3, Notes.A3, Notes.C4,
                                            Ins.Loop 
        };


        public readonly byte[] NoiseTest = { Ins.VolEffect, 0x02, Ins.ModEffect, 0x00, 
                                             Len.l8, 0x4, 0x4, Ins.VolEffect, 0x05, 0x4, Ins.VolEffect, 0x02, 0x4, 0x4, 0x4,
                                             Len.l4, Ins.VolEffect, 0x05, 0x4, 0x4, 0x4, Ins.Loop
        
        };

        public readonly byte[] DMCTest = { Len.l4, 0x1B, Len.l8, 0x1B, Len.l4, 0x0B, Len.l8, 0x0B, Len.l4, 0x1B, Len.l8, 0x1B, Len.l16, 0x1B, 0x1B, Len.l4, 0x0B, Ins.Loop 
        };


        //ticks per 32nd note
        public APU? apuReference = null; 
        
        int Tempo32 = 3;

        private OscTrack sqTrack;
        private OscTrack sq2Track;
        private OscTrack triTrack;
        private OscTrack noiseTrack;
        private DMCTrack dmcTrack;

        int DMCIndex = -1;
        int DMCTicksRemaining = 0;

        //public NoteTables.Notes GetSquare2Pitch() => Sq2Test[Sq2Index].Pitch;

        //public NoteTables.Notes GetTrianglePitch() => TriTest[TrIndex].Pitch;

        //public NoteTables.Notes GetNoisePitch() => NoiseTest[NoiseIndex].Pitch;

        //public DMC.Samples GetDMCSample() => DMCTest[DMCIndex].Sample;

        public Sequencer(APU? apu)
        {
            apuReference = apu;
            sqTrack = new(this, SqTest, Mixer.SQUARE_1);
            sq2Track = new(this, Sq2Test, Mixer.SQUARE_2);
            triTrack = new(this, TriTest, Mixer.TRIANGLE);
            noiseTrack = new(this, NoiseTest, Mixer.NOISE);
            dmcTrack = new(this, DMCTest, Mixer.DMC);
        }

        public int TickSequence()
        {
            int dirtyFlags = 0;



            if(sqTrack.TickTrack(Tempo32))
            {
                dirtyFlags = 128;
            }
            sq2Track.TickTrack(Tempo32);
            triTrack.TickTrack(Tempo32);
            noiseTrack.TickTrack(Tempo32);
            dmcTrack.TickTrack(Tempo32);

            //TrTicksRemaining--;
            //if(TrTicksRemaining <= 0)
            //{
            //    TrIndex = (TrIndex + 1) % TriTest.Length;
            //    TrTicksRemaining = TriTest[TrIndex].TickLengh;
            //    dirtyFlags |= 0b100;
            //}

            //NoiseTicksRemaining--;
            //if(NoiseTicksRemaining <= 0)
            //{
            //    NoiseIndex = (NoiseIndex + 1) % NoiseTest.Length;
            //    NoiseTicksRemaining = NoiseTest[NoiseIndex].TickLengh;
            //    dirtyFlags |= 0b1000;
            //}


            return dirtyFlags;
        }
    }

    public class Track
    {

        protected byte[]? sequence = null;
        protected Sequencer? parent = null;
        protected int targetChannel = 0;

        protected int index = -1;
        protected int ticksRemaining = 0;
        protected int tickTime = 0;

        protected int seqLength => sequence?.Length ?? 0;

        protected void IncIndex(int ticks = 1) => index = Math.Clamp(index + ticks, 0, seqLength - 1);

        public Track(Sequencer? parent, byte[]? sequence, int targetChannel)
        {
            this.parent = parent;
            this.sequence = sequence;
            this.targetChannel = targetChannel;
        }

        public virtual bool TickTrack(int Tempo32)
        {
            return false;
        }
    }
    public class DMCTrack : Track
    {
        public DMCTrack(Sequencer? parent, byte[]? sequence, int targetChannel) : base(parent, sequence, targetChannel)
        {

        }
        public override bool TickTrack(int Tempo32)
        {
            if (sequence is null || parent is null)
            {
                return false;
            }

            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                IncIndex();
                bool processedNote = false;

                while (!processedNote)
                {
                    byte val = sequence[index];

                    if (val >= 0x80 && val <= 0x9F)
                    {
                        tickTime = Len.GetTicks(val) * Tempo32;
                        IncIndex();
                    }
                    else
                    {
                        switch (val)
                        {
                            case Ins.Loop:
                                index = 0;
                                break;
                            default:
                                parent.apuReference?.TriggerDMC(val);
                                processedNote = true;
                                break;
                        }
                    }
                }
                ticksRemaining = tickTime;
            }
            return false;
        }
    }

    public class OscTrack : Track
    {
        public OscTrack(Sequencer? parent, byte[]? sequence, int targetChannel) : base(parent, sequence, targetChannel)
        {
        }
        public override bool TickTrack(int Tempo32)
        {
            if (sequence is null || parent is null)
            {
                return false;
            }

            bool looped = false;
            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                IncIndex();

                if (index == seqLength - 1)
                {
                    looped = true;
                }

                bool processedNote = false;

                while (!processedNote)
                {
                    byte val = sequence[index];

                    if (val >= 0x80 && val <= 0x9F)
                    {
                        tickTime = Len.GetTicks(val) * Tempo32;
                        IncIndex();
                    }
                    else
                    {
                        switch (val)
                        {
                            case Ins.VolEffect:
                                parent.apuReference?.SetOscilatorEffect(new VolEffect(parent.VolEffects[sequence[index + 1]]), targetChannel);
                                IncIndex(2);
                                break;
                            case Ins.ModEffect:
                                parent.apuReference?.SetOscilatorEffect(new ModEffect(parent.ModEffects[sequence[index + 1]]), targetChannel);
                                IncIndex(2);
                                break;
                            case Ins.ArpEffect:
                                parent.apuReference?.SetOscilatorEffect(new ArpEffect(parent.ArpEffects[sequence[index + 1]]), targetChannel);
                                IncIndex(2);
                                break;
                            case Ins.DutyEffect:
                                parent.apuReference?.SetOscilatorEffect(new DutyEffect(parent.DutyEffects[sequence[index + 1]]), targetChannel);
                                IncIndex(2);
                                break;
                            case Ins.Loop:
                                index = 0;
                                break;
                            default:
                                parent.apuReference?.SetOscilatorPitch(val, targetChannel);
                                processedNote = true;
                                break;
                        }
                    }
                }

                ticksRemaining = tickTime;
            }
            return looped;
        }
    }
}
