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
    [BepInPlugin(Guid, "Maker Browser Folders", Version)]
 
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class EC_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "2.1";

        internal static new ManualLogSource Logger { get; private set; }


        private static bool _insideParty;
        private IFolderBrowser _makerOutfitFolders;

        private IFolderBrowser _OutfitFolders;

        private IFolderBrowser _makerFolders;

        private IFolderBrowser _makerPoseFolders;

        private IFolderBrowser _makerPoseSFolders;

        private IFolderBrowser _makerMapFolders;

        private IFolderBrowser _makerMapSFolders;

        private IFolderBrowser _makerSceneFolders;

        private IFolderBrowser _makerHPoseIKFolders;

        public static ConfigEntry<bool> EnableMakerOutfit { get; private set; }

        public static ConfigEntry<bool> EnableOutfit { get; private set; }
        public static ConfigEntry<bool> EnableMaker { get; private set; }

        public static ConfigEntry<bool> EnableMakerPose { get; private set; }

        public static ConfigEntry<bool> EnableMakerPoseS { get; private set; }

        public static ConfigEntry<bool> EnableMakerMap { get; private set; }

        public static ConfigEntry<bool> EnableMakerMapS { get; private set; }

        public static ConfigEntry<bool> EnableMakerScene { get; private set; }

        public static ConfigEntry<bool> EnableMakerHPoseIK { get; private set; }




        public static ConfigEntry<bool> ShowDefaultCharas { get; private set; }
        public static ConfigEntry<bool> RandomCharaSubfolders { get; private set; }



        private void Awake()
        {
            Logger = base.Logger;
          
            _insideParty = Application.productName == "EmotionCreators Party";

            if (_insideParty == true)  return;
            var browsers = LoadBrowsers();
            if (browsers.Count == 0) return;

            var maker = browsers.FirstOrDefault(x => x.Key == BrowserType.Maker).Value;
            var makerOutfit = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerOutfit).Value;
            var Outfit = browsers.FirstOrDefault(x => x.Key == BrowserType.Outfit).Value;
            var makerPose = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerPose).Value;
            var makerPoseS = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerPoseS).Value;
            var makerMap = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerMap).Value;
            var makerMapS = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerMapS).Value;
            var makerScene = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerScene).Value;
            var makerHPoseIK = browsers.FirstOrDefault(x => x.Key == BrowserType.MakerHPoseIK).Value;

            if (maker != null)
                EnableMaker = Config.Bind("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");


            if (makerOutfit != null)
                EnableMakerOutfit = Config.Bind("Main game", "Enable folder browser in maker for outfits", true, "Changes take effect on game restart");

            if (Outfit != null)
                EnableOutfit = Config.Bind("Main game", "Enable folder browser in maker for scene outfits", true, "Changes take effect on game restart");

            if (makerPose != null)
                EnableMakerPose = Config.Bind("Main game", "Enable folder browser in maker for pose", true, "Changes take effect on game restart");

            if (makerPoseS != null)
                EnableMakerPoseS = Config.Bind("Main game", "Enable folder browser in maker for posesave", true, "Changes take effect on game restart");

            if (makerMap != null)
                EnableMakerMap = Config.Bind("Main game", "Enable folder browser in maker for map", true, "Changes take effect on game restart");

            if (makerMapS != null)
                EnableMakerMapS = Config.Bind("Main game", "Enable folder browser in maker for mapsave", true, "Changes take effect on game restart");

            if (makerScene != null)
                EnableMakerScene = Config.Bind("Main game", "Enable folder browser in maker for scene", true, "Changes take effect on game restart");

            if (makerHPoseIK != null)
                EnableMakerHPoseIK = Config.Bind("Main game", "Enable folder browser in maker for hik", true, "Changes take effect on game restart");

            if (maker != null && EnableMaker.Value)
            {
                _makerFolders = (IFolderBrowser)Activator.CreateInstance(maker);
            }

            if (makerOutfit != null && EnableMakerOutfit.Value)
            {
                _makerOutfitFolders = (IFolderBrowser)Activator.CreateInstance(makerOutfit);
            }

            if (Outfit != null && EnableOutfit.Value)
            {
                _OutfitFolders = (IFolderBrowser)Activator.CreateInstance(Outfit);
            }

            if (makerPose != null && EnableMakerPose.Value)
            {
                _makerPoseFolders = (IFolderBrowser)Activator.CreateInstance(makerPose);
            }

            if (makerPoseS != null && EnableMakerPoseS.Value)
            {
                _makerPoseSFolders = (IFolderBrowser)Activator.CreateInstance(makerPoseS);
            }

            if (makerMap != null && EnableMakerMap.Value)
            {
                _makerMapFolders = (IFolderBrowser)Activator.CreateInstance(makerMap);
            }

            if (makerMapS != null && EnableMakerMapS.Value)
            {
                _makerMapSFolders = (IFolderBrowser)Activator.CreateInstance(makerMapS);
            }

            if (makerScene != null && EnableMakerScene.Value)
            {
                _makerSceneFolders = (IFolderBrowser)Activator.CreateInstance(makerScene);
            }

            if (makerHPoseIK != null && EnableMakerHPoseIK.Value)
            {
                _makerHPoseIKFolders = (IFolderBrowser)Activator.CreateInstance(makerHPoseIK);
            }
        }

        internal void OnGUI()
        {

            _makerFolders?.OnGui();
            _makerOutfitFolders?.OnGui();
            _makerPoseFolders?.OnGui();
            _makerPoseSFolders?.OnGui();
            _makerMapFolders?.OnGui();
            _makerMapSFolders?.OnGui();
            _OutfitFolders?.OnGui();
            _makerSceneFolders?.OnGui();
            _makerHPoseIKFolders?.OnGui();
        }

        private static List<KeyValuePair<BrowserType, Type>> LoadBrowsers()
        {
            try
            {
                var assHooks = _insideParty ? "KK_BrowserFolders_Hooks_KKP" : "EC_BrowserFolders_Hooks_EC";
                if (assHooks != null)
         
       
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