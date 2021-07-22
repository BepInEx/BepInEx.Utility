using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BepInEx
{
    [BepInPlugin(GUID, PluginName, Version)]
    public class CatchUnityEventExceptionsPlugin : BaseUnityPlugin
    {
        public const string GUID = "CatchUnityEventExceptions";
        public const string PluginName = "Catch Unity Event Exceptions";
        public const string Version = "1.0";

        private static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;

            try
            {
                Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            }
            catch (Exception e)
            {
                Logger.LogMessage(GUID + " is not compatible with version of Unity that this game is using!");
                Debug.LogException(e);
                enabled = false;
            }
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(SceneManager), "Internal_ActiveSceneChanged")]
            private static bool Internal_ActiveSceneChangedHook(Scene previousActiveScene, Scene newActiveScene)
            {
                return !SafeInvokeEvent<SceneManager, UnityAction<Scene, Scene>>("activeSceneChanged", action => action.Invoke(previousActiveScene, newActiveScene));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SceneManager), "Internal_SceneLoaded")]
            private static bool Internal_SceneLoadedHook(Scene scene, LoadSceneMode mode)
            {
                return !SafeInvokeEvent<SceneManager, UnityAction<Scene, LoadSceneMode>>("sceneLoaded", action => action.Invoke(scene, mode));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SceneManager), "Internal_SceneUnloaded")]
            private static bool Internal_SceneUnloadedHook(Scene scene)
            {
                return !SafeInvokeEvent<SceneManager, UnityAction<Scene>>("sceneUnloaded", action => action.Invoke(scene));
            }

            private static bool SafeInvokeEvent<T, T2>(string eventFieldName, Action<T2> callHandler) where T2 : Delegate
            {
                try
                {
                    var action = (T2)typeof(T).GetField(eventFieldName, AccessTools.all).GetValue(null);
                    if (action != null)
                    {
                        foreach (T2 handler in action.GetInvocationList())
                        {
                            try
                            {
                                callHandler(handler);
                            }
                            catch (Exception e)
                            {
                                var eventName = typeof(T).Name + "." + eventFieldName;
                                Logger.LogWarning(
                                    "Caught an exception when invoking the " + eventName + " event. PLEASE FIX THE CODE THAT CAUSED THE EXCEPTION BELOW! Without this plugin, this exception WILL cause event handlers of random plugins to randomly not be run, and it WILL create a ton of unnecessary headache for plugin authors.");
                                Debug.LogException(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Failed to safely run events, falling back to original game code. Reason: " + e.Message);
                    return false;
                }

                return true;
            }
        }
    }
}