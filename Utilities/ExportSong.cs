using CatfortSound.SoundEngine;
using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using CatfortSound.SoundEngine.SongData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.Utilities
{
    public class ExportSong : IExportable
    {
        private string songName = "";

        private string header = "";

        int channelCount = 0;
        private string GetChannelLabel(ChannelIndexes channel) => $"{songName}_{channel.ToString().ToLower()}";

        private Dictionary<ChannelIndexes, string> channelStrings = [];

        public int MaxRowLength { get => 16; }


        public void AddChannel(HeaderInfo headerInfo, List<byte[]> data, ObservableCollection<Subloop> loopData, ChannelSettings channelSettings)
        {
            channelCount++;
            header += "\n";
            header += IExportable.MakeByte(headerInfo.stream.ToString());
            header += IExportable.MakeByte(headerInfo.status);

            if (headerInfo.status == 0)
            {
                return;
            }

            //set up for delay stuff
            string channelLabel = GetChannelLabel(headerInfo.channel);
            string startLabel = channelLabel;
            string channelStarter = $"{channelLabel}:\n";
            if(channelSettings.DelayLength != SoundEngine.DataTables.Lengths._)
            {
                byte delayLength = (byte)channelSettings.DelayLength;
                startLabel = $"{channelLabel}_delay";
                string delayInject = $"{startLabel}:\n";
                delayInject += IExportable.MakeRow(new byte[] { delayLength, (byte)NoteConstants.Rest });
                channelStarter = $"{delayInject}\n{channelStarter}";
            }

            header += IExportable.MakeByte(headerInfo.channel.ToString());
            header += IExportable.MakeByte(headerInfo.duty);
            header += IExportable.MakeByte(headerInfo.volume);
            header += IExportable.MakeWord(startLabel);
            header += IExportable.MakeByte(headerInfo.tempo);



            channelStrings.Add(headerInfo.channel, channelStarter);

            //TODO: this is gross and I hate it, but it works fine. clean it up

            int loopIndex = loopData.Count > 0 ? 0 : -1;
            bool buildingLoop = false;
            string musicData = "";
            int runningCount = 0;
            for (int i = 0; i < data.Count; i++)
            {
                if (loopIndex >= 0)
                {
                    if (!buildingLoop && i == loopData[loopIndex].loopStartIndex)
                    {
                        //start subloop
                        runningCount = 0;
                        if (musicData.Length > 0 && musicData[musicData.Length - 1] != '\n')
                        {
                            musicData += "\n";
                        }
                        musicData += IExportable.MakeRow(new byte[] { (byte)Instructions.SetLoop1_Counter, (byte)loopData[loopIndex].loopCount });
                        musicData += $"{channelLabel}_sublp_{loopIndex + 1}:\n";
                        buildingLoop = true;
                    }
                }

                musicData += IExportable.AccumulateRows(data[i], MaxRowLength, ref runningCount);

                if (buildingLoop && i == loopData[loopIndex].loopEndIndex)
                {
                    //end subloop
                    runningCount = 0;
                    musicData += "\n";
                    musicData += IExportable.MakeByte((byte)Instructions.Loop1);
                    musicData += IExportable.MakeWord($"{channelLabel}_sublp_{loopIndex + 1}");
                    buildingLoop = false;
                    loopIndex++;
                    if (loopIndex == loopData.Count)
                    {
                        //if we're out of loops, stop checking
                        loopIndex = -1;
                    }
                }
                else if (i == data.Count - 1)
                {
                    musicData += "\n";
                }


            }
            channelStrings[headerInfo.channel] += musicData;
            AddEndLoop(headerInfo.channel);

        }

        public void AddEndLoop(ChannelIndexes channel)
        {
            channelStrings[channel] += IExportable.MakeByte((byte)Instructions.Loop);
            channelStrings[channel] += IExportable.MakeWord(GetChannelLabel(channel));
        }
        public void Init(string title)
        {
            /*
             *  song parts:
             *  HEADER 
             *      - contains song title that we will reference in game
             *      - contians stream data for all relevant channels
             *  CHANNELS
             *      - contains stream data per channel, including subloops
             */

            //reset export output to just the header
            songName = title;
            channelCount = 0;
            channelStrings.Clear();
        }

        public string GetOutput()
        {
            string output = songName + "_header:\n";
            output += IExportable.MakeByte((byte)channelCount);
            output += header + "\n";

            foreach (string channelOutput in channelStrings.Values)
            {
                output += channelOutput + "\n";
            }
            return output;
        }

    }
}
