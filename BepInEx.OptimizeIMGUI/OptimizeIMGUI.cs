using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

            #region Cache

            private static readonly Dictionary<int, Dictionary<float, GUILayoutOption>> _layoutOptionCache =
                new Dictionary<int, Dictionary<float, GUILayoutOption>>();

            private static readonly ConstructorInfo _layoutOptionConstructor;
            private static readonly GUILayoutOption _widthTrue;
            private static readonly GUILayoutOption _widthFalse;
            private static readonly GUILayoutOption _heightTrue;
            private static readonly GUILayoutOption _heightFalse;

            private static GUILayoutOption CreateNewGuiLayoutOption(int type, object value)
            {
                return (GUILayoutOption)_layoutOptionConstructor.Invoke(new[] { type, value });
            }

            static Hooks()
            {
                _layoutOptionConstructor = AccessTools.FirstConstructor(typeof(GUILayoutOption), info => info.GetParameters().Length == 2);
                _widthTrue = CreateNewGuiLayoutOption(6, 1);
                _widthFalse = CreateNewGuiLayoutOption(6, 0);
                _heightTrue = CreateNewGuiLayoutOption(7, 1);
                _heightFalse = CreateNewGuiLayoutOption(7, 0);
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.Width))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.MinWidth))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.MaxWidth))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.Height))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.MinHeight))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.MaxHeight))]
            private static IEnumerable<CodeInstruction> CachedGetOptionFPatch(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Box),
                        new CodeMatch(OpCodes.Newobj))
                    .SetAndAdvance(OpCodes.Nop, null)
                    .Set(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(CachedGetOptionF)))
                    .Instructions();
            }

            private static GUILayoutOption CachedGetOptionF(int type, float value)
            {
                if (!_layoutOptionCache.TryGetValue(type, out var cache))
                {
                    cache = new Dictionary<float, GUILayoutOption>();
                    _layoutOptionCache[type] = cache;
                }

                if (!cache.TryGetValue(value, out var result))
                {
                    result = CreateNewGuiLayoutOption(type, value);
                    cache[value] = result;
                }

                return result;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.ExpandWidth))]
            [HarmonyPatch(typeof(GUILayout), nameof(GUILayout.ExpandHeight))]
            private static IEnumerable<CodeInstruction> CachedGetOptionBPatch(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Box),
                        new CodeMatch(OpCodes.Newobj))
                    .SetAndAdvance(OpCodes.Nop, null)
                    .Set(OpCodes.Call, AccessTools.Method(typeof(Hooks), nameof(CachedGetOptionB)))
                    .Instructions();
            }

            private static GUILayoutOption CachedGetOptionB(int type, int value)
            {
                if (type == 6)
                    return value == 0 ? _widthFalse : _widthTrue;
                if (type == 7)
                    return value == 0 ? _heightFalse : _heightTrue;

                throw new ArgumentException("Unknown type " + type, nameof(type));
            }

            #endregion
        }
    }
}