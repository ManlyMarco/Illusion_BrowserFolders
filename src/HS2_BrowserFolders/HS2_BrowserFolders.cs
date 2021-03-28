using BepInEx;
using BepInEx.Configuration;
using KKAPI.Maker;
using KKAPI.Studio;

namespace BrowserFolders
{
    [BepInProcess("HoneySelect2")]
    [BepInProcess("StudioNEOV2")]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {
        private IFolderBrowser _hs2MainGameFolders;

        public static ConfigEntry<bool> EnableMainGame { get; private set; }

        private void GameSpecificAwake()
        {
            EnableMainGame = Config.Bind("Main game", "Enable character folder browser in main game", true, "NOTE: This will patch the game to allow nested folder paths in the save file. If you turn this feature off or remove this plugin, any cards that are in subfolders will be removed from your game save (the cards themselves will not be affected, you will just have to readd them to your groups). Changes take effect on game restart");
            if (!StudioAPI.InsideStudio && EnableMainGame.Value)
                _hs2MainGameFolders = new MainGameFolders();
        }

        private void GameSpecificOnGui()
        {
            if (!StudioAPI.InsideStudio && !MakerAPI.InsideMaker)
                _hs2MainGameFolders?.OnGui();
        }
    }
}
