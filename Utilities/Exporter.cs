using CatfortSound.SoundEngine;
using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using System;
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

        public void AddEndLoop(ChannelIndexes channel)
        {
            channelStrings[channel] += MakeByte((byte)Instructions.Loop);
            channelStrings[channel] += MakeWord(GetChannelLabel(channel));
        }

        public string MakeByteRows(byte[] bytes, int length, ref int runningCount)
        {
            string row = "";
            if(runningCount == 0)
            {
                row += "    .byte";
            }
            else 
            {
                row += ",";
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                row += $" ${bytes[i].ToString("X2")}";
                runningCount = (runningCount + 1) % length;
                if(runningCount == 0)
                {
                    row += "\n";
                }

                if(i != bytes.Length - 1)
                {
                    row += runningCount == 0 ? "    .byte" : ",";
                }
            }
            return row;
        }

        public string MakeBytes(byte[] byteData)
        {
            string row = $"    .byte ";
            for(int i = 0; i < byteData.Length; i++)
            {
                row += $" ${byteData[i].ToString("X2")}";
                if(i != byteData.Length-1)
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
        public void AddChannel(HeaderInfo headerInfo, List<byte[]> data, ObservableCollection<Subloop> loopData)
        {
            header += "\n";
            header += MakeByte(headerInfo.stream.ToString());
            header += MakeByte(headerInfo.status);

            if (headerInfo.status == 0)
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

            //TODO: this is gross and I hate it, but it works fine. clean it up

            int loopIndex = loopData.Count > 0 ? 0 : -1;
            bool buildingLoop = false;
            string musicData = "";
            int runningCount = 0;
            for (int i = 0; i < data.Count; i++)
            {
                if(loopIndex >= 0)
                {
                    if(!buildingLoop && i == loopData[loopIndex].loopStartIndex)
                    {
                        //start subloop
                        runningCount = 0;
                        if (musicData.Length > 0 && musicData[musicData.Length-1] != '\n')
                        {
                            musicData += "\n";
                        }
                        musicData += MakeBytes(new byte[] { (byte)Instructions.SetLoop1_Counter, (byte)loopData[loopIndex].loopCount });
                        musicData += $"{channelLabel}_sublp_{loopIndex + 1}:\n";
                        buildingLoop = true;
                    }
                }

                musicData += MakeByteRows(data[i], maxLength, ref runningCount);

                if (buildingLoop && i == loopData[loopIndex].loopEndIndex)
                {
                    //end subloop
                    runningCount = 0;
                    musicData += "\n";
                    musicData += MakeByte((byte)Instructions.Loop1);
                    musicData += MakeWord($"{channelLabel}_sublp_{loopIndex + 1}");
                    buildingLoop = false;
                    loopIndex++;
                    if(loopIndex == loopData.Count)
                    {
                        //if we're out of loops, stop checking
                        loopIndex = -1;
                    }
                }
                else if(i == data.Count-1)
                {
                    musicData += "\n";
                }


            }
            channelStrings[headerInfo.channel] += musicData;
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
