using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using Welcome.Introductions;
using Welcome.Managers;

namespace Welcome
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class WelcomePlugin : BaseUnityPlugin
    {
        internal const string ModName = "Welcome";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "RustyMods";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource WelcomeLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        public static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        public enum Toggle { On = 1, Off = 0 }
        public void Awake()
        {
            InitConfigs();
            Intro.InitCustomIntro();
            TextureManager.InitCustomBackground();
            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy() => Config.Save();

        #region Utils
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
                WelcomeLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                WelcomeLogger.LogError($"There was an issue loading your {ConfigFileName}");
                WelcomeLogger.LogError("Please check your config entries for spelling and format!");
            }
        }
        #endregion


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<Toggle> _AlwaysShowIntro = null!;
        public static ConfigEntry<Toggle> _PluginEnabled = null!;
        public static ConfigEntry<Toggle> _UseCustomBackground = null!;
        public static ConfigEntry<Toggle> _UseBackgroundOverlay = null!;
        public static ConfigEntry<string> _CustomBackgroundName = null!;

        private void InitConfigs()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            _PluginEnabled = config("2 - Settings", "0 - Enabled", Toggle.On, "If on, plugin overrides introductions");
            _AlwaysShowIntro = config("2 - Settings", "Always Show Intro", Toggle.Off,
                "If on, plugin will always show introduction upon log in");

            _UseCustomBackground = config("2 - Settings", "Use Custom Background", Toggle.Off,
                "If on, plugin will load and use custom image");
            _UseBackgroundOverlay = config("2 - Settings", "Use Background Overlay", Toggle.On,
                "If on, background darken overlay will be applied");
            _CustomBackgroundName = config("2 - Settings", "Custom Background File name", "",
                "Set the file name plugin should look for to use as custom background. Needs to be inside [BepinEx/config/Welcome] folder");

        }

        #region ConfigMethods
        private ConfigEntry<T> config<T>(string group, string title, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, title, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string title, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, title, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }
        #endregion

        #endregion
    }
}