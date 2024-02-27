using BepInEx.Configuration;

namespace BoomboxMod;

public class BoomboxConfig
{
    public static ConfigEntry<float> Volume;
    public static ConfigEntry<string> SilencePrefix;

    private static bool isLoaded = false;

    public static void Initialize(ConfigFile config)
    {
        if (!isLoaded)
        {
            Volume = config.Bind("Tweaks", "TTS Volume", 0.5f);
            SilencePrefix = config.Bind("Tweaks", "Silence Prefix", "/", new ConfigDescription("TTS by default will be silenced if a message was sent with the / prefix, this can be changed."));
            isLoaded = true;
        }
    }
}
