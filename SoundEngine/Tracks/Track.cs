using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using CatfortSound.SoundEngine.Sequence;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CatfortSound.SoundEngine.Effects.EffectStack;

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
            Lengths delayLength = Sequencer?.SongChart?.ChannelSettings[targetChannel].DelayLength ?? Lengths._;
            ticksRemaining = (int)delayLength == 0 ? 0 : (int)(NoteConstants.GetTicks((byte)delayLength)) + 1;
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
                if (subLoopCounter <= 0)
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

            //TODO: the engine has these is bad places. we'll want to reorder them for an easier index catch
            switch (val)
            {
                case (int)Instructions.VolEffect:
                case (int)Instructions.DutyEffect:
                case (int)Instructions.ModEffect:
                case (int)Instructions.ArpEffect:
                    ProcessEffect(val);
                    IncSeqIndex(2);
                    break;
            }
        }
        public virtual void ProcessEffect(int val)
        {
            //TODO: the engine has these is bad places. we'll want to reorder them for an easier index catch
            EffectSlots type = val switch
            {
                (int)Instructions.VolEffect => EffectSlots.kVol,
                (int)Instructions.DutyEffect => EffectSlots.kDuty,
                (int)Instructions.ModEffect => EffectSlots.kMod,
                (int)Instructions.ArpEffect => EffectSlots.kArp,
                _ => throw new NotImplementedException()
            };
            int effectIdx = sequence[seqIndex + 1];
            Effect? effect = APU?.EffectsBank?.GetEffectByType((int)type, effectIdx);

            if(effect != null)
            {
                APU?.SetOscilatorEffect(type, effect, targetChannel);
            }
        }
        public virtual void ProcessNote(int note)
        {
            APU?.SetOscilatorPitch(note, targetChannel);
        }

    }
}
