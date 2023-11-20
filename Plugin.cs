using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using System.IO;
using System.Reflection;
//using ServerSync;
using UnityEngine;

namespace NeuricTweaks
{

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class NeuricTweaks : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "neuric.valheim.tweaks";
        internal const string PLUGIN_NAME = "NeuricTweaks";
        internal const string PLUGIN_VERSION = "1.0.0";
        internal const string PLUGIN_AUTHOR = "Neuric";

        private ConfigEntry<Toggle> _serverConfigLocked = null!;
        private static ConfigEntry<Toggle> loggingEnabled;

        public readonly ManualLogSource tweakLogger = BepInEx.Logging.Logger.CreateLogSource(PLUGIN_NAME);

        private static string ConfigFileName = PLUGIN_NAME + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static NeuricTweaks _instance;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        private readonly Harmony _harmony = new Harmony(PLUGIN_NAME + ".Plugin");
        public void Awake()
        {
            _instance = this;
            // Create our global config entries
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            loggingEnabled = config("1 - General", "Logging", Toggle.Off,
                "If on, additional logging will be available.");
            //_ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            // Plugin startup logic
            _ = new SailingTweaks();
            _ = new HotbarHotkeys();

            tweakLogger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            BepInEx.Logging.Logger.Sources.Remove(tweakLogger);

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                tweakLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                tweakLogger.LogError($"There was an issue loading your {ConfigFileName}");
                tweakLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        //private static readonly ConfigSync ConfigSync = new(PLUGIN_GUID)
        //{ DisplayName = PLUGIN_NAME, CurrentVersion = PLUGIN_VERSION, MinimumRequiredVersion = PLUGIN_VERSION };

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);

            // SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            // syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }



        public static void Log(string message)
        {
            _instance.tweakLogger.LogMessage(message);
        }

        public static void LogWarning(string message)
        {
            if (loggingEnabled.Value == Toggle.On)
            {
                _instance.tweakLogger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (loggingEnabled.Value == Toggle.On)
            {
                _instance.tweakLogger.LogError(message);
            }
        }

        public static void LogInfo(string message)
        {
            if (loggingEnabled.Value == Toggle.On)
            {
                _instance.tweakLogger.LogInfo(message);
            }
        }

    }

}