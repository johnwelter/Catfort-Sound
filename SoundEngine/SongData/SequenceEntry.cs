using CatfortSound.SoundEngine.Banks;
using CatfortSound.SoundEngine.Channels;
using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using FFmpeg.AutoGen;
using Melanchall.DryWetMidi.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows.Controls;

namespace CatfortSound.SoundEngine.SongData
{
    public enum GetModes
    {
        playback = 0,
        save = 1,
        export = 2
    }

    [Serializable()]
    public abstract class SequenceEntry : INotifyPropertyChanged
    {

        protected Lengths _length;    
        public Lengths Length 
        {
            get { return _length; }
            set 
            { 
                _length = value; 
                OnPropertyChanged(nameof(Length));
            }
        }

        public SequenceEntry(byte[] data)
        {
            Length = (Lengths)data[0];
        }

        public SequenceEntry()
        {
            Length = Lengths._;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }

        public virtual byte[] GetEntryBytes(GetModes getMode = GetModes.playback)
        {
            List<byte> bytes = new();
            BuildByteList(bytes, getMode);
            return bytes.ToArray();
        }

        public virtual void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            if (Length != Lengths._ || getMode == GetModes.save)
            {
                bytes.Add((byte)Length);
            }
        }
    }

    public abstract class GeneratorChannelEntry : SequenceEntry
    {

        public GeneratorChannelEntry(byte[] data) : base(data) 
        {
            VolEffect = (sbyte)data[1];
        }

        public GeneratorChannelEntry() : base()
        {
            VolEffect = -1;
        }

        //pulse, triangle, noise

        protected int _volEffect;
        public int VolEffect
        {
            get { return _volEffect; }
            set
            {
                _volEffect = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        public override void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            base.BuildByteList(bytes, getMode);
            bool save = getMode == GetModes.save;
            if (VolEffect >= 0 || save)
            {
                if(!save)
                    bytes.Add((byte)Instructions.VolEffect);
                bytes.Add((byte)VolEffect);
            }
        }

    }

    public class OscEntry : GeneratorChannelEntry
    {
        public OscEntry(byte[] data):base(data)
        {
            ModEffect = (sbyte)data[2];
            ArpEffect = (sbyte)data[3];
            //on reverse loading notes: note = byte%12, octave = floor(byte/12)+1 
            byte cmpNote = GetNote(data);
            Note = cmpNote == NoteConstants.Rest ? Notes.rest : (Notes)(cmpNote % 12);
            Octave = cmpNote == NoteConstants.Rest ? 4 : (int)(cmpNote / 12.0) + 1;
        }

        protected virtual byte GetNote(byte[] data) => data[4];

        public OscEntry() : base()
        {
            Note = Notes.rest;
            Octave = 4;
            ModEffect = -1;
            ArpEffect = -1;
        }

        //pulse, triangle
        protected int _modEffect;
        public int ModEffect
        {
            get { return _modEffect; }
            set
            {
                _modEffect = value;
                OnPropertyChanged(nameof(ModEffect));
            }
        }


        protected int _arpEffect;
        public int ArpEffect
        {
            get { return _arpEffect; }
            set
            {
                _arpEffect = value;
                OnPropertyChanged(nameof(ArpEffect));
            }
        }

        protected Notes _note;
        public Notes Note
        {
            get { return _note; }
            set
            {
                _note = value;
                OnPropertyChanged(nameof(Note));
            }
        }

        protected int _octave;
        public int Octave
        {
            get { return _octave; }
            set
            {
                _octave = value;
                OnPropertyChanged(nameof(Octave));
            }
        }

        public override void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            base.BuildByteList(bytes, getMode);
            bool save = getMode == GetModes.save;

            if (ModEffect >= 0 || save)
            {
                if (!save)
                    bytes.Add((byte)Instructions.ModEffect);
                bytes.Add((byte)ModEffect);
            }
            if (ArpEffect >= 0 || save)
            {
                if (!save)
                    bytes.Add((byte)Instructions.ArpEffect);
                bytes.Add((byte)ArpEffect);
            }

            byte outNote = Note == Notes.rest ? (byte)NoteConstants.Rest : (byte)Math.Clamp((int)Note + 0xC * (Octave-1), 0, 0x5d); 
            bytes.Add(outNote);
        }
    }

    public class PulseEntry : OscEntry
    {
        public PulseEntry(byte[] data):base(data)
        {
            DutyEffect = (sbyte)data[4];
        }
        public PulseEntry() : base()
        {
            DutyEffect = -1;
        }

        protected override byte GetNote(byte[] data) => data[5];

        protected int _dutyEffect;
        public int DutyEffect
        {
            get { return _dutyEffect; }
            set
            {
                _dutyEffect = value;
                OnPropertyChanged(nameof(DutyEffect));
            }
        }

        public override void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            base.BuildByteList(bytes, getMode);
            bool save = getMode == GetModes.save;

            //since we want the note to be the last thing, squeeze this in there
            if (DutyEffect >= 0 || save)
            {
                if (!save)
                    bytes.Insert(bytes.Count-1, (byte)Instructions.DutyEffect);

                //on export, we need the extra info
                if(getMode == GetModes.export)
                    bytes.Insert(bytes.Count-1, (byte)((DutyEffect<<6)|0x30));
                else
                    bytes.Insert(bytes.Count - 1, (byte)DutyEffect);
            }
        }
    }

    public class NoiseEntry : GeneratorChannelEntry
    {

        public NoiseEntry(byte[] data) : base(data)
        {
            Pitch = data[2];
            if(Pitch == 17)
            {
                Pitch = -1;
            }
        }

        public NoiseEntry() : base()
        {
            Pitch = -1;
        }

        protected int _pitch;
        public int Pitch
        {
            get { return _pitch; }
            set
            {
                _pitch = value;
                OnPropertyChanged(nameof(Pitch));
            }
        }

        public override void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            base.BuildByteList(bytes, getMode);
            bytes.Add(Pitch == -1 ? (byte)17 : (byte)Pitch);
        }
    }

    public class DMCEntry : SequenceEntry
    {
        public DMCEntry(byte[] data) : base(data)
        {
            byte cmpDMC = data[1];

            if((DMCSamples)cmpDMC == DMCSamples.kNone)
            {
                Sample = DMCSamples.kNone;
                Pitch = 0;
            }
            else
            {
                Pitch = cmpDMC & 0x0F;
                Sample = (DMCSamples)(cmpDMC >> 4);
            }
        }

        public DMCEntry() : base()
        {
            Sample = DMCSamples.kNone;
            Pitch = 0;
        }

        protected DMCSamples _sample;
        public DMCSamples Sample
        {
            get { return _sample; }
            set
            {
                _sample = value;
                OnPropertyChanged(nameof(Sample));
            }
        }

        protected int _pitch;
        public int Pitch
        {
            get { return _pitch; }
            set
            {
                _pitch = value;
                OnPropertyChanged(nameof(Pitch));
            }
        }

        public override void BuildByteList(in List<byte> bytes, GetModes getMode = GetModes.playback)
        {
            base.BuildByteList(bytes, getMode);
            if (Sample == DMCSamples.kNone)
            {
                bytes.Add((byte)Sample);
            }
            else
            {
                bytes.Add((byte)((byte)Sample << 4 | Pitch));
            }
        }
    }
}
