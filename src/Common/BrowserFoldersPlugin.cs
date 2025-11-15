using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, Constants.Name, Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
#if KKP
    // KKP needs a completely different set of hooks !!only for its main exe!!
    // The studio exe needs hooks from base KK instead.
    // Don't need to care about AIS steam since it is handled by a compatibility layer.
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#else
    [BepInProcess(KoikatuAPI.GameProcessName)]
#if !EC
    [BepInProcess(KoikatuAPI.StudioProcessName)]
#endif
#if KKS
    [BepInIncompatibility("KKS_StudioDefaultData")]
#endif
#endif
    public class BrowserFoldersPlugin : BaseUnityPlugin
    {
#if KKP
        public const string Guid = "marco.FolderBrowser.kkp"; // Must be different to avoid conflict with KK assembly
#else
        public const string Guid = "marco.FolderBrowser";
#endif
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger { get; private set; }
        internal static string UserDataPath { get; } = Utils.NormalizePath(Path.Combine(Paths.GameRootPath, "UserData")); // UserData.Path

#if KKP || KKS
        internal static ConfigEntry<bool> ShowDefaultCharas { get; private set; }
#endif

        private IFolderBrowser[] _instances;
        private int _lastScreenHeight;
        private ConfigEntry<bool> _adaptToResolution;

        private void Awake()
        {
            Logger = base.Logger;
#if KKP
            // Need to do this to use the same config file as KK assembly
            var configFile = new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "marco.FolderBrowser.cfg"), false, Info.Metadata);
#else
            var configFile = Config;
#endif

            _adaptToResolution = configFile.Bind("General", "Scale size and position on resolution change", true, "Attempt to automatically adjust sizes and positions of all windows whenever game resolution is changed. May cause window sizes and positions to drift, especially when using resolutions in aspect ratios other than 16:9.");

            var storeRects = configFile.Bind("General", "Save window sizes and positions", true, "Store window sizes and positions to config so it persists across game restarts. If disabled, the values are reset to defaults on game start.");
            var storedRects = configFile.Bind("General", "Window Rectangles", string.Empty, new ConfigDescription("Stored window positions and sizes.", null, "Advanced"));
            var storedRectsDic = DeserializeRects(storeRects.Value ? storedRects.Value : null);
            KoikatuAPI.Quitting += (o, e) => storedRects.Value = SerializeRects(_instances, storedRectsDic);

#if KKP || KKS
            ShowDefaultCharas = configFile.Bind("General", "Show default cards", true, "Default character and outfit cards will be added to the lists. They are visible in the root directory.");
#endif

            var enableFilesystemWatchers = configFile.Bind("General", "Automatically refresh when files change", true, "When files are added/deleted/updated the list will automatically update. If disabled you have to hit the refresh button manually when files are changed.");
            enableFilesystemWatchers.SettingChanged += (s, e) => FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;
            FolderTreeView.EnableFilesystemWatcher = enableFilesystemWatchers.Value;

            var harmony = new Harmony(Guid);
            _instances = LoadBrowsers(harmony, configFile, storedRectsDic);
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

#if KKP || KKS
        internal static bool DrawDefaultCardsToggle()
        {
            GUI.changed = false;
            var newVal = GUILayout.Toggle(ShowDefaultCharas.Value, "Show default cards");
            if (GUI.changed)
            {
                ShowDefaultCharas.Value = newVal;
                return true;
            }
            return false;
        }
#endif

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

#if !EC
            var insideStudio = KKAPI.Studio.StudioAPI.InsideStudio;
#else
            const bool insideStudio = false;
#endif
            // Initialize IFolderBrowser instances
            allInstances.RemoveAll(instance =>
            {
                string cacheKey = null;
                try
                {
                    cacheKey = GetCacheKey(instance);
                    var success = instance.Initialize(insideStudio, config, harmony);
                    if (success && windowRects.TryGetValue(cacheKey, out var storedRect))
                        instance.WindowRect = storedRect;
                    return !success;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to initialize instance of {cacheKey ?? instance?.ToString()} - {e}");
                    return true;
                }
            });

            Logger.LogInfo($"Loaded {allInstances.Count}/{allTypesCount} folder browsers: {string.Join(", ", allInstances.Select(x => x.GetType().Name).ToArray())}");
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

        private static string SerializeRects(IFolderBrowser[] instances, Dictionary<string, Rect> rectDict)
        {
            foreach (var instance in instances)
                rectDict[GetCacheKey(instance)] = instance.WindowRect;

            return string.Join("|", rectDict.Select(x =>
            {
                var rect = x.Value;
                return $"{x.Key},{rect.x:F0},{rect.y:F0},{rect.width:F0},{rect.height:F0}";
            }).ToArray());
        }

        private static string GetCacheKey(IFolderBrowser instance)
        {
            //return instance?.GetType().FullName ?? throw new Exception("this should never happen " + instance);
            return instance?.GetType().Name ?? throw new Exception("this should never happen");
        }

        private void CheckScreenSizeChange()
        {
            var newScreenHeight = Screen.height;
            if (newScreenHeight == _lastScreenHeight) return;

            var newScreenWidth = Screen.width;
            var screenRect = new Rect(0, 0, newScreenWidth, newScreenHeight);

            float scaleChange;
            int xOffset;
            if (_adaptToResolution.Value && _lastScreenHeight > 0)
            {
                // Game UI mostly scales based on height
                scaleChange = newScreenHeight / (float)_lastScreenHeight;

                // Calculate extra offset from left edge when screen aspect ratio changes because UI stays at 16:9
                const float targetAspect = 16f / 9f;
                var desiredWidth = Mathf.RoundToInt(targetAspect * newScreenHeight);
                xOffset = (newScreenWidth - desiredWidth) / 2;
            }
            else
            {
                scaleChange = 0;
                xOffset = 0;
            }

            foreach (var instance in _instances)
            {
                var rect = instance.WindowRect;
                if (rect.width == 0 || rect.height == 0)
                {
                    rect = instance.GetDefaultRect();
                }
                else
                {
                    if (scaleChange > 0)
                    {
                        rect = new Rect(Mathf.RoundToInt(xOffset + rect.x * scaleChange), Mathf.RoundToInt(rect.y * scaleChange),
                                        Mathf.RoundToInt(rect.width * scaleChange), Mathf.RoundToInt(rect.height * scaleChange));
                    }

                    // Ensure the window is not completely off-screen
                    if (!screenRect.Overlaps(rect))
                        rect = instance.GetDefaultRect();
                }

                instance.WindowRect = rect;
            }

            _lastScreenHeight = newScreenHeight;
        }
    }
}
