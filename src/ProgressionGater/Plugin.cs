using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ProgressionGater
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.catosaur.progressiongater";
        public const string Name = "Progression Gater";
        public const string Version = "0.1.2";

        private Harmony _harmony;
        private FileSystemWatcher _configWatcher;
        private DateTime _lastWatcherEventUtc = DateTime.MinValue;
        private volatile bool _reloadRequested;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Bind(Config, RequestConfigReload);
            ProgressionService.Initialize();
            SetupConfigWatcher();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(ProgressionPatches));
            _harmony.PatchAll(typeof(AdminCommands));

            Log.LogInfo($"{Name} {Version} loaded with {ProgressionService.Rules.Bosses.Count} progression bosses.");
        }

        private void Update()
        {
            if (!_reloadRequested) return;
            _reloadRequested = false;

            try
            {
                Config.Reload();
                ProgressionService.ReloadRules();
                ProgressionService.Broadcast();
                Log.LogInfo("Configuration reloaded and synchronized to connected players.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Configuration reload failed: {ex}");
            }
        }

        private void RequestConfigReload()
        {
            _reloadRequested = true;
        }

        private void SetupConfigWatcher()
        {
            try
            {
                string path = Config.ConfigFilePath;
                _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                _configWatcher.Changed += (_, __) =>
                {
                    DateTime now = DateTime.UtcNow;
                    if ((now - _lastWatcherEventUtc).TotalMilliseconds < 500) return;
                    _lastWatcherEventUtc = now;
                    RequestConfigReload();
                };
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Config live-reload watcher could not be started: {ex.Message}");
            }
        }

        internal static void SaveConfig()
        {
            Instance?.Config.Save();
        }

        private void OnDestroy()
        {
            _configWatcher?.Dispose();
            _harmony?.UnpatchAll(Guid);
        }
    }
}
