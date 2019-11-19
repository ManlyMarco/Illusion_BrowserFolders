using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class KK_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "1.4.1";

        internal static new ManualLogSource Logger { get; private set; }

        private IFolderBrowser _sceneFolders;
        private IFolderBrowser _makerFolders;
        private IFolderBrowser _classroomFolders;
        private IFolderBrowser _freeHFolders;
        private IFolderBrowser _newGameFolders;

        public static ConfigEntry<bool> EnableMaker { get; private set; }
        public static ConfigEntry<bool> EnableClassroom { get; private set; }
        public static ConfigEntry<bool> EnableFreeH { get; private set; }

        public static ConfigEntry<bool> EnableStudio { get; private set; }
        public static ConfigEntry<bool> StudioSaveOverride { get; private set; }

        public static ConfigEntry<bool> ShowDefaultCharas { get; private set; }

        internal void OnGUI()
        {
            if (_sceneFolders != null) _sceneFolders.OnGui();
            else
            {
                _makerFolders?.OnGui();
                _classroomFolders?.OnGui();
                _freeHFolders?.OnGui();
                _newGameFolders?.OnGui();
            }
        }

        internal void Start()
        {
            Logger = base.Logger;

            var browsers = LoadBrowsers();
            if (browsers.Count == 0) return;

            var maker = browsers.FirstOrDefault(x => x.Type == BrowserType.Maker);
            var classroom = browsers.FirstOrDefault(x => x.Type == BrowserType.Classroom);
            var newGame = browsers.FirstOrDefault(x => x.Type == BrowserType.NewGame);
            var freeH = browsers.FirstOrDefault(x => x.Type == BrowserType.FreeH);
            var scene = browsers.FirstOrDefault(x => x.Type == BrowserType.Scene);

            if (maker != null)
                EnableMaker = Config.AddSetting("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");

            if (classroom != null || newGame != null)
                EnableClassroom = Config.AddSetting("Main game", "Enable folder browser in classroom/new game browser", true, "Changes take effect on game restart");

            if (freeH != null)
                EnableFreeH = Config.AddSetting("Main game", "Enable folder browser in Free H browser", true, "Changes take effect on game restart");

            if (scene != null)
            {
                EnableStudio = Config.AddSetting("Chara Studio", "Enable folder browser in scene browser", true, "Changes take effect on game restart");
                StudioSaveOverride = Config.AddSetting("Chara Studio", "Save scenes to current folder", true, "When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).");
            }

            if (Application.productName == "CharaStudio")
            {
                if (EnableStudio != null && EnableStudio.Value)
                    _sceneFolders = scene;
            }
            else
            {
                if (Application.productName == "Koikatsu Party")
                    ShowDefaultCharas = Config.AddSetting("Main game", "Show default character cards", true);

                if (EnableMaker != null && EnableMaker.Value)
                    _makerFolders = maker;

                if (EnableClassroom != null && EnableClassroom.Value)
                {
                    _classroomFolders = classroom;
                    _newGameFolders = newGame;
                }

                if (EnableFreeH != null && EnableFreeH.Value)
                    _freeHFolders = freeH;
            }
        }

        private List<IFolderBrowser> LoadBrowsers()
        {
            try
            {
                var assHooks = Application.productName == "Koikatsu Party" ? "KK_BrowserFolders_Hooks_KKP" : "KK_BrowserFolders_Hooks_KK";
                return GetBrowsers(Assembly.Load(assHooks)).ToList();
            }
            catch (FileNotFoundException ex) { Logger.LogWarning("Failed to load browsers - " + ex); }

            return new List<IFolderBrowser>();
        }

        private static IEnumerable<IFolderBrowser> GetBrowsers(Assembly ass)
        {
            var browserType = typeof(IFolderBrowser);
            return ass.GetTypesSafe()
                .Where(x => x.IsClass && !x.IsAbstract && browserType.IsAssignableFrom(x))
                .Attempt(Activator.CreateInstance)
                .Cast<IFolderBrowser>();
        }
    }
}
