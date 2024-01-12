using BepInEx.Configuration;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;

namespace NeuricTweaks
{
    internal class SailingTweaks
    {
        private static ConfigEntry<float> sailForceMultHalf = config("2 - Sailing", "Half Sail Force Mult", 1.0f, "Sail Force Multiplier applied when sail is at half.");
        private static ConfigEntry<float> sailForceMultFull = config("2 - Sailing", "Full Sail Force Mult", 1.0f, "Sail Force Multiplier applied when sail is fully open.");

        private static ConfigEntry<float> sailSpeedMultHalf = config("2 - Sailing", "Half Sail Speed Mult", 1.0f, "Sail Speed Multiplier applied when sail is at half.");
        private static ConfigEntry<float> sailSpeedMultFull = config("2 - Sailing", "Full Sail Speed Mult", 1.0f, "Sail Speed Multiplier applied when sail is fully open.");

        private static ConfigEntry<NeuricTweaks.Toggle> rowSpeedPerPlayer = config("3 - Rowing", "Rowing Mult Per Player", NeuricTweaks.Toggle.On, "When Enabled, 'Rowing_Speed_Mult' will be also multiplied by number of players in the Ship.");
        private static ConfigEntry<float> rowSpeedMult = config("3 - Rowing", "Rowing Speed Mult", 1.0f, "Multiplier applied to rudder speed.");

        private static float defSailForceFactor = 0.1f; 

        #region Configuration
        private static BaseUnityPlugin? _plugin;
        private static BaseUnityPlugin plugin => _plugin ??= (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(Assembly.GetExecutingAssembly().DefinedTypes.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

        private static bool hasConfigSync = true;
        private static object? _configSync;

        private static object? configSync
        {
            get
            {
                if (_configSync == null && hasConfigSync)
                {
                    if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
                    {
                        _configSync = Activator.CreateInstance(configSyncType, NeuricTweaks.PLUGIN_GUID + " SailingTweaks");
                        configSyncType.GetField("CurrentVersion").SetValue(_configSync, NeuricTweaks.PLUGIN_VERSION.ToString());
                        configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
                    }
                    else
                    {
                        hasConfigSync = false;
                    }
                }

                return _configSync;
            }
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);

            configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T)).Invoke(configSync, new object[] { configEntry });

            return configEntry;
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, string description) => config(group, name, value, new ConfigDescription(description));
        #endregion Configuration

        static SailingTweaks()
        {
            NeuricTweaks.Log("Patching Sailing Speeds");
            Harmony _harmony = new(NeuricTweaks.PLUGIN_NAME + ".SailingTweaks");
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.GetSailForce)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SailingTweaks), nameof(Patch_SailForce))));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.GetSailForce)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SailingTweaks), nameof(Patch_SailSpeed))));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.Start)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(SailingTweaks), nameof(Patch_RudderSpeed))));
            
        }

        private static void Patch_SailForce(Ship __instance)
        {
            if (__instance.m_speed == Ship.Speed.Half)
            {
                __instance.m_sailForceFactor = defSailForceFactor * sailForceMultHalf.Value;
            }
            else if (__instance.m_speed == Ship.Speed.Full)
            {
                __instance.m_sailForceFactor = defSailForceFactor * sailForceMultFull.Value;
            }
        }

        private static void Patch_SailSpeed(Ship __instance, ref Vector3 __result)
        {
            if (__instance.m_speed == Ship.Speed.Half)
            {
                __result.x *= sailSpeedMultHalf.Value;
                __result.z *= sailSpeedMultHalf.Value;
            }
            else if (__instance.m_speed == Ship.Speed.Full)
            {
                __result.x *= sailSpeedMultFull.Value;
                __result.z *= sailSpeedMultFull.Value;
            }
        }

        private static void Patch_RudderSpeed(Ship __instance)
        {
            if (rowSpeedPerPlayer.Value == NeuricTweaks.Toggle.On)
            {
                __instance.m_backwardForce *= Math.Max(__instance.m_players.Count, 1) * rowSpeedMult.Value;
            } 
            else
            {
                __instance.m_backwardForce *= rowSpeedMult.Value;
            }
        }
    }
}
