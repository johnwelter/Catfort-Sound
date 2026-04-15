using CatfortSound.SoundEngine.Effects;
using CatfortSound.Utilities;
using CatfortSound.ViewModels;
using Melanchall.DryWetMidi.Interaction;
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
using static CatfortSound.SoundEngine.Effects.EffectStack;

namespace CatfortSound.SoundEngine.Banks
{

    public class EffectsBank : INotifyPropertyChanged
    {
        public ObservableCollection<EffectModelData> VolumeEffects = [];
        public ObservableCollection<EffectModelData> ModularEffects = [];
        public ObservableCollection<EffectModelData> ArpeggioEffects = [];
        public ObservableCollection<EffectModelData> DutyCycleEffects = [];

        public ObservableCollection<EffectModelData>[] Banks = new ObservableCollection<EffectModelData>[4];

        //TODO: Move default effects into some kind of embeded file
        #region Default Effects
        public List<byte[]> VolEffects =
        [
            new byte[] {15, 14, 13, 12, 9, 5, 0, (byte)LoopTypes.Last},
            new byte[] {1, 1, 2, 2, 3, 3, 4, 4, 7, 7, 8, 8, 10, 10, 12, 12, 13, 13, 14, 14, 15, 15, (byte)LoopTypes.Last},
            new byte[] {13, 13, 13, 12, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 6, 6, 6, 5, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 2, 1, 0, (byte)LoopTypes.Last},
            new byte[] {15, 11, 9, 8, 7, 6, 0, (byte)LoopTypes.Last},
            new byte[] {11, 11, 10, 9, 8, 7, 6, 6, 6, 5, (byte)LoopTypes.Last},
            new byte[] {15, 14, 12, 10, 0, (byte)LoopTypes.Last},
            new byte[] {15, 14, 12, 10, 9, (byte)LoopTypes.Last},
            new byte[] {9, 8, 6, 4, 0, (byte)LoopTypes.Last},
            new byte[] {9, 8, 6, 4, 3, (byte)LoopTypes.Last},
            new byte[] {14, 9, 8, 6, 4, 3, 2, 1, 0, (byte)LoopTypes.Last},
            new byte[] {14, 6, 2, 0, (byte)LoopTypes.Last},
            new byte[] {14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0, (byte)LoopTypes.Last},
            new byte[] {15, 0, (byte)LoopTypes.Last},
            new byte[] {15, (byte)LoopTypes.Last},
            new byte[] {14, 15, 15, 15, 9, 9, 9, 14, 14, 14, 8, 8, 8, 13, 13, 13, 7, 7, 7, 12, 12, 12, 6, 6, 6, 11, 11, 11, 5, 5, 5, 10, 10, 10, 4, 4, 4, 9, 9, 9, 3, 3, 3, 8, 8, 8, 0, (byte)LoopTypes.Last},
            new byte[] {8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 15, 15, 15, 14, 14, 14, 14, 14, 14, 13, 13, 13, 13, 13, 13, 12, 12, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11, 10, 10, 10, 10, 10, 10, 9, 9, 9, 9, 9, 9, 8, 8, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 0, (byte)LoopTypes.Last},
        ];

        public List<string> VolEffectNames =
        [
            "ve_short_staccato",
            "ve_fade_in",
            "ve_blip_echo",
            "ve_tgl_1",
            "ve_tgl_2",
            "ve_battlekid_1",
            "ve_battlekid_1b",
            "ve_battlekid_2",
            "ve_battlekid_2b",
            "ve_drum_decay",
            "ve_hiHat_decay",
            "ve_long_decay",
            "ve_tinyDecy",
            "ve_noDecay",
            "ve_long_tremelo",
            "ve_veryLong_decay",
        ];

