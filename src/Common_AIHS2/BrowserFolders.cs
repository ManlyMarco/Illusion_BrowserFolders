using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInProcess(KoikatuAPI.GameProcessName)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    public class BrowserFoldersPlugin : BaseUnityPlugin
    {
        public const string Guid = Constants.Guid;
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger { get; private set; }
        internal static string UserDataPath { get; } = Utils.NormalizePath(Path.Combine(Paths.GameRootPath, "UserData")); // UserData.Path

        private IFolderBrowser[] _instances;
        private int _lastScreenHeight;

        private void Awake()
        {
            Logger = base.Logger;

            var storeRects = Config.Bind("General", "Save window sizes and positions", true, "Store window sizes and positions to config so it persists across game restarts. If disabled, the values are reset to defaults on game start.");
            var storedRects = Config.Bind("General", "Window Rectangles", string.Empty, new ConfigDescription("Window positions and sizes.", null, "Advanced"));
            var storedRectsDic = DeserializeRects(storeRects.Value ? storedRects.Value : null);
            KoikatuAPI.Quitting += (o, e) => storedRects.Value = SerializeRects(_instances);

            var enableFilesystemWatchers = Config.Bind("General", "Automatically refresh when files change", true, "When files are added/deleted/updated the list will automatically update. If disabled you have to hit the refresh button manually when files are changed.");
            enableFilesystemWatchers.SettingChanged += (s, e) => FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;
            FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;

            var harmony = new Harmony(Guid);
            _instances = LoadBrowsers(harmony, Config, storedRectsDic);
        }

        private void Update()
        {
            CheckScreenSizeChange();

            for (var i = 0; i < _instances.Length; i++)
                _instances[i].Update();
        }

        private void OnGUI()
        {
            for (var i = 0; i < _instances.Length; i++)
                _instances[i].OnGui();
        }

        private static IFolderBrowser[] LoadBrowsers(Harmony harmony, ConfigFile config, Dictionary<string, Rect> windowRects)
        {
            // Collect all IFolderBrowser instances
            var allTypes = typeof(BrowserFoldersPlugin).Assembly.GetTypesSafe().Where(x => typeof(IFolderBrowser).IsAssignableFrom(x) && !x.IsAbstract);
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

            // Initialize IFolderBrowser instances
            allInstances.RemoveAll(instance =>
            {
                var fullName = instance?.GetType().FullName ?? throw new Exception("huh " + instance);
                try
                {
                    var success = instance.Initialize(StudioAPI.InsideStudio, config, harmony);
                    if (success && windowRects.TryGetValue(fullName, out var storedRect))
                        instance.WindowRect = storedRect;
                    return !success;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to initialize instance of {fullName} - {e}");
                    return true;
                }
            });

            Logger.LogInfo($"Loaded {allInstances.Count}/{allTypesCount} folder browsers: {string.Join(", ", allInstances.Select(x => x.GetType().Name))}");
            return allInstances.ToArray();
        }

        private static Dictionary<string, Rect> DeserializeRects(string storedRectsValue)
        {
            if (string.IsNullOrEmpty(storedRectsValue)) return new Dictionary<string, Rect>();

            return storedRectsValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Attempt(x =>
            {
                var parts = x.Split(',');
                var fullname = parts[0];
                var rect = new Rect(int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
                return new KeyValuePair<string, Rect>(fullname, rect);
            }).ToDictionary(x => x.Key, x => x.Value);
        }

        private static string SerializeRects(IFolderBrowser[] instances)
        {
            return string.Join("|", instances.Select(x =>
            {
                var rect = x.WindowRect;
                return $"{x.GetType().FullName},{rect.x},{rect.y},{rect.width},{rect.height}";
            }));
        }

        private void CheckScreenSizeChange()
        {
            var newScreenHeight = Screen.height;
            if (newScreenHeight == _lastScreenHeight) return;

            var newScreenWidth = Screen.width;
            var screenRect = new Rect(0, 0, newScreenWidth, newScreenHeight);
            
            // Game UI mostly scales based on height
            var scaleChange = _lastScreenHeight > 0 ? newScreenHeight / (float)_lastScreenHeight : 0;

            // Calculate extra offset from left edge when screen aspect ratio changes because UI stays at 16:9
            const float targetAspect = 16f / 9f;
            var desiredWidth = Mathf.RoundToInt(targetAspect * newScreenHeight);
            var xOffset = (newScreenWidth - desiredWidth) / 2;

            foreach (var instance in _instances)
            {
                var rect = instance.WindowRect;
                if (rect.width == 0 || rect.height == 0)
                {
                    rect = instance.GetDefaultRect();
                }
                else if (scaleChange > 0)
                {
                    rect = new Rect(Mathf.RoundToInt(xOffset + rect.x * scaleChange), Mathf.RoundToInt(rect.y * scaleChange),
                                    Mathf.RoundToInt(rect.width * scaleChange), Mathf.RoundToInt(rect.height * scaleChange));
                }

                // Ensure the window is not completely off-screen
                if (!screenRect.Overlaps(rect))
                    rect = instance.GetDefaultRect();

                instance.WindowRect = rect;
            }

            _lastScreenHeight = newScreenHeight;
        }
    }
}
