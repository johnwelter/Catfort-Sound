using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using CatfortSound.SoundEngine.SongData;
using CatfortSound.SoundEngine.Tracks;
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
using System.Windows.Controls;

namespace CatfortSound.SoundEngine.Sequence
{
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

    public class Sequencer
    {
        public SongChart SongChart;

        //ticks per 32nd note
        public APU? APU = null;

        private Track[] tracks = new Track[5];

        public Sequencer(APU? apu)
        {
            APU = apu;

            SongChart = new SongChart();

            tracks[0] = new OscTrack(this, (int)ChannelIndexes.SQUARE_1);
            tracks[1] = new OscTrack(this, (int)ChannelIndexes.SQUARE_2);
            tracks[2] = new OscTrack(this, (int)ChannelIndexes.TRIANGLE);
            tracks[3] = new OscTrack(this, (int)ChannelIndexes.NOISE);
            tracks[4] = new DMCTrack(this, (int)ChannelIndexes.DPMC);
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

        public void TickSequence()
        {
            foreach(var track in tracks)
            {
                bool looped = track.TickTrack(SongChart.LockedTempo);
            }
        }
    }

}
