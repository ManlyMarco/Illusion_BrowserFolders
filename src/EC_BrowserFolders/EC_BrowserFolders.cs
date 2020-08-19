using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BrowserFolders.Hooks;
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
        private IFolderBrowser _makerFolders;
        private IFolderBrowser _makerPoseFolders;
        private IFolderBrowser _makerPoseSaveFolders;
        private IFolderBrowser _makerMapFolders;
        private IFolderBrowser _makerMapSaveFolders;
        private IFolderBrowser _makerSceneFolders;
        //private IFolderBrowser _makerHPoseIKFolders;

        public static ConfigEntry<bool> EnableMakerOutfit { get; private set; }
        public static ConfigEntry<bool> EnableMakerChara { get; private set; }
        public static ConfigEntry<bool> EnableMakerPose { get; private set; }
        public static ConfigEntry<bool> EnableMakerMap { get; private set; }
        public static ConfigEntry<bool> EnableMakerScene { get; private set; }
        //public static ConfigEntry<bool> EnableMakerHPoseIK { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            const string settingGroup = "Enable folder browser for";
            const string restartNote = "Changes take effect on game restart";
            EnableMakerChara = Config.Bind(settingGroup, "Characters", true, restartNote);
            EnableMakerOutfit = Config.Bind(settingGroup, "Outfits", true, restartNote);
            EnableMakerPose = Config.Bind(settingGroup, "Poses", true, restartNote);
            EnableMakerMap = Config.Bind(settingGroup, "Maps", true, restartNote);
            EnableMakerScene = Config.Bind(settingGroup, "Scenes", true, restartNote);
            //EnableMakerHPoseIK = Config.Bind(settingGroup, "H Pose IK", true, restartNote);

            if (EnableMakerChara.Value) _makerFolders = new MakerCharaFolders();
            if (EnableMakerOutfit.Value) _makerOutfitFolders = new MakerOutfitFolders();
            if (EnableMakerPose.Value) { _makerPoseFolders = new MakerPoseFolders(); _makerPoseSaveFolders = new MakerPoseSaveFolders(); }
            if (EnableMakerMap.Value) { _makerMapFolders = new MakerMapFolders(); _makerMapSaveFolders = new MakerMapSaveFolders(); }
            if (EnableMakerScene.Value) _makerSceneFolders = new MakerSceneFolders();
            //if (EnableMakerHPoseIK.Value) _makerHPoseIKFolders = new MakerHPoseIKFolders();
        }

        private void OnGUI()
        {
            _makerFolders?.OnGui();
            _makerOutfitFolders?.OnGui();
            if (_makerPoseFolders != null)
            {
                _makerPoseFolders.OnGui();
                _makerPoseSaveFolders.OnGui();
            }
            if (_makerMapFolders != null)
            {
                _makerMapFolders.OnGui();
                _makerMapSaveFolders.OnGui();
            }
            _makerSceneFolders?.OnGui();
            //_makerHPoseIKFolders?.OnGui();
        }
    }
}