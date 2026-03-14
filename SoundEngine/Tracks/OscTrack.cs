using CatfortSound.SoundEngine.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Tracks
{
    public class OscTrack : Track
    {
        public OscTrack(Sequencer? parent, int targetChannel) : base(parent, targetChannel)
        {
        }
    }
}
