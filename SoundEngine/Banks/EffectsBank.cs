using CatfortSound.SoundEngine.Effects;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Banks
{
    public class EffectData : INotifyPropertyChanged
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
            get{  return loopPoint; }
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

        public EffectData(string name, bool centered = false)
        {
            Length = 16;
            Centered = centered;
            for (int i = 0; i < 256; i++)
            {
                Bytes.Add(0);
            }
            Name = name;
        }

        public EffectData(string name, byte[] bytes, bool centered)
        {
            Centered = centered;
            Name = name;
            Length = bytes.Length;
            byte[] cmds = {0xFF, 0xFE, 0xFD, 0x80, 0x81, 0x82};
            int offset = Centered ? 3 : 0;

            for (int i = 0; i < 256; i++)
            {
                if (i < bytes.Length)
                {
                    if (i > 0 && bytes[i-1] == cmds[offset+2])
                    {
                        Length--;
                        continue;
                    }

                    if (bytes[i] == cmds[offset] || bytes[i] == cmds[offset+1] || bytes[i] == cmds[offset+2])
                    {
                        if (bytes[i] == cmds[offset])
                        {
                            //loop last
                            LoopPoint = -1;
                        }
                        else if( bytes[i] == cmds[offset+1])
                        {
                            //loop all
                            LoopPoint = 0;
                        }
                        else if (bytes[i] == cmds[offset + 2])
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
            if(!canLoop)
            {
                LoopDetails.Add(Centered ? (byte)0x80 : (byte)0xFF);
            }
            else
            {
                if (LoopPoint == Length - 1 || LoopPoint == -1 || LoopPoint > Length)
                {
                    //loop last
                    LoopDetails.Add(Centered? (byte)0x80 : (byte)0xFF);
                }    
                else if(LoopPoint == 0)
                {
                    //loop all
                    LoopDetails.Add(Centered? (byte)0x81 : (byte)0xFE);
                }
                else if(LoopPoint > 0 && LoopPoint < Length - 1)
                {
                    //loop point
                    LoopDetails.Add(Centered? (byte) 0x82 : (byte)0xFD);
                    if(Centered)
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

    public class EffectsBank : INotifyPropertyChanged
    {

        public ObservableCollection<EffectData> VolumeEffects = [];
        public ObservableCollection<EffectData> ModularEffects = [];
        public ObservableCollection<EffectData> ArpeggioEffects = [];
        public ObservableCollection<EffectData> DutyCycleEffects = [];

        public Dictionary<Type, ObservableCollection<EffectData>> Banks;

        #region Default Effects
        public List<byte[]> VolEffects = new()
        {
            new byte[] {15, 14, 13, 12, 9, 5, 0, 0xFF},
            new byte[] {1, 1, 2, 2, 3, 3, 4, 4, 7, 7, 8, 8, 10, 10, 12, 12, 13, 13, 14, 14, 15, 15, 0xFF},
            new byte[] {13, 13, 13, 12, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 6, 5, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 2, 1, 0, 0xFF},
            new byte[] {15, 11, 9, 8, 7, 6, 0, 0xFF},
            new byte[] {11, 11, 10, 9, 8, 7, 6, 6, 6, 5, 0xFF},
            new byte[] {15, 14, 12, 10, 0, 0xFF},
            new byte[] {15, 14, 12, 10, 9, 0xFF},
            new byte[] {9, 8, 6, 4, 0, 0xFF},
            new byte[] {9, 8, 6, 4, 3, 0xFF},
            new byte[] {14, 9, 8, 6, 4, 3, 2, 1, 0, 0xFF},
            new byte[] {14, 6, 2, 0, 0xFF},
            new byte[] {14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0, 0xFF},
            new byte[] {15, 0, 0xFF},
            new byte[] {15, 0xFF},
            new byte[] {14, 15, 15, 15, 9, 9, 9, 14, 14, 14, 8, 8, 8, 13, 13, 13, 7, 7, 7, 12, 12, 12, 6, 6, 6, 11, 11, 11, 5, 5, 5, 10, 10, 10, 4, 4, 4, 9, 9, 9, 3, 3, 3, 8, 8, 8, 0, 0xFF},
            new byte[] {8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 15, 15, 15, 14, 14, 14, 14, 14, 14, 13, 13, 13, 13, 13, 13, 12, 12, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11, 10, 10, 10, 10, 10, 10, 9, 9, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 0, 0xFF},
        };

        public List<byte[]> ModEffects = new()
        {
            new byte[] {0, ModEffect.LOOP_ALL},
            new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10},
            new byte[] {0x10, 0x0C, 0x08, 0x04, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, ModEffect.LOOP_PART, 0x10 },
            new byte[] {0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, ModEffect.LOOP_LAST },
        };

        public List<byte[]> ArpEffects = new()
        {
            new byte[] {0x00, 0x80},
            new byte[] { 0x00, 0x00, 0xF8, 0xF8, 0x80},
            new byte[] { 0x00, 0x03, 0x06, 0x80},
            new byte[] { 0xFB, 0x00, 0x03, 0x80},
            new byte[] { 0x00, 0x04, 0x08, 0x80},
            new byte[] { 0x00, 0x04, 0x07, 0x0B, 0x80},
            new byte[] { 0x00, 0x03, 0x07, 0x0A, 0x80},
            new byte[] { 0x00, 0x03, 0x07, 0x08, 0x80},
            new byte[] { 0xF8, 0x00, 0x07, 0x0C, 0x80},
            new byte[] { 0xFE, 0x00, 0x03, 0x07, 0x80},
            new byte[] { 0xFB, 0xFE, 0x00, 0x04, 0x80},
            new byte[] { 0x00, 0x03, 0x06, 0x09, 0x80},
            new byte[] { 0x00, 0x04, 0x07, 0x0C, 0x80},
        };

        public List<byte[]> DutyEffects = new()
        {
            new byte[] { 0, 0xFF },
            new byte[] { 1, 0xFF },
            new byte[] { 2, 0xFF },
            new byte[] { 3, 0xFF },
        };
        #endregion
        public EffectsBank()
        {
            for(int i = 0; i < VolEffects.Count; i++)
            {
                VolumeEffects.Add(new EffectData($"{nameof(VolEffect)}_Default_{i}", VolEffects[i], false));
            }

            for (int i = 0; i < ModEffects.Count; i++)
            {
                ModularEffects.Add(new EffectData($"{nameof(ModEffect)}_Default_{i}", ModEffects[i], true));
            }

            for (int i = 0; i < ArpEffects.Count; i++)
            {
                ArpeggioEffects.Add(new EffectData($"{nameof(ArpEffect)}_Default_{i}", ArpEffects[i], true));
            }

            for (int i = 0; i < DutyEffects.Count; i++)
            {
                DutyCycleEffects.Add(new EffectData($"{nameof(DutyEffect)}_Default_{i}", DutyEffects[i], false));
            }


            Banks = [];
            Banks.Add(typeof(VolEffect), VolumeEffects);
            Banks.Add(typeof(ModEffect), ModularEffects);
            Banks.Add(typeof(ArpEffect), ArpeggioEffects);
            Banks.Add(typeof(DutyEffect), DutyCycleEffects);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public byte[] GetEffectByType(Type type, int idx)
        {
            bool canLoop = type switch
            {
                Type vol when type == typeof(VolEffect) => false,
                Type mod when type == typeof(ModEffect) => true,
                Type arp when type == typeof(ArpEffect) => false,
                Type dty when type == typeof(DutyEffect) => false,
                _ => false
            };
            return Banks[type][idx].GetEffectBytes(canLoop);
        }
    }
}
