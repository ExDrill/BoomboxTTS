using System;

namespace BoomboxSynthesizer;

public class Message
{
    private const string ZiraPrefix = "zira=";
    private const string DavidPrefix = "david=";

    public string content;
    public string voice;

    protected Message(string content, string voice)
    {
        this.content = content;
        this.voice = voice;
    }

    public static Message Deserialize(string data)
    {
        Console.WriteLine(data);
        if (data.StartsWith(ZiraPrefix))
        {
            return new Message(data.Remove(0, ZiraPrefix.Length), "Microsoft Zira Desktop");
        }
        else if (data.StartsWith(DavidPrefix))
        {
            return new Message(data.Remove(0, DavidPrefix.Length), "Microsoft David Desktop");
        }
        else return new Message(data, "Microsoft David Desktop");
    }
}
