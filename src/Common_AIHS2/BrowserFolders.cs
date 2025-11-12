using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Common;
using KKAPI;
using KKAPI.Maker;
using KKAPI.Studio;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = Constants.Guid;
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger { get; private set; }

        private IFolderBrowser _sceneFolders;
        private IFolderBrowser _studioCharaFolders;
        private IFolderBrowser _studioOutfitFolders;
        private IFolderBrowser _makerCharaFolders;
        private IFolderBrowser _makerClothesFolders;
        
        public static ConfigEntry<bool> EnableMaker { get; private set; }
        public static ConfigEntry<bool> EnableMakerCoord { get; private set; }
        public static ConfigEntry<bool> EnableStudio { get; private set; }
        public static ConfigEntry<bool> EnableStudioChara { get; private set; }
        public static ConfigEntry<bool> EnableStudioOutfit { get; private set; }
        public static ConfigEntry<bool> StudioSaveOverride { get; private set; }
        public static ConfigEntry<bool> EnableFilesystemWatchers { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            // TODO need to collect a list of browsers then init them and add update call

            EnableMaker = Config.Bind("Main game", "Enable character folder browser in maker", true, "Changes take effect on game restart");
            if (!StudioAPI.InsideStudio && EnableMaker.Value) _makerCharaFolders = new MakerFolders();

            EnableMakerCoord = Config.Bind("Main game", "Enable clothes folder browser in maker", true, "Changes take effect on game restart");
            if (!StudioAPI.InsideStudio && EnableMakerCoord.Value) _makerClothesFolders = new MakerOutfitFolders();

            EnableStudio = Config.Bind("Chara Studio", "Enable folder browser in scene browser", true, "Changes take effect on game restart");
            if (StudioAPI.InsideStudio && EnableStudio.Value) _sceneFolders = new SceneFolders();

            EnableStudioChara = Config.Bind("Chara Studio", "Enable folder browser in character browser", true, "Changes take effect on game restart");
            if (StudioAPI.InsideStudio && EnableStudioChara.Value) _studioCharaFolders = new StudioCharaFolders();

            EnableStudioOutfit = Config.Bind("Chara Studio", "Enable folder browser in outfit browser", true, "Changes take effect on game restart");
            if (StudioAPI.InsideStudio && EnableStudioOutfit.Value) _studioOutfitFolders = new StudioOutfitFolders();

            StudioSaveOverride = Config.Bind("Chara Studio", "Save scenes to current folder", false, "When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).");

            EnableFilesystemWatchers = Config.Bind("General", "Automatically refresh when files change", true, "When files are added/deleted/updated the list will automatically update. If disabled you have to hit the refresh button manually when files are changed.");
            EnableFilesystemWatchers.SettingChanged += (s, e) => FolderTreeView.EnableFilesystemWatcher = EnableFilesystemWatchers.Value;
            FolderTreeView.EnableFilesystemWatcher = EnableFilesystemWatchers.Value;

            GameSpecificAwake();
        }

        private void OnGUI()
        {
            if (StudioAPI.InsideStudio)
            {
                _sceneFolders?.OnGui();
                _studioCharaFolders?.OnGui();
                _studioOutfitFolders?.OnGui();
            }
            else if (MakerAPI.InsideMaker)
            {
                _makerCharaFolders?.OnGui();
                _makerClothesFolders?.OnGui();
            }

            GameSpecificOnGui();
        }

        internal static string UserDataPath { get; } = Utils.NormalizePath(Path.Combine(Paths.GameRootPath, "UserData")); // UserData.Path
    }
}
