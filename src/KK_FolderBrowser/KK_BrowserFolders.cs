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
        public const string Version = "1.5";

        internal static new ManualLogSource Logger { get; private set; }

        private static bool _insideStudio;
        private static bool _insideParty;

        private IFolderBrowser _sceneFolders;
        private IFolderBrowser _makerFolders;
        private IFolderBrowser _classroomFolders;
        private IFolderBrowser _freeHFolders;
        private IFolderBrowser _newGameFolders;
        private IFolderBrowser _studioCharaFolders;
        private IFolderBrowser _makerOutfitFolders;
        private IFolderBrowser _studioOutfitFolders;

        
        public static ConfigEntry<bool> EnableMaker { get; private set; }

        public static ConfigEntry<bool> EnableMakerOutfit { get; private set; }
        public static ConfigEntry<bool> EnableClassroom { get; private set; }
        public static ConfigEntry<bool> EnableFreeH { get; private set; }

        public static ConfigEntry<bool> EnableStudio { get; private set; }
        public static ConfigEntry<bool> EnableStudioChara { get; private set; }
        public static ConfigEntry<bool> StudioSaveOverride { get; private set; }
        public static ConfigEntry<bool> EnableStudioOutfit { get; private set; }

        public static ConfigEntry<bool> ShowDefaultCharas { get; private set; }

        internal void OnGUI()
        {
            if (_insideStudio)
            {
                _sceneFolders?.OnGui();
                _studioCharaFolders?.OnGui();
                _studioOutfitFolders?.OnGui();
            }
            else
            {
                _makerFolders?.OnGui();
                _classroomFolders?.OnGui();
                _freeHFolders?.OnGui();
                _newGameFolders?.OnGui();
                _makerOutfitFolders?.OnGui();
            }
        }

        private void Awake()
        {
            Logger = base.Logger;
            _insideStudio = Application.productName == "CharaStudio";
            _insideParty = Application.productName == "Koikatsu Party";

            var browsers = LoadBrowsers();
            if (browsers.Count == 0) return;

            var maker = browsers.FirstOrDefault(x => x.Key == BrowserType.Maker).Value;
            var makerOutfit = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerOutfit).Value;
            var classroom = browsers.FirstOrDefault(x => x.Key == BrowserType.Classroom).Value;
            var newGame = browsers.FirstOrDefault(x => x.Key == BrowserType.NewGame).Value;
            var freeH = browsers.FirstOrDefault(x => x.Key == BrowserType.FreeH).Value;
            var scene = browsers.FirstOrDefault(x => x.Key == BrowserType.Scene).Value;
            var studioChara = browsers.FirstOrDefault(x => x.Key == BrowserType.StudioChara).Value;
            var studioOutfit = browsers.FirstOrDefault(x => x.Key == BrowserType.StudioOutfit).Value;

            if (maker != null)
                EnableMaker = Config.Bind("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");

            if (makerOutfit != null)
                EnableMakerOutfit = Config.Bind("Main game", "Enable folder browser in maker for outfits", true, "Changes take effect on game restart");

            if (classroom != null || newGame != null)
                EnableClassroom = Config.Bind("Main game", "Enable folder browser in classroom/new game browser", true, "Changes take effect on game restart");

            if (freeH != null)
                EnableFreeH = Config.Bind("Main game", "Enable folder browser in Free H browser", true, "Changes take effect on game restart");

            if (scene != null || studioChara != null || studioOutfit != null)
            {
                EnableStudio = Config.Bind("Chara Studio", "Enable folder browser in scene browser", true, "Changes take effect on game restart");
                EnableStudioChara = Config.Bind("Chara Studio", "Enable folder browser in character browser", true, "Changes take effect on game restart");
                StudioSaveOverride = Config.Bind("Chara Studio", "Save scenes to current folder", true, "When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).");
                EnableStudioOutfit = Config.Bind("Chara Studio", "Enable folder browser in outfit browser", true, "Changes take effect on game restart");
            }

            if (_insideStudio)
            {
                if (scene != null && EnableStudio.Value)
                    _sceneFolders = (IFolderBrowser)Activator.CreateInstance(scene);

                if (studioChara != null && EnableStudioChara.Value)
                    _studioCharaFolders = (IFolderBrowser)Activator.CreateInstance(studioChara);

                if (studioOutfit != null && EnableStudioOutfit.Value)
                    _studioOutfitFolders = (IFolderBrowser)Activator.CreateInstance(studioOutfit);
            }
            else
            {
                if (_insideParty)
                    ShowDefaultCharas = Config.Bind("Main game", "Show default character cards", true);

                if (maker != null && EnableMaker.Value)
                    _makerFolders = (IFolderBrowser)Activator.CreateInstance(maker);

                if (makerOutfit != null && EnableMakerOutfit.Value)
                    _makerOutfitFolders = (IFolderBrowser)Activator.CreateInstance(makerOutfit);

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
                var assHooks = _insideParty ? "KK_BrowserFolders_Hooks_KKP" : "KK_BrowserFolders_Hooks_KK";
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
