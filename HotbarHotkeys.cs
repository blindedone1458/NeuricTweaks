using BepInEx.Configuration;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeuricTweaks
{
    internal class HotbarHotkeys
    {
        private class Hotkey
        {
            public ConfigEntry<KeyboardShortcut> hotkey {  get; set; }
            public ConfigEntry<string> label { get; set; }
            public ConfigEntry<NeuricTweaks.Toggle> replaceDef {  get; set; }
        }

        private const int defaultNumKeys = 8;
        private static Hotkey[] hotkeys = new Hotkey[defaultNumKeys];
        private static KeyCode[] defaultKeyCodes = {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8
        };
         
       

        #region Configuration
        private static BaseUnityPlugin? _plugin;
        private static BaseUnityPlugin plugin => _plugin ??= (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(Assembly.GetExecutingAssembly().DefinedTypes.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

        private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);

            return configEntry;
        }

        private static ConfigEntry<T> config<T>(string group, string name, T value, string description) => config(group, name, value, new ConfigDescription(description));
        #endregion Configuration

        
        static HotbarHotkeys()
        {
            NeuricTweaks.Log("Patching Hotbar Hotkeys");

            // hard coding the vanilla defaults, should be safe for other mods or features that expand this
            

            for (var i = 0; i < defaultNumKeys; i++)
            {
                hotkeys[i] = new Hotkey
                {
                    hotkey = config("4 - Hotbar Hotkeys", $"Hotbar Slot {i+1} - Key", new KeyboardShortcut(KeyCode.None), $"Hotkey for Hotbar Slot {i+1}"),
                    label = config("4 - Hotbar Hotkeys", $"Hotbar Slot {i+1} - Label", (i+1).ToString(), $"Label for Hotbar Slot {i+1}. Empty will use the assigned Key. Unicode does work if you do \\u####"),
                    replaceDef = config("4 - Hotbar Hotkeys", $"Hotbar Slot {i+1} - Replace", NeuricTweaks.Toggle.On, "If enabled, Hotkey will replace default hotkey. If disabled, default hotkey and one set here will both work.")
                };

                if (hotkeys[i].hotkey.Value.MainKey == KeyCode.None)
                    hotkeys[i].hotkey.Value = new KeyboardShortcut(defaultKeyCodes[i]);
            }

            Harmony _harmony = new(NeuricTweaks.PLUGIN_NAME + ".HotbarHotkeys");
            //_harmony.Patch(AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Update)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HotbarHotkeys), nameof(Patch_Hotkeys))));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(Player), nameof(Player.UseHotbarItem)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HotbarHotkeys), nameof(Patch_HotbarSelection))));
            _harmony.Patch(AccessTools.DeclaredMethod(typeof(HotkeyBar), nameof(HotkeyBar.Update)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(HotbarHotkeys), nameof(Patch_HotkeyLabels))));

        }

        internal void Update()
        {
            Check_Hotkeys();
        }

        private static void Check_Hotkeys()
        {
            Player player = Player.m_localPlayer;
            if (player != null && player.TakeInput())
            {
                for (var i = 0; i < defaultNumKeys; i++)
                {
                    if (ZInput.GetKeyDown(hotkeys[i].hotkey.Value.MainKey))
                    {
                        player.UseHotbarItem((i + 1) * 10);
                    }
                }
            }
        }

        // prefix patch Player.UseHotbarItem
        private static bool Patch_HotbarSelection(ref int index)
        {
            // vanilla hotkeys come in as 1-8
            // modded hotkeys are 10-80
            bool modded = false;
            if (index / 10 >= 1)
            {
                modded = true;
                index /= 10;
            }

            Hotkey checkHotkey = hotkeys[index - 1];

            if (modded)
            {
                return true;
            } 
            else
            {
                if ((checkHotkey.replaceDef.Value == NeuricTweaks.Toggle.Off && checkHotkey.hotkey.Value.MainKey != defaultKeyCodes[index - 1]) || checkHotkey.hotkey.Value.MainKey == KeyCode.None)
                {
                    return true;
                }
            }

            return false;
        }

        // postfix patch HotkeyBar.Update
        private static void Patch_HotkeyLabels(HotkeyBar __instance)
        {
            // Make sure we're not stomping on EquipmentAndQuickSlots
            if (__instance.isActiveAndEnabled && __instance.name != "QuickSlotsHotkeyBar")
            {
                
                for (var i = 0; i < __instance.m_elements.Count; i++)
                {
                    
                    if (String.IsNullOrEmpty(hotkeys[i].label?.Value))
                    {
                        __instance.m_elements[i].m_go.transform.Find("binding").GetComponent<TMP_Text>().text = hotkeys[i].hotkey.Value.MainKey.ToString();
                    }
                    else
                    {
                        __instance.m_elements[i].m_go.transform.Find("binding").GetComponent<TMP_Text>().text = hotkeys[i].label.Value;
                    }
                    
                }

            }
        }
    }
}
