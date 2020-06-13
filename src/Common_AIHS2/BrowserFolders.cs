using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "1.6";

        internal static new ManualLogSource Logger { get; private set; }

        private IFolderBrowser _sceneFolders;
        private IFolderBrowser _studioCharaFolders;
        private IFolderBrowser _makerCharaFolders;

        public static ConfigEntry<bool> EnableMaker { get; private set; }
        public static ConfigEntry<bool> EnableStudio { get; private set; }
        public static ConfigEntry<bool> EnableStudioChara { get; private set; }
        public static ConfigEntry<bool> StudioSaveOverride { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            EnableMaker = Config.Bind("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");
            if (!StudioAPI.InsideStudio && EnableMaker.Value) _makerCharaFolders = new MakerFolders();

            EnableStudio = Config.Bind("Chara Studio", "Enable folder browser in scene browser", true, "Changes take effect on game restart");
            if (StudioAPI.InsideStudio && EnableStudio.Value) _sceneFolders = new SceneFolders();

            EnableStudioChara = Config.Bind("Chara Studio", "Enable folder browser in character browser", true, "Changes take effect on game restart");
            if (StudioAPI.InsideStudio && EnableStudioChara.Value) _studioCharaFolders = new StudioCharaFolders();

            StudioSaveOverride = Config.Bind("Chara Studio", "Save scenes to current folder", false, "When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).");
        }

        private void OnGUI()
        {
            _sceneFolders?.OnGui();
            _studioCharaFolders?.OnGui();
            _makerCharaFolders?.OnGui();
        }

        internal static string UserDataPath { get; } = Utils.NormalizePath(Path.Combine(Paths.GameRootPath, "UserData")); // UserData.Path
    }
}
