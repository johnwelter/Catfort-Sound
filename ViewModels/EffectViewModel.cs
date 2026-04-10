using CatfortSound.SoundEngine.Banks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace CatfortSound.ViewModels
{
    public class EffectViewModel
    {
        public ObservableCollection<EffectModelData> EffectList { get; set; }

        public EffectViewModel()
        {
            EffectList = new ObservableCollection<EffectModelData>();
        }

        public EffectViewModel(System.Collections.IList startList)
        {
            EffectList = (ObservableCollection<EffectModelData>)startList;
        }

        public void AddEffect(EffectModelData newData)
        {
            EffectList.Add(newData);
        }

        public void RemoveEffect(int Index)
        {
            if(EffectList.Count > Index)
                EffectList.RemoveAt(Index);
        }
    }
}