        public List<byte[]> ModEffects =
        [
            new byte[] {0, (byte)LoopTypes.cAll},
            new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, (byte)LoopTypes.cPart, 0x10},
            new byte[] {0x10, 0x0C, 0x08, 0x04, 0x00, 0x00, 0xFE, 0xFE, 0xFC, 0xFC, 0xFE, 0xFE, 0x00, 0x00, 0x02, 0x02, 0x04, 0x04, 0x02, 0x02, (byte)LoopTypes.cPart, 0x10 },
            new byte[] {0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, (byte)LoopTypes.cLast },
        ];

        public List<string> ModEffectNames =
        [
            "me_none",
            "me_mod",
            "me_sweep",
            "me_bassKick",
        ];

        public List<byte[]> ArpEffects =
        [
            new byte[] {0x00, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x00, 0xF8, 0xF8, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x03, 0x06, (byte)LoopTypes.cAll},
            new byte[] { 0xFB, 0x00, 0x03, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x04, 0x08, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x04, 0x07, 0x0B, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x03, 0x07, 0x0A, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x03, 0x07, 0x08, (byte)LoopTypes.cAll},
            new byte[] { 0xF8, 0x00, 0x07, 0x0C, (byte)LoopTypes.cAll},
            new byte[] { 0xFE, 0x00, 0x03, 0x07, (byte)LoopTypes.cAll},
            new byte[] { 0xFB, 0xFE, 0x00, 0x04, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x03, 0x06, 0x09, (byte)LoopTypes.cAll},
            new byte[] { 0x00, 0x04, 0x07, 0x0C, (byte)LoopTypes.cAll},
        ];

        public List<string> ArpEffectNames =
        [
            "arp_none",
            "arp_lowerThird",
            "arp_dimTriad",
            "arp_minInvn1",
            "arp_sharp5",
            "arp_maj7",
            "arp_min7",
            "arp_minFlat6",
            "arp_spread5thlowerThird",
            "arp_min7Invn1",
            "arp_dom7Invn2",
            "arp_dim7",
            "arp_majPlusOct",
        ];


        public List<byte[]> DutyEffects =
        [
            new byte[] { 0, (byte)LoopTypes.Last },
            new byte[] { 1, (byte)LoopTypes.Last },
            new byte[] { 2, (byte)LoopTypes.Last },
            new byte[] { 3, (byte)LoopTypes.Last },
        ];
        #endregion
        public EffectsBank()
        {
            for(int i = 0; i < VolEffects.Count; i++)
            {
                VolumeEffects.Add(new EffectModelData(VolEffectNames[i], VolEffects[i], false));
            }

            for (int i = 0; i < ModEffects.Count; i++)
            {
                ModularEffects.Add(new EffectModelData(ModEffectNames[i], ModEffects[i], true));
            }

            for (int i = 0; i < ArpEffects.Count; i++)
            {
                ArpeggioEffects.Add(new EffectModelData(ArpEffectNames[i], ArpEffects[i], true));
            }

            for (int i = 0; i < DutyEffects.Count; i++)
            {
                DutyCycleEffects.Add(new EffectModelData($"Duty_Default_{i}", DutyEffects[i], false));
            }

            Banks[0] = VolumeEffects;
            Banks[1] = DutyCycleEffects;
            Banks[2] = ModularEffects;
            Banks[3] = ArpeggioEffects;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Effect? GetEffectByType(int type, int idx)
        {
            // TODO: this is a hack while the NES engine is in progress to make sure we don't have invalid effects 
            bool canLoop = type >= 2;
            EffectModelData data = Banks[type][idx];
            return new Effect(data.GetEffectBytes(canLoop), data.Centered);
        }

        public (string tables, string indicies) GenerateExportFile(int type, out string fileName)
        {
            ExportEffectList exEffect = new();
            bool canLoop = type >= 2;

            fileName = ((EffectSlots)type).ToString();
            exEffect.Init(fileName);

            foreach (EffectModelData effect in Banks[type])
            {
                exEffect.AddEffect(effect, canLoop);
            }

            return (exEffect.GetOutput(), exEffect.GetIndexList());
        }
    }
}
