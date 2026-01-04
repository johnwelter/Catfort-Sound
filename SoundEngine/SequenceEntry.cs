using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CatfortSound.SoundEngine
{

    public abstract class SequenceEntry
    {
        public SequenceEntry()
        {
            Length = Lengths._;
        }
        public Lengths Length { get; set; }
        public abstract byte[] GetEntryBytes();

    }

    public abstract class GeneratorChannelEntry : SequenceEntry
    {
        public GeneratorChannelEntry() : base()
        {
            volEffect = -1;
        }
        //pulse, triangle, noise
        public int volEffect { get; set; }

    }

    public class OscEntry : GeneratorChannelEntry
    {
        public OscEntry() : base()
        {
            Note = Notes.rest;
            modEffect = -1;
            arpEffect = -1;
        }

        //pulse, triangle
        public int modEffect { get; set; }
        public int arpEffect { get; set; }
        public Notes Note { get; set; }

        public override byte[] GetEntryBytes()
        {
            List<byte> bytes = new();
            if(Length != Lengths._)
            {
                bytes.Add((byte)Length);
            }
            if(volEffect >= 0)
            {
                bytes.Add((byte)Instructions.VolEffect);
                bytes.Add((byte)volEffect);
            }
            if(modEffect >= 0)
            {
                bytes.Add((byte)Instructions.ModEffect);
                bytes.Add((byte)modEffect);
            }
            if(arpEffect >= 0)
            {
                bytes.Add((byte)Instructions.ArpEffect);
                bytes.Add((byte)arpEffect);
            }
            bytes.Add((byte)Note);

            return bytes.ToArray();
        }
    }

    public class PulseEntry : OscEntry
    {
        public PulseEntry() : base()
        {
            dutyEffect = -1;
        }
        public int dutyEffect { get; set; }

        public override byte[] GetEntryBytes()
        {
            List<byte> bytes = new();
            if (Length != Lengths._)
            {
                bytes.Add((byte)Length);
            }
            if (volEffect >= 0)
            {
                bytes.Add((byte)Instructions.VolEffect);
                bytes.Add((byte)volEffect);
            }
            if (modEffect >= 0)
            {
                bytes.Add((byte)Instructions.ModEffect);
                bytes.Add((byte)modEffect);
            }
            if (arpEffect >= 0)
            {
                bytes.Add((byte)Instructions.ArpEffect);
                bytes.Add((byte)arpEffect);
            }
            if(dutyEffect >= 0)
            {
                bytes.Add((byte)Instructions.DutyEffect);
                bytes.Add((byte)dutyEffect);
            }

            bytes.Add((byte)Note);



            return bytes.ToArray();
        }
    }

    public class NoiseEntry : GeneratorChannelEntry
    {
        public NoiseEntry() : base()
        {
            pitch = -1;
        }
        public int pitch { get; set; }

        public override byte[] GetEntryBytes()
        {
            List<byte> bytes = new();
            if (Length != Lengths._)
            {
                bytes.Add((byte)Length);
            }
            if (volEffect >= 0)
            {
                bytes.Add((byte)Instructions.VolEffect);
                bytes.Add((byte)volEffect);
            }

            //TODO: make this more graceful. 

            bytes.Add(pitch == -1 ? (byte)17 : (byte)pitch);

            return bytes.ToArray();
        }
    }

    public class DMCEntry : SequenceEntry
    {
        public DMC.Samples sample { get; set; }
        public int pitch { get; set; }

        public override byte[] GetEntryBytes()
        {
            List<byte> bytes = new();
            if (Length != Lengths._)
            {
                bytes.Add((byte)Length);
            }

            if(sample == DMC.Samples.kNone)
            {
                bytes.Add((byte)sample);
            }
            else
            {
                bytes.Add((byte)(((byte)sample << 4) | pitch));
            }
            return bytes.ToArray();
        }
    }
}
