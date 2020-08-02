using UnityEngine;

namespace BepInEx
{
    /// <summary>
    /// Allow toggling full screen with alt+enter in games where that has been disabled
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class EnableFullScreenToggle : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.EnableFullScreenToggle";
        public const string PluginName = "Enable Full Screen Toggle";
        public const string Version = "1.0";

        internal void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                //This section of code is never reached on Unity builds where full screen can be toggled, it seems
                //We can safely toggle full screen without risk of it being toggled twice
                Screen.fullScreen = !Screen.fullScreen;
        }
    }
}
