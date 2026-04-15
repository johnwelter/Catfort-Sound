using CatfortSound.SoundEngine.DataTables;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.SongData
{
    public class ChannelSettings : INotifyPropertyChanged
    {
        private Lengths delayLength = Lengths._;
        public Lengths DelayLength
        {
            get { return delayLength; }
            set
            {
                delayLength = value;
                OnPropertyChanged(nameof(DelayLength));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
