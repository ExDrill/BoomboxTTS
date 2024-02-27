using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BoomboxMod;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class BoomboxPlugin : BaseUnityPlugin
{
    public const string ModGUID = "ExDrill.BoomboxMod";
    public const string ModName = "BoomboxMod";
    public const string ModVersion = "1.0.0";

    public static BoomboxClient Client;
    public static bool IsNiceChatLoaded;
    public readonly Harmony harmony = new Harmony(ModGUID);
    
    protected static ManualLogSource logger;


    public void Awake()
    {
        logger = base.Logger;
        Client = new BoomboxClient();
        IsNiceChatLoaded = false;

        Client.Initialize();
        BoomboxConfig.Initialize(base.Config);
        harmony.PatchAll();
    }

    public void OnDestroy()
    {
        // NiceChat compatibility
        if (Harmony.HasAnyPatches("taffyko.NiceChat"))
        {
            Log("Nice Chat detected.");
            IsNiceChatLoaded = true;
        }
    }

    public static void Log(object data)
    {
        logger?.LogInfo(data);
    }

    public static void Warn(object data)
    {
        logger?.LogWarning(data);
    }

    public static void Error(object data)
    {
        logger?.LogError(data);
    }

    public static string GetBaseDirectory()
    {
        return new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath.Replace($"{BoomboxPlugin.ModName}.dll", "");
    }
}
