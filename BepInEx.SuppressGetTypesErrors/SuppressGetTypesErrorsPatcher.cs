using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil;

namespace BepInEx
{
    public static class SuppressGetTypesErrorsPatcher
    {
        public const string GUID = "BepInEx.SuppressGetTypesErrorsPatcher";
        public const string PluginName = "Suppress Type.GetTypes Errors";
        public const string Version = "1.0";

        // Needed to be a valid patcher
        public static IEnumerable<string> TargetDLLs { get; } = Enumerable.Empty<string>();

        // Needed to be a valid patcher
        public static void Patch(AssemblyDefinition assembly) { }

        public static void Finish()
        {
            // Need to run this in finalizer after all assemblies are patched or we might patch an assembly that gets replaced later
            Harmony.CreateAndPatchAll(typeof(SuppressGetTypesErrorsPatcher), GUID);
        }

        [HarmonyPatch(typeof(Assembly), nameof(Assembly.GetTypes), new Type[0])]
        [HarmonyFinalizer]
        public static void HandleReflectionTypeLoad(ref Exception __exception, ref Type[] __result)
        {
            if (__exception == null)
                return;
            if (__exception is ReflectionTypeLoadException re)
            {
                __exception = null;
                __result = re.Types.Where(t => t != null).ToArray();
                UnityEngine.Debug.Log($"Encountered ReflectionTypeLoadException which was suppressed. Full error: \n${TypeLoader.TypeLoadExceptionToString(re)}");
            }
        }
    }
}
