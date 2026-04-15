using CatfortSound.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CatfortSound.Utilities
{
    internal class ExportEffectList : IExportable
    {
        public int MaxRowLength => 16;

        public string effectType = "";

        private List<string> effectTitles = [];
        private List<string> effectBytes = [];

        public void Init(string title)
        {
            effectType = title;
            effectTitles.Clear();
            effectBytes.Clear();
        }

        public void AddEffect(EffectModelData effect, bool canLoop)
        {
            effectTitles.Add(effect.Name);
            int count = 0;
            effectBytes.Add(IExportable.AccumulateRows(effect.GetEffectBytes(canLoop), MaxRowLength, ref count));
        }

        public string GetOutput()
        {
            string output = $"{effectType}:\n";
            string list = "";
            string effects = "";

            for (int i = 0; i < effectTitles.Count; i++)
            {
                var title = $"se_{effectTitles[i]}";
                list += IExportable.MakeWord(title);
                effects += $"{title}:\n";
                effects += $"{effectBytes[i]}\n\n";
            }

            output += list;
            output += "\n";
            output += effects;
            output += "\n";
            output += "\n";
            return output;
        }

        public string GetIndexList()
        {
            string output = "";
            for (int i = 0; i < effectTitles.Count; i++)
            {
                output += $"{effectTitles[i]} = ${i.ToString("X2")}\n";
            }
            output += "\n";
            return output;

        }
    }
}
