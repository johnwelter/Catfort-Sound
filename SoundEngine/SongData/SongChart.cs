using CatfortSound.SoundEngine.DataTables;
using CatfortSound.SoundEngine.Sequence;
using CatfortSound.Utilities;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.SongData
{
    public class SongChart : INotifyPropertyChanged
    {

        private int tempo = 0x3A;
        public int Tempo
        {
            get { return tempo; }
            set
            {
                tempo = value;
                OnPropertyChanged(nameof(Tempo));
            }
        }

        public int LockedTempo;

        public System.Collections.IList[] Channels =
        [
            new ObservableCollection<PulseEntry>(),
            new ObservableCollection<PulseEntry>(),
            new ObservableCollection<OscEntry>(),
            new ObservableCollection<NoiseEntry>(),
            new ObservableCollection<DMCEntry>(),
        ];

        public ObservableCollection<Subloop>[] Subloops =
        [
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
            new ObservableCollection<Subloop>(),
        ];

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LockTempo()
        {
            LockedTempo = Tempo;
        }

        public void Clear()
        {
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i].Clear();
                Subloops[i].Clear();
            }
        }

        public int GetChannelLength(int idx)
        {
            PropertyInfo? property = Channels[idx].GetType().GetProperty("Count");
            return (int?)property?.GetValue(Channels[idx]) ?? 0;
        }

        public byte[] GetEntry(int channel, int entry)
        {
            return GetChannelBytes(Channels[channel], GetModes.playback, entry);
        }

        public byte[] GenerateSaveFileBuffer()
        {
            List<byte> SerializedChart = new();
            SerializedChart.Add((byte)Tempo);
            for (int i = 0; i < Channels.Length; i++)
            {
                SerializedChart.AddRange(BitConverter.GetBytes(GetChannelLength(i)));
                SerializedChart.AddRange(GetChannelBytes(Channels[i], GetModes.save));
                SerializedChart.AddRange(BitConverter.GetBytes(Subloops[i].Count));
                foreach (Subloop loop in Subloops[i])
                {
                    SerializedChart.AddRange(loop.GetLoopDataBytes());
                }
            }
            return SerializedChart.ToArray();
        }

        public string GenerateExportFile(string songName)
        {
            Exporter exporter = new(songName);

            exporter.InitExport((byte)Channels.Length);

            foreach (ChannelIndexes channel in Enum.GetValues(typeof(ChannelIndexes)))
            {
                byte[] channelData = GetChannelBytes(Channels[(int)channel], GetModes.export);
                HeaderInfo headerInfo = new HeaderInfo();
                headerInfo.stream = (Streams)channel;
                headerInfo.status = channelData.Length > 0 ? (byte)1 : (byte)0;
                headerInfo.channel = channel;

                byte duty = channel switch
                {
                    ChannelIndexes.SQUARE_1 => (byte)0x30,
                    ChannelIndexes.SQUARE_2 => (byte)0x30,
                    ChannelIndexes.TRIANGLE => (byte)0x80,
                    ChannelIndexes.NOISE => (byte)0x30,
                    _ => 0
                };

                headerInfo.duty = duty;
                headerInfo.volume = 0;
                headerInfo.tempo = (byte)Tempo;

                exporter.AddChannel(headerInfo, channelData);
            }

            return exporter.GetOutput();
        }

        public int GetNextInt(byte[] buffer, ref int index)
        {
            byte[] intArray = new byte[4];
            Array.Copy(buffer, index, intArray, 0, 4);
            index += 4;
            return BitConverter.ToInt32(intArray, 0);
        }

        public void LoadSaveFileBuffer(byte[] buffer)
        {

            Clear();
            //TODO - make this more... not like this.
            int[] widths = [6, 6, 5, 3, 2];
            int loopWidth = 3 * sizeof(int);

            int bufferIdx = 1;
            int chunkLength = 0;

            Tempo = buffer[0];
            for (int i = 0; i < Channels.Length; i++)
            {
                //chunk 1 = tracker data
                chunkLength = GetNextInt(buffer, ref bufferIdx) * widths[i];
                byte[] chunkArray = new byte[chunkLength];
                Array.Copy(buffer, bufferIdx, chunkArray, 0, chunkArray.Length);
                bufferIdx += chunkLength;
                ReadEntryBytes(Channels[i], chunkArray, widths[i]);

                //chunk 2 = loop data - always 3 bytes long each
                chunkLength = GetNextInt(buffer, ref bufferIdx) * loopWidth;
                int maxIndex = bufferIdx + chunkLength;
                while (bufferIdx < maxIndex)
                {
                    int start = GetNextInt(buffer, ref bufferIdx);
                    int end = GetNextInt(buffer, ref bufferIdx);
                    int count = GetNextInt(buffer, ref bufferIdx);
                    Subloops[i].Add(new Subloop(start, end, count));
                }
            }
        }

        public static void ReadEntryBytes(System.Collections.IList entryList, byte[] bytes, int width)
        {
            for (int i = 0; i < bytes.Length; i += width)
            {
                byte[] shortArray = new byte[width];
                Array.Copy(bytes, i, shortArray, 0, width);
                var itemType = entryList.GetType().GetGenericArguments().Single();
                ConstructorInfo? ctor = itemType?.GetConstructor(new Type[] { typeof(byte[]) });
                var instance = ctor?.Invoke(new object[] { shortArray });
                entryList.Add(instance);
            }
        }

        private static byte[] GetChannelBytes(System.Collections.IList entryList, GetModes getMode, int idx = -1)
        {
            if (entryList.Count == 0)
            {
                return [];
            }

            if (getMode == GetModes.playback)
            {
                try
                {
                    var entry = entryList[idx] as SequenceEntry;
                    return entry?.GetEntryBytes() ?? [];
                }
                catch
                {
                    Debug.WriteLine("missing index for playback, defaulting to empty");
                    return [];
                }
            }

            List<byte> cumlulativeBytes = [];

            for (int i = 0; i < entryList.Count; i++)
            {
                SequenceEntry? item = entryList[i] as SequenceEntry;
                cumlulativeBytes.AddRange(item?.GetEntryBytes(getMode) ?? []);
            }
            return cumlulativeBytes.ToArray();
        }

    }
}
