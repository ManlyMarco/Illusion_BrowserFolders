using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BrowserFolders.Hooks.EC;
using Common;
using KKAPI;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class EC_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = Constants.Guid;
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger { get; private set; }


        private IFolderBrowser _makerOutfitFolders;
        private IFolderBrowser _outfitFolders;
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

        private void Awake()
        {
            Logger = base.Logger;

            EnableMaker = Config.Bind("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");
            EnableMakerOutfit = Config.Bind("Main game", "Enable folder browser in maker for outfits", true, "Changes take effect on game restart");
            EnableOutfit = Config.Bind("Main game", "Enable folder browser in maker for scene outfits", true, "Changes take effect on game restart");
            EnableMakerPose = Config.Bind("Main game", "Enable folder browser in maker for pose", true, "Changes take effect on game restart");
            EnableMakerPoseS = Config.Bind("Main game", "Enable folder browser in maker for posesave", true, "Changes take effect on game restart");
            EnableMakerMap = Config.Bind("Main game", "Enable folder browser in maker for map", true, "Changes take effect on game restart");
            EnableMakerMapS = Config.Bind("Main game", "Enable folder browser in maker for mapsave", true, "Changes take effect on game restart");
            EnableMakerScene = Config.Bind("Main game", "Enable folder browser in maker for scene", true, "Changes take effect on game restart");
            EnableMakerHPoseIK = Config.Bind("Main game", "Enable folder browser in maker for hik", true, "Changes take effect on game restart");

            if (EnableMaker.Value) _makerFolders = new MakerFolders();
            if (EnableMakerOutfit.Value) _makerOutfitFolders = new MakerOutfitFolders();
            if (EnableOutfit.Value) _outfitFolders = new OutfitFolders();
            if (EnableMakerPose.Value) _makerPoseFolders = new MakerPoseFolders();
            if (EnableMakerPoseS.Value) _makerPoseSFolders = new MakerPoseSFolders();
            if (EnableMakerMap.Value) _makerMapFolders = new MakerMapFolders();
            if (EnableMakerMapS.Value) _makerMapSFolders = new MakerMapSFolders();
            if (EnableMakerScene.Value) _makerSceneFolders = new MakerSceneFolders();
            if (EnableMakerHPoseIK.Value) _makerHPoseIKFolders = new MakerHPoseIKFolders();
        }

        internal void OnGUI()
        {
            _makerFolders?.OnGui();
            _makerOutfitFolders?.OnGui();
            _makerPoseFolders?.OnGui();
            _makerPoseSFolders?.OnGui();
            _makerMapFolders?.OnGui();
            _makerMapSFolders?.OnGui();
            _outfitFolders?.OnGui();
            _makerSceneFolders?.OnGui();
            _makerHPoseIKFolders?.OnGui();
        }
    }
}