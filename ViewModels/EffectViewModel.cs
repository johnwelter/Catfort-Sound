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
        public ObservableCollection<EffectData> EffectList { get; set; }

        public EffectViewModel()
        {
            EffectList = new ObservableCollection<EffectData>();
        }

        public EffectViewModel(System.Collections.IList startList)
        {
            EffectList = (ObservableCollection<EffectData>)startList;
        }

        public void AddEffect(EffectData newData)
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
