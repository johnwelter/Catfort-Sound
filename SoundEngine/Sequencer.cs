using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
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

        public static float GetTicks(byte idx, bool use8) => use8?LenTable8[idx - 0x80] : LenTable[idx - 0x80];

        public static readonly float[] LenTable =
        {
            1, 2, 4, 8, 16, 32, (1 + 2), (2 + 4), (4 + 8), (8 + 16), (16 + 32), (4f/3f), (8f/3f), (16f/3f)
        };

        public static readonly float[] LenTable8 =
        {
            0.25f, 0.5f, 1, 2, 4, 8, (0.25f + 0.5f), (0.5f + 1), (1 + 2), (2 + 4), (4 + 8), (1f/3f), (2f/3f), (4f/3f)
        };
    }

    public enum Instructions
    {
        End = 0xA0,
        Loop = 0xA1,
        VolEffect = 0xA2, //in engine, effects are technically always on - we just have single byte "none" options!
        DutyEffect = 0xA3, // for full channel duty shifts, not effects *in engine* - will fix later, but will program it as effects in here
        SetLoop1_Counter = 0xA4,
        Loop1 = 0xA5,
        SetNoteOffset = 0xA7,
        Transpose = 0xA8,
        ModEffect = 0xA9,
        ArpEffect = 0xAA,
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


    public class Sequence
    {

        public List<PulseEntry> pulse1Sequence = new();
        public List<PulseEntry> pulse2Sequence = new();
        public List<OscEntry> triangleSequence = new();
        public List<NoiseEntry> noiseSequence = new();
        public List<DMCEntry> dmcSequence = new();

        public List<Subloop> pulse1Subloops = new();
        public List<Subloop> pulse2Subloops = new();
        public List<Subloop> triangleSubloops = new();
        public List<Subloop> noiseSubloops = new();
        public List<Subloop> dmcSubloops = new();
        public void Clear()
        {
            pulse1Sequence.Clear();
            pulse1Subloops.Clear();
            pulse2Sequence.Clear();
            pulse2Subloops.Clear();
            triangleSequence.Clear();
            triangleSubloops.Clear();
            noiseSequence.Clear();
            noiseSubloops.Clear();
            dmcSequence.Clear();
            dmcSubloops.Clear();
        }
    }
    public class Sequencer
    {
        //effect table
        public List<byte[]> VolEffects = new()
        {
            new byte[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0 },
            new byte[] { 15, 15, 15, 11, 11, 11, 7, 7, 7, 5, 5, 5 },
            new byte[] { 15, 9, 6, 3, 0 },
            new byte[] { 10, 11, 12, 13, 14, 15, 0, 0, 10, 10, 10, 10, 10, 0, 0, 5, 5, 5, 5, 0, 0, 2, 2, 1, 1, 1, 0},
            new byte[] { 15 },
            new byte[] { 15, 9, 8, 6, 4, 3, 2, 1, 0},
            new byte[] { 0 },
            new byte[] {15, 15, 15, 15, 15, 14, 14, 14, 0},
            new byte[] { 15, 15, 9, 9, 8, 8, 6, 6, 4, 4, 3, 3, 2, 2, 1, 1, 0},
            new byte[] { 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 0},
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
            new byte[] { 2 },
            new byte[] { 1 },
            new byte[] { 0, 0, 0, 0, 0, 1, 1, 1, 2 },
        };

        public Sequence seqChart;

        public readonly byte[] SqTest = { Ins.VolEffect, 0x03, Ins.ModEffect, 0x00, Ins.DutyEffect, 0x00,
                                                    Len.l8, NoteClass.A2, NoteClass.E4, NoteClass.G3, NoteClass.Fs3, NoteClass.D4, NoteClass.G3,
                                                    NoteClass.D3, NoteClass.E4, NoteClass.G3, NoteClass.Fs3, NoteClass.D4, NoteClass.G3,
                                                    NoteClass.C3, NoteClass.B3, Len.l4, NoteClass.G3, NoteClass.B2,
                                                    Len.l8, NoteClass.A2, NoteClass.E4, NoteClass.G3, NoteClass.Fs3, NoteClass.D4, NoteClass.G3,
                                                    NoteClass.A2, NoteClass.E4, NoteClass.A3, NoteClass.E3, NoteClass.B3, NoteClass.A3,
                                                    NoteClass.A2, NoteClass.E4,
                                                    Ins.VolEffect, 0x00, Len.l4, Ins.ModEffect, 0x05, NoteClass.G4, Ins.ModEffect, 0x01, NoteClass.B4, Len.d2, NoteClass.C5, 
                                                    Len.l4, Ins.ModEffect, 0x05, NoteClass.C5, Ins.ModEffect, 0x01, NoteClass.D5, NoteClass.Fs4,
                                                    Len.d2, NoteClass.Gs4, Ins.ModEffect, 0x00, NoteClass.rest, NoteClass.rest, NoteClass.rest,
                                                    Ins.Loop
        };

        public readonly byte[] Sq2Test = { Ins.VolEffect, 0x00, Ins.ModEffect, 0x01, Ins.DutyEffect, 0x02,
                                                    Len.l2, NoteClass.A4, Len.l4, Ins.ModEffect, 0x03, NoteClass.A4, Len.l8, Ins.ModEffect, 0x01, NoteClass.E5, Len.d4, NoteClass.E5, Len.l4, Ins.ModEffect, 0x04, NoteClass.E5,
                                                    Ins.VolEffect, 0x04, Ins.ModEffect, 0x05, NoteClass.A4, Ins.ModEffect, 0x00, NoteClass.B4, NoteClass.G4, Ins.VolEffect, 0x00, Ins.ModEffect, 0x01, Len.d2, NoteClass.A4,
                                                    Len.d2, Ins.ModEffect, 0x00, NoteClass.rest,
                                                    Len.l4, NoteClass.rest, Ins.ModEffect, 0x05, NoteClass.E5, Ins.ModEffect, 0x01, NoteClass.G5, Len.l2, NoteClass.A5, Len.l4, NoteClass.G5, 
                                                    Ins.ModEffect, 0x05, NoteClass.E5, Ins.ModEffect, 0x01, NoteClass.Fs5, NoteClass.D5,
                                                    Len.d2, NoteClass.E5, Ins.ModEffect, 0x00, NoteClass.rest, NoteClass.rest, NoteClass.rest,

                                                    Ins.Loop 
        
        };

        public readonly byte[] TriTest = { Ins.VolEffect, 0x05, 
                                            Len.d4, NoteClass.A3, Len.l4, NoteClass.A3, Len.l8, NoteClass.A3, Len.l4, NoteClass.E3, NoteClass.G3, NoteClass.B3,
                                            NoteClass.F3, NoteClass.E3, NoteClass.B3,
                                            Len.d4, NoteClass.A3, Len.l4, NoteClass.A3, Len.l8, NoteClass.A3, Len.l4, NoteClass.E3, NoteClass.G3, NoteClass.B3,

                                            Len.d4, NoteClass.A3, Len.l4, NoteClass.A3, Len.l8, NoteClass.A3, Len.l4, NoteClass.C4, NoteClass.B3, NoteClass.F3,
                                            NoteClass.G4, NoteClass.E4, NoteClass.C4,
                                            Len.d4, NoteClass.B3, Len.l4, NoteClass.B3, Len.l8, NoteClass.B3, Len.l4, NoteClass.F3, NoteClass.A3, NoteClass.C4,
                                            Len.d4, NoteClass.B3, Len.l4, NoteClass.B3, Len.l8, NoteClass.B3, Len.l4, NoteClass.F3, NoteClass.A3, NoteClass.C4,
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

        bool use8 = false;
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

            seqChart = new Sequence();

            sqTrack = new(this, seqChart.pulse1Sequence, seqChart.pulse1Subloops, Mixer.SQUARE_1);
            sq2Track = new(this, seqChart.pulse2Sequence, seqChart.pulse2Subloops, Mixer.SQUARE_2);
            triTrack = new(this, seqChart.triangleSequence, seqChart.triangleSubloops, Mixer.TRIANGLE);
            noiseTrack = new(this, seqChart.noiseSequence, seqChart.noiseSubloops, Mixer.NOISE);
            dmcTrack = new(this, seqChart.dmcSequence, seqChart.dmcSubloops, Mixer.DMC);
        }

        public void Reload()
        {
            sqTrack.LoadSequenceData(seqChart.pulse1Sequence, seqChart.pulse1Subloops);         
            sq2Track.LoadSequenceData(seqChart.pulse2Sequence, seqChart.pulse2Subloops);        
            triTrack.LoadSequenceData(seqChart.triangleSequence, seqChart.triangleSubloops);          
            noiseTrack.LoadSequenceData(seqChart.noiseSequence, seqChart.noiseSubloops);          
            dmcTrack.LoadSequenceData(seqChart.dmcSequence, seqChart.dmcSubloops);
            Reset();
        }

        public void Reset()
        {
            sqTrack.Reset(); 
            sq2Track.Reset();
            triTrack.Reset();
            noiseTrack.Reset();
            dmcTrack.Reset();
        }

        public void ClearSequencer()
        {
            seqChart.Clear();
            Reset();
        }

        public void SetTempo(int tempo, bool? use8)
        {
            this.use8 = use8 ?? false;
            Tempo32 = tempo;
        }

        public int TickSequence()
        {
            int dirtyFlags = 0;

            if(sqTrack.TickTrack(Tempo32, use8))
            {
                dirtyFlags |= 128;
            }
            
            sq2Track.TickTrack(Tempo32, use8);
            triTrack.TickTrack(Tempo32, use8);
            noiseTrack.TickTrack(Tempo32, use8);
            dmcTrack.TickTrack(Tempo32, use8);

            return dirtyFlags;
        }
    }

    public class Subloop
    {
        public Subloop()
        {
            loopStartIndex = -1;
            loopEndIndex = -1;
            loopCount = 0;
        }
        public int loopStartIndex { get; set; } // where to return when the loop instrtuction is played
        public int loopEndIndex { get; set; } // where to insert the loop instruction 
        public int loopCount { get; set; } //how many times to loop before skipping the loop instruction
    }

    public class Track
    {
        protected IEnumerable<SequenceEntry> sequenceData;
        protected List<Subloop> subLoopList;
        protected byte[] sequence = { };
        protected Sequencer? parent = null;
        protected int targetChannel = 0;

        protected int ticksRemaining = 0;
        protected int tickTime = 0;

        protected int seqIndex = -1;
        protected int entryIndex = -1;

        protected int seqLength => sequence?.Length ?? 0;
        protected int dataLength => sequenceData.Count();

        //only allow one for now
        protected int subLoopIndex = -1;
        protected int subLoopCounter = -1;


        public void Reset()
        {
            ticksRemaining = 0;
            tickTime = 0;
            seqIndex = -1;
            entryIndex = -1;
            sequence = new byte[] { };

            bool hasSubLoops = subLoopList.Count > 0;
            subLoopIndex = hasSubLoops? 0 : -1;
            subLoopCounter = hasSubLoops ? subLoopList[subLoopIndex].loopCount : -1;
        }

        public void LoadSequenceData(IEnumerable<SequenceEntry> data, List<Subloop> loops)
        {
            sequenceData = data;
            subLoopList = loops;
        }

        protected void IncEntryIndex(int ticks = 1) => entryIndex = Math.Clamp(entryIndex + ticks, 0, dataLength);
        protected void IncSeqIndex(int ticks = 1) => seqIndex = Math.Clamp(seqIndex + ticks, 0, seqLength);

        public Track(Sequencer? parent, IEnumerable<SequenceEntry> data, List<Subloop> loops, int targetChannel)
        {
            this.parent = parent;
            this.sequenceData = data;
            this.targetChannel = targetChannel;
            this.subLoopList = loops;
        }

        public virtual bool TickTrack(int Tempo32, bool use8)
        {
            if (sequenceData is null || sequenceData.Count() == 0 || parent is null)
            {
                return false;
            }

            bool trackLooped = false;
            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                IncEntryIndex();
                if (entryIndex == dataLength)
                {
                    entryIndex = 0;

                    //song loops, reset the subloop system
                    bool hasSubLoops = subLoopList.Count > 0;
                    subLoopIndex = hasSubLoops ? 0 : -1;
                    subLoopCounter = hasSubLoops ? subLoopList[subLoopIndex].loopCount : -1;

                    trackLooped = true;
                }

                sequence = sequenceData.ElementAt<SequenceEntry>(entryIndex).GetEntryBytes();
                seqIndex = 0;
                while (seqIndex < seqLength)
                {
                    byte val = sequence[seqIndex];

                    if (val >= 0x80 && val <= 0x9F)
                    {
                        tickTime = (int)(Len.GetTicks(val, use8) * Tempo32);
                        IncSeqIndex();
                    }
                    else if (val >= 0xA0)
                    {
                        ParseInstruction(val);
                    }
                    else
                    {
                        ProcessNote(val);
                        IncSeqIndex();
                    }
                }
                //note chunk was finished - check for loops
                ProcessLoop();

                ticksRemaining = tickTime;
            }
            return trackLooped;
        }

        private void ProcessLoop()
        {
            //if we don't have proper loop indexes, bail out
            if(subLoopList is null || subLoopList.Count == 0 || subLoopIndex == -1)
            {
                return;
            }

            //otherwise, check if the current entry index matches the current subloop end index
            if(entryIndex == subLoopList[subLoopIndex].loopEndIndex)
            {
                //this was the same note as the end of the loop - decrement the loop count
                subLoopCounter--;
                if(subLoopCounter < 0)
                {
                    //that was the last loop for this sub loop, increment the loop index, don't update the entry index
                    subLoopIndex++;
                    if(subLoopIndex == subLoopList.Count)
                    {
                        //kill the subloop system if the song ends - track looping will reset this instead
                        subLoopIndex = -1;
                    }
                    else
                    {
                        subLoopCounter = subLoopList[subLoopIndex].loopCount;
                    }
                }
                else 
                {
                    //set the entry point - entry indexes are incremented at the beginning of processing, so we'll go for one before
                    entryIndex = subLoopList[subLoopIndex].loopStartIndex - 1;
                }
            }
        }

        public virtual void ParseInstruction(int val)
        {

            if(parent is null)
            {
                return;
            }
            switch (val)
            {
                case Ins.VolEffect:
                    parent.apuReference?.SetOscilatorEffect(new VolEffect(parent.VolEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case Ins.ModEffect:
                    parent.apuReference?.SetOscilatorEffect(new ModEffect(parent.ModEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case Ins.ArpEffect:
                    parent.apuReference?.SetOscilatorEffect(new ArpEffect(parent.ArpEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case Ins.DutyEffect:
                    parent.apuReference?.SetOscilatorEffect(new DutyEffect(parent.DutyEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
            } 
        }

        public virtual void ProcessNote(int note)
        {
            if (parent is null)
            {
                return;
            }
            parent.apuReference?.SetOscilatorPitch(note, targetChannel);
        }
    }

    public class DMCTrack : Track
    {
        public DMCTrack(Sequencer? parent, IEnumerable<SequenceEntry> data, List<Subloop> loops, int targetChannel) : base(parent, data, loops, targetChannel)
        {

        }
        public override void ProcessNote(int note)
        {
            if (parent is null)
            {
                return;
            }
            parent.apuReference?.TriggerDMC(note);
        }

        public override void ParseInstruction(int val)
        {
            //DMC ignores instructions... for now
            ProcessNote(val);
            IncSeqIndex();
        }
    }

    public class OscTrack : Track
    {

        public OscTrack(Sequencer? parent, IEnumerable<SequenceEntry> data, List<Subloop> loops, int targetChannel) : base(parent, data, loops, targetChannel)
        {
        }
    }
}
