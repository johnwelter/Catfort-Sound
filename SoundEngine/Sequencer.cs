using CatfortSound.SoundEngine;
using CatfortSound.ViewModels;
using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
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
    public class SongChart
    {
        public object[] Channels =
        [
            new ObservableCollection<PulseEntry>(),
            new ObservableCollection<PulseEntry>(),
            new ObservableCollection<OscEntry>(),
            new ObservableCollection<NoiseEntry>(),
            new ObservableCollection<DMCEntry>(),
        ];

        public ObservableCollection<Subloop>[] Subloops =
        [
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
        ];

        public void Clear()
        {
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i].ClearChannel();
                Subloops[i].Clear();
            }
        }

        public int GetChannelLength(int idx)
        {
            PropertyInfo property = Channels[idx].GetType().GetProperty("Count");
            return (int)property.GetValue(Channels[idx]);
        }

        public byte[] GetChannelEntry(int channel, int entry)
        {
            return Channels[channel].GetEntryBytes(entry);
        }

        public byte[] GetFullByteData()
        {
            List<byte> SerializedChart = new();
            for(int i = 0; i < Channels.Length; i++)
            {
                SerializedChart.AddRange(BitConverter.GetBytes(GetChannelLength(i)));
                SerializedChart.AddRange(Channels[i].GetEntryBytes());
                SerializedChart.AddRange(BitConverter.GetBytes(Subloops[i].Count));
                foreach (Subloop loop in Subloops[i])
                {
                    SerializedChart.AddRange(loop.GetLoopDataBytes());
                }
            }
            return SerializedChart.ToArray();
        }

        public int GetNextInt(byte[] buffer, ref int index)
        {
            byte[] intArray = new byte[4];
            Array.Copy(buffer, index, intArray, 0, 4);
            index += 4;
            return BitConverter.ToInt32 (intArray, 0);
        }

        public void ReadByteData(byte[] buffer)
        {

            Clear();
            //TODO - make this more... not like this.
            int[] widths = [7, 7, 6, 3, 2];
            int loopWidth = (3 * sizeof(int));

            int bufferIdx = 0;
            int chunkLength = 0;
            for(int i = 0; i < Channels.Length; i++)
            {
                //chunk 1 = tracker data
                chunkLength = GetNextInt(buffer, ref bufferIdx) * widths[i];
                byte[] chunkArray = new byte[chunkLength];
                Array.Copy(buffer, bufferIdx, chunkArray, 0, chunkArray.Length);
                bufferIdx += chunkLength;
                Channels[i].ReadEntryBytes(chunkArray, widths[i]);

                //chunk 2 = loop data - always 3 bytes long each
                chunkLength = GetNextInt(buffer, ref bufferIdx) * loopWidth;
                int maxIndex = bufferIdx + chunkLength;
                while(bufferIdx < maxIndex)
                {
                    int start = GetNextInt(buffer, ref bufferIdx);
                    int end = GetNextInt(buffer, ref bufferIdx);
                    int count = GetNextInt(buffer, ref bufferIdx);
                    Subloops[i].Add(new Subloop(start, end, count));
                }
            }
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

        public SongChart SongChart;

        //ticks per 32nd note
        public APU? apuReference = null;

        bool use8 = false;
        int Tempo32 = 3;

        private Track[] tracks = new Track[5];

        public Sequencer(APU? apu)
        {
            apuReference = apu;

            SongChart = new SongChart();

            tracks[0] = new OscTrack(this, (int)ChannelIndexes.SQUARE_1);
            tracks[1] = new OscTrack(this, (int)ChannelIndexes.SQUARE_2);
            tracks[2] = new OscTrack(this, (int)ChannelIndexes.TRIANGLE);
            tracks[3] = new OscTrack(this, (int)ChannelIndexes.NOISE);
            tracks[4] = new DMCTrack(this, (int)ChannelIndexes.DMC);
        }

        public void Reset()
        {
            foreach(var track in tracks)
            {
                track.Reset();
            }
        }

        public void ClearSequencer()
        {
            SongChart.Clear();
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

            int idx = 0;
            foreach(var track in tracks)
            {
                bool looped = track.TickTrack(Tempo32, use8);
                if(idx == 0 && looped)
                {
                    dirtyFlags |= 128;
                }
            }
            return dirtyFlags;
        }
    }

    public class Subloop
    {
        public Subloop(int startIndex, int endIndex, int count)
        {
            loopStartIndex = startIndex;
            loopEndIndex = endIndex;
            loopCount = count;
        }

        public Subloop()
        {
            loopStartIndex = -1;
            loopEndIndex = -1;
            loopCount = 0;
        }
        public int loopStartIndex { get; set; } // where to return when the loop instrtuction is played
        public int loopEndIndex { get; set; } // where to insert the loop instruction 
        public int loopCount { get; set; } //how many times to loop before skipping the loop instruction

        public byte[] GetLoopDataBytes()
        {
            List<byte> loopData = new List<byte>();
            loopData.AddRange(BitConverter.GetBytes(loopStartIndex));
            loopData.AddRange(BitConverter.GetBytes(loopEndIndex));
            loopData.AddRange(BitConverter.GetBytes(loopCount));

            return loopData.ToArray();
        }
    }

    public class Track
    {

        protected Sequencer? parent;
        protected byte[] sequence = { };
        protected int targetChannel = 0;

        protected int ticksRemaining = 0;
        protected int tickTime = 0;

        protected int seqIndex = -1;
        protected int entryIndex = -1;

        protected int seqLength => sequence?.Length ?? 0;
        protected int dataLength => parent?.SongChart.GetChannelLength(targetChannel) ?? 0;
        protected ObservableCollection<Subloop> channelSubloops => parent.SongChart.Subloops[targetChannel];

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

            bool hasSubLoops = channelSubloops.Count > 0;
            subLoopIndex = hasSubLoops? 0 : -1;
            subLoopCounter = hasSubLoops ? channelSubloops[subLoopIndex].loopCount : -1;
        }

        protected void IncEntryIndex(int ticks = 1) => entryIndex = Math.Clamp(entryIndex + ticks, 0, dataLength);
        protected void IncSeqIndex(int ticks = 1) => seqIndex = Math.Clamp(seqIndex + ticks, 0, seqLength);

        public Track(Sequencer? parent, int targetChannel)
        {
            this.parent = parent;
            this.targetChannel = targetChannel;
        }

        public virtual bool TickTrack(int Tempo32, bool use8)
        {
            if (parent is null)
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
                    bool hasSubLoops = channelSubloops.Count > 0;
                    subLoopIndex = hasSubLoops ? 0 : -1;
                    subLoopCounter = hasSubLoops ? channelSubloops[subLoopIndex].loopCount : -1;

                    trackLooped = true;
                }

                sequence = parent.SongChart.GetChannelEntry(targetChannel, entryIndex);
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
            if(channelSubloops is null || channelSubloops.Count == 0 || subLoopIndex == -1)
            {
                return;
            }

            //otherwise, check if the current entry index matches the current subloop end index
            if(entryIndex == channelSubloops[subLoopIndex].loopEndIndex)
            {
                //this was the same note as the end of the loop - decrement the loop count
                subLoopCounter--;
                if(subLoopCounter < 0)
                {
                    //that was the last loop for this sub loop, increment the loop index, don't update the entry index
                    subLoopIndex++;
                    if(subLoopIndex == channelSubloops.Count)
                    {
                        //kill the subloop system if the song ends - track looping will reset this instead
                        subLoopIndex = -1;
                    }
                    else
                    {
                        subLoopCounter = channelSubloops[subLoopIndex].loopCount;
                    }
                }
                else 
                {
                    //set the entry point - entry indexes are incremented at the beginning of processing, so we'll go for one before
                    entryIndex = channelSubloops[subLoopIndex].loopStartIndex - 1;
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
                case (int)Instructions.VolEffect:
                    parent.apuReference?.SetOscilatorEffect(new VolEffect(parent.VolEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case (int)Instructions.ModEffect:
                    parent.apuReference?.SetOscilatorEffect(new ModEffect(parent.ModEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case (int)Instructions.ArpEffect:
                    parent.apuReference?.SetOscilatorEffect(new ArpEffect(parent.ArpEffects[sequence[seqIndex + 1]]), targetChannel);
                    IncSeqIndex(2);
                    break;
                case (int)Instructions.DutyEffect:
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
        public DMCTrack(Sequencer? parent, int targetChannel) : base(parent, targetChannel)
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

        public OscTrack(Sequencer? parent, int targetChannel) : base (parent, targetChannel) 
        {
        }
    }
}
