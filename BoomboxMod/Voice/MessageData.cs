using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoomboxMod.Voice;
public readonly struct MessageData
{
    public readonly int playerId;
    public readonly float[] buffer; 
    public readonly float lengthSeconds;

    public MessageData(int playerId, float[] buffer, float lengthSeconds)
    {
        this.playerId = playerId;
        this.buffer = buffer;
        this.lengthSeconds = lengthSeconds;
    }
}
