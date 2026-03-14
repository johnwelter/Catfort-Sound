using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
using System.Collections.Generic;
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

    public class Exporter
    {
        private string songName;

        private string header;

        private int maxLength = 16;

        private string GetChannelLabel(ChannelIndexes channel) => $"{songName}_{channel.ToString().ToLower()}";

        private Dictionary<ChannelIndexes, string> channelStrings = [];
        public Exporter(string songName) 
        {
            this.songName = songName;
            header = "";
        }

        public void InitExport(byte streamCount)
        {
            //reset export output to just the header
            header = songName + "_header:\n";
            header += MakeByte(streamCount);
            channelStrings.Clear();
        }
        public void AddByteRows(byte[] bytes, int length, ChannelIndexes channel)
        {
            channelStrings[channel] += MakeByteRows(bytes, length);
        }

        public void AddEndLoop(ChannelIndexes channel)
        {
            channelStrings[channel] += MakeByte((byte)Instructions.Loop);
            channelStrings[channel] += MakeWord(GetChannelLabel(channel));
        }

        public string MakeByteRows(byte[] bytes, int length)
        {
            string row = "    .byte";
            int count = 0;
            foreach (byte b in bytes)
            {
                row += $" ${b.ToString("X2")}";
                count++;
                if(count % length == 0 && count != bytes.Length)
                {
                    row += "\n";
                    row += "    .byte";
                }
                else if(count != bytes.Length)
                {
                    row += ",";
                }
            }
            row += "\n";
            return row;
        }

        public string MakeByte(string byteData)
        {
            return $"    .byte {byteData}\n";
        }

        public string MakeByte(byte byteData)
        {
            return MakeByte($"${byteData.ToString("X2")}");
        }

        public string MakeWord(string word)
        {
            return $"    .word {word}\n";
        }
        public void AddChannel(HeaderInfo headerInfo, byte[] data)
        {
            header += "\n";
            header += MakeByte(headerInfo.stream.ToString());
            header += MakeByte(headerInfo.status);

            if(headerInfo.status == 0)
            {
                return;
            }

            header += MakeByte(headerInfo.channel.ToString());
            header += MakeByte(headerInfo.duty);
            header += MakeByte(headerInfo.volume);
            string channelLabel = GetChannelLabel(headerInfo.channel);
            header += MakeWord(channelLabel);
            header += MakeByte(headerInfo.tempo);

            channelStrings.Add(headerInfo.channel, $"{channelLabel}:\n");

            AddByteRows(data, maxLength, headerInfo.channel);
            AddEndLoop(headerInfo.channel);
            
        }

        public string GetOutput()
        {
            string output = header + "\n";
            foreach(string channelOutput in channelStrings.Values)
            {
                output += channelOutput + "\n";
            }
            return output;
        }
    }
}
