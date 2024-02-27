using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using BoomboxMod.Voice;
using Unity.Netcode;
using LethalNetworkAPI;

namespace BoomboxMod;

[HarmonyPatch]
public class Patches
{
    private static string lastSentMessage = "";

    [PublicNetworkVariable]
    public static LethalNetworkVariable<Dictionary<string, string>> UserToVoice = new("boomboxUserToVoice")
    {
        Value = new()
    };

    private static float[] emptyBuffer = new float[BoomboxClient.InBufferSize];

    private static readonly List<Message> Queue = new List<Message>();
    private static Task<MessageData> currentTask = null;

    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    [HarmonyPostfix]
    public static void AddPlayerChatMessageClientRpcPostfix(HUDManager __instance, string chatMessage, int playerId)
    {
        PlayerControllerB[] players = GetPlayers();
        if (playerId > players.Length || players[playerId] == null) return;
        
        PlayerControllerB player = players[playerId];

        if (VoiceChanged(player, chatMessage)) return;
        BoomboxPlugin.Log(player.playerUsername);

        if (!UserToVoice.Value.ContainsKey(player.playerUsername)) UserToVoice.Value[player.playerUsername] = "none";
        if (UserToVoice.Value[player.playerUsername] == "none") return;

        if (!BoomboxPlugin.IsNiceChatLoaded)
        {
            if (lastSentMessage == chatMessage) return;
            lastSentMessage = chatMessage;
        }
        if (chatMessage.StartsWith(BoomboxConfig.SilencePrefix.Value)) return;

        Queue.Add(new Message(playerId, chatMessage));
    }

    [HarmonyPatch(typeof(HUDManager), "Update")]
    [HarmonyPostfix]
    public static void UpdatePostfix(HUDManager __instance)
    {
        if (currentTask != null)
        {
            if (!currentTask.IsCompleted) { return; }

            if (!currentTask.IsCanceled && !currentTask.IsFaulted)
            {
                Speak(__instance.playersManager, currentTask.Result);
            }

            currentTask = null;
        }
        if (Queue.Count > 0)
        {
            Message next = Queue[0];
            PlayerControllerB player = __instance.playersManager.allPlayerScripts[next.playerId];

            Queue.RemoveAt(0);
            currentTask = Task.Run(() => BoomboxPlugin.Client.Speak(next.playerId, next.content, UserToVoice.Value[player.playerUsername], 1.0f));
        }
    }

    public static void Speak(StartOfRound playerManager, MessageData data)
    {
        PlayerControllerB player = playerManager.allPlayerScripts[data.playerId];
        if (player == null)
        {
            BoomboxPlugin.Warn("Couldn't find player");
            return;
        }
        BoomboxPlugin.Log("Found player");
        GameObject speaker = GetOrCreateSpeaker(player);

        AudioSource audioSource = speaker.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            BoomboxPlugin.Error("Failed to speak, audio source not found!");
            return;
        }
        if (audioSource.clip == null)
        {
            audioSource.clip = AudioClip.Create("BOOMBOX_CLIP", BoomboxClient.InBufferSize, 1, 11025, false);
        }
        BoomboxPlugin.Log("Adding sample to clip.");
        audioSource.clip.SetData(emptyBuffer, 0);
        audioSource.clip.SetData(data.buffer, 0);

        if (SoundManager.Instance.playerVoiceMixers[player.playerClientId] == null) return;
        
        audioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[player.playerClientId];
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 40f;
        audioSource.dopplerLevel = 0.5f;
        audioSource.pitch = 1f;
        audioSource.spatialize = true;
        audioSource.spatialBlend = player.isPlayerDead ? 0f : 1f;
        bool playerHasDeathPermissions = !player.isPlayerDead || StartOfRound.Instance.localPlayerController.isPlayerDead;
        audioSource.volume = playerHasDeathPermissions ? BoomboxConfig.Volume.Value : 0;

        BoomboxPlugin.Log("Adjusted audio source");

        AudioHighPassFilter highPassFilter = speaker.GetComponent<AudioHighPassFilter>();
        if (highPassFilter != null)
        {
            highPassFilter.enabled = false;
        }

        AudioLowPassFilter lowPassFilter = speaker.GetComponent<AudioLowPassFilter>();
        if (lowPassFilter != null)
        {
            lowPassFilter.lowpassResonanceQ = 1;
            lowPassFilter.cutoffFrequency = 5000;
        }

        BoomboxPlugin.Log("Adjusted audio filters");

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        audioSource.PlayOneShot(audioSource.clip, 1f);
        RoundManager.Instance.PlayAudibleNoise(speaker.transform.position, 25f, 0.7f);
        BoomboxPlugin.Log("Playing audio!");
    }

    private static GameObject GetOrCreateSpeaker(PlayerControllerB player)
    {
        GameObject speaker = player.gameObject.transform.Find("Speaker")?.gameObject;
        if (speaker == null)
        {
            BoomboxPlugin.Log("No speaker found, creating a new one...");
            GameObject newSpeaker = new GameObject("Speaker");
            newSpeaker.transform.parent = player.transform;
            newSpeaker.transform.localPosition = Vector3.zero;
            newSpeaker.AddComponent<AudioSource>();
            newSpeaker.AddComponent<AudioHighPassFilter>();
            newSpeaker.AddComponent<AudioLowPassFilter>();
            return newSpeaker;
        }
        BoomboxPlugin.Log("Speaker found!");
        return speaker;
    }

    private static bool VoiceChanged(PlayerControllerB player, string chatMessage)
    {
        if (chatMessage.StartsWith("!voice "))
        {
            string[] args = chatMessage.Split(' ');

            if (args[1] == "david" || args[1] == "zira")
            {
                BoomboxPlugin.Log($"Switched voice to: {args[1]}");
                UserToVoice.Value[player.playerUsername] = args[1];
                return true;
            }
            BoomboxPlugin.Log($"Cleared voice");
            UserToVoice.Value[player.playerUsername] = "none";
            return true;
        }
        return false;
    }

    public static PlayerControllerB[] GetPlayers()
    {
        return HUDManager.Instance.playersManager.allPlayerScripts;
    }
}
