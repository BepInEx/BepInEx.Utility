using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace BepInEx
{
    /// <summary>
    /// Mute the game in background
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class MuteInBackground : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.MuteInBackground";
        public const string PluginName = "Mute In Background";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        internal static float? OriginalVolume = null;

        public static ConfigEntry<bool> ConfigMuteInBackground { get; private set; }

        internal void Awake()
        {
            Logger = base.Logger;
            ConfigMuteInBackground = Config.Bind("Config", "Mute In Background", false, "Whether to mute the game when in the background, i.e. alt-tabbed.");
        }

        internal void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                //Restore the original volume if one was previously set
                if (OriginalVolume != null)
                    AudioListener.volume = (float)OriginalVolume;
                OriginalVolume = null;
            }
            else if (ConfigMuteInBackground.Value)
            {
                //Store the original volume and set the volume to zero
                OriginalVolume = AudioListener.volume;
                AudioListener.volume = 0;
            }
        }
    }
}
