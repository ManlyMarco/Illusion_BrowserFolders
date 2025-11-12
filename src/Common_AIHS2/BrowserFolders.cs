using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    public class AI_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = Constants.Guid;
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger { get; private set; }

        private static IFolderBrowser[] _instances;

        private void Awake()
        {
            Logger = base.Logger;

            var harmony = new Harmony(Guid);

            var allTypes = typeof(AI_BrowserFolders).Assembly.GetTypesSafe().Where(x => typeof(IFolderBrowser).IsAssignableFrom(x) && !x.IsAbstract);
            var allTypesCount = 0;
            var allInstances = new List<IFolderBrowser>();
            foreach (var browserType in allTypes)
            {
                allTypesCount++;
                try
                {
                    allInstances.Add((IFolderBrowser)Activator.CreateInstance(browserType));
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to create instance of {browserType.FullName} - {e}");
                }
            }

            allInstances.RemoveAll(instance =>
            {
                try
                {
                    return !instance.Initialize(StudioAPI.InsideStudio, Config, harmony);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to initialize instance of {instance.GetType().FullName} - {e}");
                    return true;
                }
            });

            _instances = allInstances.ToArray();
            Logger.LogInfo($"Loaded {_instances.Length}/{allTypesCount} folder browsers: {string.Join(", ", allInstances.Select(x => x.GetType().Name))}");

            var enableFilesystemWatchers = Config.Bind("General", "Automatically refresh when files change", true, "When files are added/deleted/updated the list will automatically update. If disabled you have to hit the refresh button manually when files are changed.");
            enableFilesystemWatchers.SettingChanged += (s, e) => FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;
            FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;
        }

        private void Update()
        {
            for (var i = 0; i < _instances.Length; i++)
                _instances[i].Update();
        }

        private void OnGUI()
        {
            for (var i = 0; i < _instances.Length; i++)
                _instances[i].OnGui();
        }

        internal static string UserDataPath { get; } = Utils.NormalizePath(Path.Combine(Paths.GameRootPath, "UserData")); // UserData.Path
    }
}
