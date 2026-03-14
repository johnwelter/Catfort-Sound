using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using CatfortSound.SoundEngine.Sequence;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Tracks
{
    public class Track
    {
        protected Sequencer? Sequencer;
        protected APU? APU => Sequencer?.APU;
        protected byte[] sequence = [];
        protected int targetChannel = 0;


        protected int tickTotal = 0;
        protected int ticksRemaining = 0;
        protected int tickTime = 0;

        protected int seqIndex = -1;
        protected int entryIndex = -1;

        protected int seqLength => sequence?.Length ?? 0;
        protected int dataLength => Sequencer?.SongChart.GetChannelLength(targetChannel) ?? 0;
        protected ObservableCollection<Subloop> channelSubloops => Sequencer?.SongChart?.Subloops[targetChannel] ?? [];

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
            subLoopIndex = hasSubLoops ? 0 : -1;
            subLoopCounter = hasSubLoops ? channelSubloops[subLoopIndex].loopCount : -1;
        }
        protected void IncEntryIndex(int ticks = 1) => entryIndex = Math.Clamp(entryIndex + ticks, 0, dataLength);
        protected void IncSeqIndex(int ticks = 1) => seqIndex = Math.Clamp(seqIndex + ticks, 0, seqLength);
        public Track(Sequencer? parent, int targetChannel)
        {
            this.Sequencer = parent;
            this.targetChannel = targetChannel;
        }
        public virtual bool TickTrack(int Tempo)
        {
            if (Sequencer is null)
            {
                return false;
            }

            bool trackLooped = false;

            tickTotal += Tempo;
            if (tickTotal <= 0xFF)
            {
                return false;
            }
            tickTotal -= 0xFF;

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

                sequence = Sequencer.SongChart.GetEntry(targetChannel, entryIndex);
                seqIndex = 0;
                while (seqIndex < seqLength)
                {
                    byte val = sequence[seqIndex];

                    if (val >= 0x80 && val <= 0x9F)
                    {
                        tickTime = (int)(NoteConstants.GetTicks(val));
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
            if (channelSubloops is null || channelSubloops.Count == 0 || subLoopIndex == -1)
            {
                return;
            }

            //otherwise, check if the current entry index matches the current subloop end index
            if (entryIndex == channelSubloops[subLoopIndex].loopEndIndex)
            {
                //this was the same note as the end of the loop - decrement the loop count
                subLoopCounter--;
                if (subLoopCounter < 0)
                {
                    //that was the last loop for this sub loop, increment the loop index, don't update the entry index
                    subLoopIndex++;
                    if (subLoopIndex == channelSubloops.Count)
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
            if (Sequencer is null)
            {
                return;
            }

            switch (val)
            {
                case (int)Instructions.VolEffect:
                case (int)Instructions.ModEffect:
                case (int)Instructions.ArpEffect:
                case (int)Instructions.DutyEffect:
                    ProcessEffect(val);
                    IncSeqIndex(2);
                    break;
            }
        }
        public virtual void ProcessEffect(int val)
        {
            Effect? effect = val switch
            {
                (int)Instructions.VolEffect => new VolEffect(),
                (int)Instructions.ModEffect => new ModEffect(),
                (int)Instructions.ArpEffect => new ArpEffect(),
                (int)Instructions.DutyEffect => new DutyEffect(),
                _ => throw new NotImplementedException()
            };
            int effectIdx = sequence[seqIndex + 1];
            effect.SetEffectBytes(APU?.EffectsBank?.GetEffectByType(effect.GetType(), effectIdx) ?? []);
            APU?.SetOscilatorEffect(effect, targetChannel);
        }
        public virtual void ProcessNote(int note)
        {
            APU?.SetOscilatorPitch(note, targetChannel);
        }

    }
}
