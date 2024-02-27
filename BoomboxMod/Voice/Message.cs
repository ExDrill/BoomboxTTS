namespace BoomboxMod.Voice;
public class Message
{
    public readonly int playerId;
    public readonly string content;
    
    public Message(int playerId, string content)
    {
        this.playerId = playerId;
        this.content = content;
    }
}