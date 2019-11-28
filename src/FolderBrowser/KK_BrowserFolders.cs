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
        public const string Version = "1.4.2";

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

        private void Awake()
        {
            Logger = base.Logger;

            var browsers = LoadBrowsers();
            if (browsers.Count == 0) return;

            var maker = browsers.FirstOrDefault(x => x.Key == BrowserType.Maker).Value;
            var classroom = browsers.FirstOrDefault(x => x.Key == BrowserType.Classroom).Value;
            var newGame = browsers.FirstOrDefault(x => x.Key == BrowserType.NewGame).Value;
            var freeH = browsers.FirstOrDefault(x => x.Key == BrowserType.FreeH).Value;
            var scene = browsers.FirstOrDefault(x => x.Key == BrowserType.Scene).Value;

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
                if (scene != null && EnableStudio.Value)
                    _sceneFolders = (IFolderBrowser)Activator.CreateInstance(scene);
            }
            else
            {
                if (Application.productName == "Koikatsu Party")
                    ShowDefaultCharas = Config.AddSetting("Main game", "Show default character cards", true);

                if (maker != null && EnableMaker.Value)
                    _makerFolders = (IFolderBrowser)Activator.CreateInstance(maker);

                if (EnableClassroom != null && EnableClassroom.Value)
                {
                    if (classroom != null) _classroomFolders = (IFolderBrowser)Activator.CreateInstance(classroom);
                    if (newGame != null) _newGameFolders = (IFolderBrowser)Activator.CreateInstance(newGame);
                }

                if (freeH != null && EnableFreeH.Value)
                    _freeHFolders = (IFolderBrowser)Activator.CreateInstance(freeH);
            }
        }

        private static List<KeyValuePair<BrowserType, Type>> LoadBrowsers()
        {
            try
            {
                var assHooks = Application.productName == "Koikatsu Party" ? "KK_BrowserFolders_Hooks_KKP" : "KK_BrowserFolders_Hooks_KK";
                return GetBrowsers(Assembly.Load(assHooks));
            }
            catch (FileNotFoundException ex) { Logger.LogWarning("Failed to load browsers - " + ex); }

            return new List<KeyValuePair<BrowserType, Type>>();
        }

        private static List<KeyValuePair<BrowserType, Type>> GetBrowsers(Assembly ass)
        {
            var query = from t in ass.GetTypesSafe()
                        where t.IsClass
                        let attr = t.GetCustomAttributes(false).OfType<BrowserTypeAttribute>().FirstOrDefault()
                        where attr != null
                        group t by attr.BrowserType;
            return query.Select(x => new KeyValuePair<BrowserType, Type>(x.Key, x.Single())).ToList();
        }
    }
}
