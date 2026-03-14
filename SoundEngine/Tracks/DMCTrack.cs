using CatfortSound.SoundEngine.Sequence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatfortSound.SoundEngine.Tracks
{
    public class DMCTrack : Track
    {
        public DMCTrack(Sequencer? parent, int targetChannel) : base(parent, targetChannel)
        {

        }
        public override void ProcessNote(int note)
        {
            if (Sequencer is null)
            {
                return;
            }
            Sequencer.APU?.TriggerDMC(note);
        }

        public override void ParseInstruction(int val)
        {
            //DMC ignores instructions... for now
            ProcessNote(val);
            IncSeqIndex();
        }
    }
}
