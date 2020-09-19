using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class OptimizeIMGUI : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.OptimizeIMGUI";
        public const string PluginName = "Optimize IMGUI GC allocations";
        public const string Version = "1.0";
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static class Hooks
        {
            /// <summary>
            /// This hook fixes the mere existence of OnGUI code generating a ton of unnecessary garbage
            /// </summary>
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GUILayoutUtility), "Begin")]
            private static IEnumerable<CodeInstruction> FixOnguiGarbageDump(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var luT = AccessTools.TypeByName("UnityEngine.GUILayoutUtility") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutUtility\")");
                var lcT = luT.GetNestedType("LayoutCache", AccessTools.all) ?? throw new MissingMemberException("luT.GetNestedType(\"LayoutCache\", AccessTools.all)");
                var topF = AccessTools.Field(lcT, "topLevel") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"topLevel\")");
                var winF = AccessTools.Field(lcT, "windows") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"windows\")");
                var curF = AccessTools.Field(luT, "current") ?? throw new MissingMemberException("AccessTools.Field(luT, \"current\")");
                var lgF = AccessTools.Field(lcT, "layoutGroups") ?? throw new MissingMemberException("AccessTools.Field(lcT, \"layoutGroups\")");
                var lgT = AccessTools.TypeByName("UnityEngine.GUILayoutGroup") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutGroup\")");
                var entrF = AccessTools.Field(lgT, "entries") ?? throw new MissingMemberException("AccessTools.Field(lgT, \"entries\")");
                var entrT = AccessTools.TypeByName("UnityEngine.GUILayoutEntry") ?? throw new MissingMemberException("AccessTools.TypeByName(\"UnityEngine.GUILayoutEntry\")");
                var entrClearM = AccessTools.Method(typeof(List<>).MakeGenericType(entrT), "Clear");

                var sltIdM = AccessTools.Method(luT, "SelectIDList");
                var curP = AccessTools.PropertyGetter(typeof(Event), "current");
                var typeP = AccessTools.PropertyGetter(typeof(Event), "type");

                var l0 = generator.DefineLabel();
                var l1 = generator.DefineLabel();
                var l2 = generator.DefineLabel();

                var replacementInstr = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldc_I4_0),
                    new CodeInstruction(OpCodes.Call, sltIdM),
                    new CodeInstruction(OpCodes.Stloc_0),
                    new CodeInstruction(OpCodes.Call, curP),
                    new CodeInstruction(OpCodes.Callvirt, typeP),
                    new CodeInstruction(OpCodes.Ldc_I4_8),
                    new CodeInstruction(OpCodes.Bne_Un_S, l0),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Brtrue_S, l1),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(lgT, new Type[0])),
                    new CodeInstruction(OpCodes.Stfld, topF),
                    new CodeInstruction(OpCodes.Ldloc_0) {labels = new List<Label> {l1}},
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Ldfld, entrF),
                    new CodeInstruction(OpCodes.Callvirt, entrClearM),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, winF),
                    new CodeInstruction(OpCodes.Brtrue_S, l2),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(lgT, new Type[0])),
                    new CodeInstruction(OpCodes.Stfld, winF),
                    new CodeInstruction(OpCodes.Ldloc_0) {labels = new List<Label> {l2}},
                    new CodeInstruction(OpCodes.Ldfld, winF),
                    new CodeInstruction(OpCodes.Ldfld, entrF),
                    new CodeInstruction(OpCodes.Callvirt, entrClearM),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Stfld, topF),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, lgF),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Stack), nameof(Stack.Clear))),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, lgF),
                    new CodeInstruction(OpCodes.Ldsfld, curF),
                    new CodeInstruction(OpCodes.Ldfld, topF),
                    new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Stack), nameof(Stack.Push))),
                    new CodeInstruction(OpCodes.Ret),
                    new CodeInstruction(OpCodes.Ldsfld, curF) {labels = new List<Label> {l0}}
                };

                var instr = instructions.ToList();
                var c = 0;
                for (var i = instr.Count - 1; i >= 0; i--)
                {
                    c = c + Convert.ToInt32(instr[i].opcode == OpCodes.Ldsfld);
                    if (c == 3)
                    {
                        for (var j = i + 1; j < instr.Count; j++)
                            replacementInstr.Add(instr[j]);
                        break;
                    }
                }

                if (c != 3) throw new InvalidOperationException("IL footprint does not match what was expected");
                return replacementInstr;
            }
        }
    }
}
