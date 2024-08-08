using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class IMGUITextCursorFix : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.IMGUITextCursorFix";
        public const string PluginName = "IMGUITextCursorFix";
        public const string Version = "1.0";

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static class Hooks
        {
            // Patch created by jshepler
            [HarmonyTranspiler, HarmonyPatch(typeof(TextEditor), "position", MethodType.Setter)]
            private static IEnumerable<CodeInstruction> TextEditor_set_position(IEnumerable<CodeInstruction> instructions)
            {
                var scrollOffset = typeof(TextEditor).GetField("scrollOffset");

                var cm = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Stfld, scrollOffset))
                    .Advance(-1)
                    .RemoveInstructions(3);

                return cm.InstructionEnumeration();
            }
        }
    }
}
