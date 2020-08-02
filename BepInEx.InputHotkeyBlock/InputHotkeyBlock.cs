using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BepInEx
{
    /// <summary>
    /// Intercepts GetKey to prevent plugin hotkeys from firing while typing in an input field
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class InputHotkeyBlock : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.InputHotkeyBlock";
        public const string PluginName = "Input Hotkey Block";
        public const string Version = "1.3";

        private static Type TMPInputFieldType;

        internal void Main()
        {
            //Try to get the type of the TextMeshPro InputField, if present
            TMPInputFieldType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (TMPInputFieldType == null)
                TMPInputFieldType = Type.GetType("TMPro.TMP_InputField, TextMeshPro-1.0.55.56.0b12");

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool HotkeyBlock()
        {
            //UI elements from some mods
            if (GUIUtility.keyboardControl > 0)
                return false;

            if (EventSystem.current?.currentSelectedGameObject != null)
            {
                //TextMeshPro InputField
                if (TMPInputFieldType != null)
                    if (EventSystem.current.currentSelectedGameObject.GetComponent(TMPInputFieldType) != null)
                        return false;

                //Other InputFields
                if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// GetKey hooks. When HotkeyBlock returns false the GetKey functions will be prevented from running.
        /// </summary>
        internal static partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
            internal static bool GetKeyCode() => HotkeyBlock();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(string))]
            internal static bool GetKeyString() => HotkeyBlock();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
            internal static bool GetKeyDownCode() => HotkeyBlock();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(string))]
            internal static bool GetKeyDownString() => HotkeyBlock();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
            internal static bool GetKeyUpCode() => HotkeyBlock();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(string))]
            internal static bool GetKeyUpString() => HotkeyBlock();
        }
    }
}
