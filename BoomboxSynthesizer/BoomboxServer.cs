using System;
using System.IO;
using System.IO.Pipes;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;

namespace BoomboxSynthesizer;

public class BoomboxServer
{
    private static readonly SpeechAudioFormatInfo DefaultAudioFormat = new SpeechAudioFormatInfo(11250, AudioBitsPerSample.Sixteen, AudioChannel.Mono);

    public readonly NamedPipeServerStream serverStream;
    public readonly StreamReader reader;
    public readonly BinaryWriter binaryWriter;
    public readonly SpeechSynthesizer synthesizer;

    private bool connectionClosed = false;

    public BoomboxServer()
    {
        this.serverStream = new NamedPipeServerStream("BOOMBOX");
        this.reader = new StreamReader(serverStream);
        this.binaryWriter = new BinaryWriter(serverStream);
        this.synthesizer = new SpeechSynthesizer();
    }

    public void Initialize()
    {
        Console.WriteLine("Connecting to server...");
        this.serverStream.WaitForConnection();
        Console.WriteLine("Successfully connected to server!");

        Listen();
    }

    private void Listen()
    {
        while (!connectionClosed)
        {
            if (!serverStream.IsConnected)
            {
                this.connectionClosed = true;
                break;
            }
            try {
                string data = reader.ReadLine();
                Message msg = Message.Deserialize(data);
                Console.WriteLine($"Received from client: {msg.content}");
                ReceiveMessage(msg);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Server failed to receive message: {e}");
                Disconnect(true);
                continue;
            }
        }
        Disconnect();
    }

    private void ReceiveMessage(Message message)
    {
        if (message.content == null || message.content == "") return;

        byte[] buffer = CreateAudioBuffer(message.content, message.voice);

        Console.WriteLine("Audio buffer generated, sending to client");
        binaryWriter.Write(buffer.Length);
        serverStream.Write(buffer, 0, buffer.Length);
        binaryWriter.Flush();
    }

    private byte[] CreateAudioBuffer(string message, string voice)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            Console.WriteLine("Processing...");
            synthesizer.SetOutputToAudioStream(stream, DefaultAudioFormat);
            synthesizer.SelectVoice(voice);
            synthesizer.Speak(message);
            return stream.ToArray();
        }
    }

    private void Disconnect(bool forced = false)
    {
        if (forced || serverStream.IsConnected)
        {
            serverStream.Disconnect();
        }
    }
}
