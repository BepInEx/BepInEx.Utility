using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using BepInEx.Logging;
using UnityEngine;

namespace BepInEx
{
    /// <summary>
    /// Improves loading times and reduces or eliminates stutter in games that abuse Resources.UnloadUnusedAssets and/or GC.Collect.
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    public class ResourceUnloadOptimizations : BaseUnityPlugin
    {
        public const string GUID = "BepInEx.ResourceUnloadOptimizations";
        public const string PluginName = "Resource Unload Optimizations";
        public const string Version = "1.0";

        private static new ManualLogSource Logger;

        private static AsyncOperation _currentOperation;
        private static Func<AsyncOperation> _originalUnload;

        private static int _garbageCollect;
        private float _waitTime;

        public static ConfigEntry<bool> DisableUnload { get; private set; }
        public static ConfigEntry<bool> OptimizeMemoryUsage { get; private set; }
        public static ConfigEntry<int> PercentMemoryThreshold { get; private set; }

        internal void Awake()
        {
            Logger = base.Logger;

            DisableUnload = Config.Bind("Unload Throttling", "Disable Resource Unload", false, "Disables all resource unloading. Requires large amounts of RAM or will likely crash your game. NOT RECOMMENDED FOR NORMAL USE");
            OptimizeMemoryUsage = Config.Bind("Unload Throttling", "Optimize Memory Usage", true, "Use more memory (if available) in order to load the game faster and reduce random stutter.");
            PercentMemoryThreshold = Config.Bind("Unload Throttling", "Optimize Memory Threshold", 75, "Minimum amount of memory to be used before resource unloading will run.");

            InstallHooks();
            StartCoroutine(CleanupCo());
        }

        private static void InstallHooks()
        {
            var target = AccessTools.Method(typeof(Resources), nameof(Resources.UnloadUnusedAssets));
            var replacement = AccessTools.Method(typeof(Hooks), nameof(Hooks.UnloadUnusedAssetsHook));

            var detour = new NativeDetour(target, replacement);
            detour.Apply();

            _originalUnload = detour.GenerateTrampoline<Func<AsyncOperation>>();

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private IEnumerator CleanupCo()
        {
            while (true)
            {
                while (Time.realtimeSinceStartup < _waitTime)
                    yield return null;

                _waitTime = Time.realtimeSinceStartup + 1;

                if (_garbageCollect > 0)
                {
                    if (--_garbageCollect == 0)
                        RunGarbageCollect();
                }
            }
        }

        private static AsyncOperation RunUnloadAssets()
        {
            // Only allow a single unload operation to run at one time
            if (_currentOperation == null || _currentOperation.isDone && !PlentyOfMemory())
            {
                Logger.LogDebug("Starting unused asset cleanup");
                _currentOperation = _originalUnload();
            }
            return _currentOperation;
        }

        private static void RunGarbageCollect()
        {
            if (PlentyOfMemory()) return;

            Logger.LogDebug("Starting full garbage collection");
            // Use different overload since we disable the parameterless one
            GC.Collect(GC.MaxGeneration);
        }

        private static bool PlentyOfMemory()
        {
            if (!OptimizeMemoryUsage.Value) return false;

            var mem = MemoryInfo.GetCurrentStatus();
            if (mem == null) return false;

            // Clean up more aggresively during loading, less aggresively during gameplay
            var pageFileFree = mem.ullAvailPageFile / (float)mem.ullTotalPageFile;
            var plentyOfMemory = mem.dwMemoryLoad < PercentMemoryThreshold.Value // physical memory free %
                                 && pageFileFree > 0.3f // page file free %
                                 && mem.ullAvailPageFile > 2ul * 1024ul * 1024ul * 1024ul; // at least 2GB of page file free
            if (!plentyOfMemory)
                return false;

            Logger.LogDebug($"Skipping cleanup because of low memory load ({mem.dwMemoryLoad}% RAM, {100 - (int)(pageFileFree * 100)}% Page file, {mem.ullAvailPageFile / 1024 / 1024}MB available in PF)");
            return true;
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GC), nameof(GC.Collect), new Type[0])]
            public static bool GCCollectHook()
            {
                // Throttle down the calls. Keep resetting the timer until things calm down since it's usually fairly low memory usage
                _garbageCollect = 3;
                // Disable the original method, Invoke will call it later
                return false;
            }

            // Replacement method needs to be inside a static class to be used in NativeDetour
            public static AsyncOperation UnloadUnusedAssetsHook()
            {
                if (DisableUnload.Value)
                    return null;
                else
                    return RunUnloadAssets();
            }
        }
    }
}
