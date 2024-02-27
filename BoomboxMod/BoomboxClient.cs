using BoomboxMod.Voice;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BoomboxMod;

public class BoomboxClient
{
    public static readonly int OutBufferSize = 8192;
    public static readonly int InBufferSize = 8388608;

    private readonly NamedPipeClientStream clientStream;
    private readonly StreamWriter streamWriter;
    private readonly BinaryReader binaryReader;


    public float[] buffer = new float[InBufferSize];
    public byte[] audioByteBuffer = new byte[InBufferSize * 2];

    public float audioLengthSeconds = 0.0f;
    public bool connected = false;

    public BoomboxClient()
    {
        try
        {
            this.clientStream = new NamedPipeClientStream("BOOMBOX");
            this.streamWriter = new StreamWriter(clientStream, Encoding.UTF8, OutBufferSize, true);
            this.binaryReader = new BinaryReader(clientStream, Encoding.UTF8, true);
        }
        catch (IOException e)
        {
            BoomboxPlugin.Error(e);
        }
    }

    public void Initialize()
    {
        CreateServer();
        ConnectToServer();
        this.connected = true;
    }

    private void CreateServer()
    {
        Process[] processes = Process.GetProcessesByName("BoomboxSynthesizer");
        foreach (Process process in processes) process.Close();

        string directory = BoomboxPlugin.GetBaseDirectory();
        Process server = Process.Start(directory + "BoomboxSynthesizer.exe");

        if (server == null) BoomboxPlugin.Error("Failed to start BoomboxSynthesizer.");
        else BoomboxPlugin.Log("Started BoomboxSynthesizer");
    }

    public string FilterMessageContent(string input)
    {
        return input.Replace("\r", "").Replace("\n", "").Replace("_", " ");
    }

    private void ConnectToServer()
    {
        BoomboxPlugin.Log("Connecting to server...");
        try
        {
            clientStream.Connect(7500);
        }
        catch (TimeoutException e)
        {
            BoomboxPlugin.Error($"Connection timed out: {e}");
        }
        catch (IOException e)
        {
            BoomboxPlugin.Error($"Failed to connect to server: {e}");
        }
    }

    public MessageData Speak(int playerId, string content, string voice, float volumeScale = 1f)
    {
        if (!connected)
        {
            BoomboxPlugin.Error("Tried to speak before connection established!");
            return default;
        }
        SendMessageToServer(FilterMessageContent(content), voice);
        ClearSamplesBuffer();

        int msgLength = binaryReader.ReadInt32(); //msg length is number of samples 16 bit

        if (msgLength > audioByteBuffer.Length)
        {
           audioByteBuffer = new byte[msgLength];
        }
        Array.Clear(buffer, 0, msgLength);
        int lastNonZeroValueIndex = 0;

        binaryReader.Read(audioByteBuffer, 0, msgLength);
        for (int i = 0; i < Math.Min(msgLength / 2, buffer.Length); i++)
        {
            float nextSample = volumeScale * ((float)BitConverter.ToInt16(audioByteBuffer, i * 2) / 32767f); // convert half -1 to 1 float
            buffer[i] = nextSample;
            if (nextSample != 0f) { lastNonZeroValueIndex = i; }
        }
        var currentAudioLengthInSeconds = (float)lastNonZeroValueIndex / (float)11025; // 11025 hz

        BoomboxPlugin.Log($"Completed: {content}");
        return new MessageData(playerId, buffer, currentAudioLengthInSeconds);
    }

    private void SendMessageToServer(string content, string voice)
    {
        BoomboxPlugin.Log($"Sending to server: {content}");
        
        if (!clientStream.IsConnected)
        {
            this.CreateServer();
            this.ConnectToServer();
        }
        try
        {
            streamWriter.WriteLine($"{voice}={content}");
            streamWriter.Flush();
        }
        catch (Exception error)
        {
            BoomboxPlugin.Error(error);
        }
    }

    private void ClearSamplesBuffer()
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0f;
        }
    }
}
