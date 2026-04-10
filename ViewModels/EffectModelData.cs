using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Effects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CatfortSound.ViewModels
{
    public class EffectModelData : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private int length;
        public int Length
        {
            get { return length; }
            set
            {
                length = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        private ObservableCollection<byte> bytes = [];
        public ObservableCollection<byte> Bytes
        {
            get { return bytes; }
            set
            {
                bytes = value; OnPropertyChanged(nameof(Bytes));
            }
        }

        private int loopPoint = -1;
        public int LoopPoint
        {
            get { return loopPoint; }
            set { loopPoint = value; OnPropertyChanged(nameof(LoopPoint)); }
        }

        //set up for completeness, but this really shouldn't be tampered with
        private bool centered = false;
        public bool Centered
        {
            get { return centered; }
            set
            {
                centered = value; OnPropertyChanged(nameof(Centered));
            }
        }

        public EffectModelData(string name, bool centered = false)
        {
            Length = 16;
            Centered = centered;
            for (int i = 0; i < 256; i++)
            {
                Bytes.Add(0);
            }
            Name = name;
        }

        public EffectModelData(string name, byte[] bytes, bool centered)
        {
            Centered = centered;
            Name = name;
            Length = bytes.Length;

            //don't count loop data toward length of effect, sine it's stored outside 

            for (int i = 0; i < 256; i++)
            {
                if (i < bytes.Length)
                {
                    if (i > 0 && bytes[i - 1] == Effect.LOOP_PART(Centered))
                    {
                        Length--;
                        continue;
                    }

                    if (Effect.IsLoopType(bytes[i], Centered))
                    {
                        if (bytes[i] == Effect.LOOP_LAST(Centered))
                        {
                            //loop last
                            LoopPoint = -1;
                        }
                        else if (bytes[i] == Effect.LOOP_ALL(Centered))
                        {
                            //loop all
                            LoopPoint = 0;
                        }
                        else if (bytes[i] == Effect.LOOP_PART(Centered))
                        {
                            //loop part
                            LoopPoint = (Length - 2) - bytes[i + 1];
                        }
                        Length--;

                        continue;
                    }
                }

                Bytes.Add((i < bytes.Length) ? bytes[i] : (byte)0);
            }
        }

        public byte[] GetEffectBytes(bool canLoop)
        {
            List<byte> LoopDetails = [];
            if (!canLoop)
            {
                LoopDetails.Add(Effect.LOOP_LAST(Centered));
            }
            else
            {
                if (LoopPoint == Length - 1 || LoopPoint == -1 || LoopPoint > Length)
                {
                    //loop last
                    LoopDetails.Add(Effect.LOOP_LAST(Centered));
                }
                else if (LoopPoint == 0)
                {
                    //loop all
                    LoopDetails.Add(Effect.LOOP_ALL(Centered));
                }
                else if (LoopPoint > 0 && LoopPoint < Length - 1)
                {
                    //loop point
                    LoopDetails.Add(Effect.LOOP_PART(Centered));
                    if (Centered)
                    {
                        LoopDetails.Add((byte)(Length - LoopPoint));
                    }
                }
            }

            List<byte> outBytes = Bytes.Take(Length).ToList();
            outBytes.AddRange(LoopDetails);
            return outBytes.ToArray();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
