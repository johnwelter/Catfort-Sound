using CatfortSound.SoundEngine;
using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.Utilities
{

    public struct HeaderInfo
    {
        public Streams stream;
        public byte status;
        public ChannelIndexes channel;
        public byte duty;
        public byte volume;
        public byte tempo; 
    }

    public interface IExportable
    {
        protected const string BYTE_TAG = "    .byte";
        protected const string WORD_TAG = "    .word";
        protected static string GetTag(Type tagType)
        {
            if (tagType == typeof(string))
            {
                return WORD_TAG;
            }
            else if (tagType == typeof(byte))
            {
                return BYTE_TAG;
            }
            else
            {
                return $"ERRORTYPE: {tagType.ToString()}";
            }
        }
        protected static string GetDataString<T>(T datum)
        {
            return datum switch
            {
                string s => s,
                byte b => b.ToString("X2"),
                _ => "ERROR"
            };
        }

        int MaxRowLength { get; }

        public static string AccumulateRows<T>(T[] data, int maxLength, ref int runningCount)
        {
            string rowTag = GetTag(typeof(T));
            string row = "";
            if (runningCount == 0)
            {
                row += rowTag;
            }
            else
            {
                row += ",";
            }

            for (int i = 0; i < data.Length; i++)
            {
                row += $" ${GetDataString(data[i])}";
                runningCount = (runningCount + 1) % maxLength;
                if (runningCount == 0)
                {
                    row += "\n";
                }

                if (i != data.Length - 1)
                {
                    row += runningCount == 0 ? rowTag : ",";
                }
            }
            return row;

        }

        public static string MakeRow<T>(T[] data)
        {
            string row = $"{GetTag(typeof(T))} ";
            for(int i = 0; i < data.Length; i++)
            {
                row += $" ${GetDataString(data[i])}";
                if(i != data.Length-1)
                {
                    row += ",";
                }
            }
            row += "\n";
            return row;
        }

        public static string MakeByte(string byteData)
        {
            return $"{BYTE_TAG} {byteData}\n";
        }

        public static string MakeByte(byte byteData)
        {
            return MakeByte($"${byteData.ToString("X2")}");
        }

        public static string MakeWord(string word)
        {
            return $"{WORD_TAG} {word}\n";
        }
        void Init(string title);
        string GetOutput();
    }
}
