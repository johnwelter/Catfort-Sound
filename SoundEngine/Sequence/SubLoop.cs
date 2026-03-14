using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Sequence
{
    public class Subloop
    {
        public Subloop(int startIndex, int endIndex, int count)
        {
            loopStartIndex = startIndex;
            loopEndIndex = endIndex;
            loopCount = count;
        }

        public Subloop()
        {
            loopStartIndex = -1;
            loopEndIndex = -1;
            loopCount = 0;
        }
        public int loopStartIndex { get; set; } // where to return when the loop instrtuction is played
        public int loopEndIndex { get; set; } // where to insert the loop instruction 
        public int loopCount { get; set; } //how many times to loop before skipping the loop instruction

        public byte[] GetLoopDataBytes()
        {
            List<byte> loopData = new List<byte>();
            loopData.AddRange(BitConverter.GetBytes(loopStartIndex));
            loopData.AddRange(BitConverter.GetBytes(loopEndIndex));
            loopData.AddRange(BitConverter.GetBytes(loopCount));

            return loopData.ToArray();
        }
    }
}
