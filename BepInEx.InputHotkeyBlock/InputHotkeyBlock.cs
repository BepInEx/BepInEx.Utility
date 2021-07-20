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
        public const string Version = "1.4";

        private static Type TMPInputFieldType;

        internal void Main()
        {
            //Try to get the type of the TextMeshPro InputField, if present
            TMPInputFieldType = Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro");
            if (TMPInputFieldType == null)
                TMPInputFieldType = Type.GetType("TMPro.TMP_InputField, TextMeshPro-1.0.55.56.0b12");

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static GameObject previousSelectedGameObject;
        private static bool previousState;

        private static bool inhibit;

        /// <summary>
        /// Check if an input field is selected
        /// </summary>
        private static bool AllowInput()
        {
            if (inhibit)
                return true;

            //UI elements from some mods
            if (GUIUtility.keyboardControl > 0)
                return false;

            var currentSelectedGameObject = EventSystem.current?.currentSelectedGameObject; // Checking with ? is fine here
            if (currentSelectedGameObject != null)
            {
                // Buffer results to prevent unnecessary GetComponent calls
                if (currentSelectedGameObject == previousSelectedGameObject)
                {
                    if (IsWhitelistedKey()) return true;
                    return previousState;
                }

                previousSelectedGameObject = currentSelectedGameObject;

                if (currentSelectedGameObject.GetComponent<InputField>() != null ||
                    TMPInputFieldType != null && currentSelectedGameObject.GetComponent(TMPInputFieldType) != null)
                {
                    previousState = false;

                    if (IsWhitelistedKey()) return true;
                    return false;
                }
            }
            else
            {
                previousSelectedGameObject = null;
            }

            previousState = true;
            return true;
        }

        private static bool IsWhitelistedKey()
        {
            inhibit = true;
            var isWhitelistedKey = Input.GetKey(KeyCode.Tab);
            inhibit = false;
            return isWhitelistedKey;
        }

        /// <summary>
        /// GetKey hooks. When HotkeyBlock returns false the GetKey functions will be prevented from running.
        /// </summary>
        internal static partial class Hooks
        {
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(KeyCode))]
            internal static bool GetKeyCode() => AllowInput();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKey), typeof(string))]
            internal static bool GetKeyString() => AllowInput();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(KeyCode))]
            internal static bool GetKeyDownCode() => AllowInput();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyDown), typeof(string))]
            internal static bool GetKeyDownString() => AllowInput();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(KeyCode))]
            internal static bool GetKeyUpCode() => AllowInput();
            [HarmonyPrefix, HarmonyPatch(typeof(Input), nameof(Input.GetKeyUp), typeof(string))]
            internal static bool GetKeyUpString() => AllowInput();
        }
    }
}
