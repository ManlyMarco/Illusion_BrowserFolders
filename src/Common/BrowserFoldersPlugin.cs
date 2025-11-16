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
        internal static ConfigEntry<bool> ShowDefaultCards { get; private set; }
        internal static bool DrawDefaultCardsToggle()
        {
            GUI.changed = false;
            var newVal = GUILayout.Toggle(ShowDefaultCards.Value, "Show default cards");
            if (GUI.changed)
            {
                ShowDefaultCards.Value = newVal;
                return true;
            }
            return false;
        }
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

            _adaptToResolution = configFile.Bind("General", "Scale window size and position on resolution change", true, "Attempt to automatically adjust sizes and positions of all windows whenever game resolution is changed. May cause window sizes and positions to drift, especially when using resolutions in aspect ratios other than 16:9.");

            var storeRects = configFile.Bind("General", "Save window sizes and positions", true, "Store window sizes and positions to config so it persists across game restarts. If disabled, the values are reset to defaults on game start.");
            var storedRects = configFile.Bind("General", "Window sizes and positions", string.Empty, new ConfigDescription("Stored window rectangles in screen size percentages.", null, "Advanced"));
            var storedRectsDic = DeserializeRects(storeRects.Value ? storedRects.Value : null);
            KoikatuAPI.Quitting += (o, e) => storedRects.Value = SerializeRects(_instances, storedRectsDic);

#if KKP || KKS
            ShowDefaultCards = configFile.Bind("General", "Show default cards", true, "Default character and outfit cards will be added to the lists. They are visible in the root directory.");
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

        private static IFolderBrowser[] LoadBrowsers(Harmony harmony, ConfigFile config, Dictionary<string, Rect> windowRects)
        {
            // Collect all IFolderBrowser instances
            var allTypes = AccessTools.GetTypesFromAssembly(typeof(BrowserFoldersPlugin).Assembly).Where(x => typeof(IFolderBrowser).IsAssignableFrom(x) && !x.IsAbstract);
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

        #region Storing window rects
        private static Dictionary<string, Rect> DeserializeRects(string storedRectsValue)
        {
            if (string.IsNullOrEmpty(storedRectsValue)) return new Dictionary<string, Rect>();

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            return storedRectsValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Attempt(x =>
            {
                var parts = x.Split(';');
                var key = parts[0];
                // Parse as percentage
                var px = float.Parse(parts[1]);
                var py = float.Parse(parts[2]);
                var pwidth = float.Parse(parts[3]);
                var pheight = float.Parse(parts[4]);
                var rect = new Rect((int)(screenWidth * px), (int)(screenHeight * py), (int)(screenWidth * pwidth), (int)(screenHeight * pheight));
                return new KeyValuePair<string, Rect>(key, rect);
            }).ToDictionary(x => x.Key, x => x.Value);
        }
        private static string SerializeRects(IFolderBrowser[] instances, Dictionary<string, Rect> rectDict)
        {
            foreach (var instance in instances)
                rectDict[GetCacheKey(instance)] = instance.WindowRect;

            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            return string.Join("|", rectDict.Select(x =>
            {
                var rect = x.Value;
                // Store as percentage of resolution
                var px = rect.x / screenWidth;
                var py = rect.y / screenHeight;
                var pwidth = rect.width / screenWidth;
                var pheight = rect.height / screenHeight;
                return $"{x.Key};{px:F3};{py:F3};{pwidth:F3};{pheight:F3}";
            }).ToArray());
        }
        private static string GetCacheKey(IFolderBrowser instance)
        {
            return instance?.GetType().Name ?? throw new Exception("this should never happen");
        }

        #endregion

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
