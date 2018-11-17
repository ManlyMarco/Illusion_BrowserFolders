using BepInEx;

namespace SceneBrowserFolders
{
    [BepInPlugin(Guid, "Scene Browser Folders", "1.0")]
    [BepInProcess("CharaStudio.exe")]
    internal class SceneBrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.SceneBrowserFolders";

        private SceneFolders _sceneFolders;

        private void OnGUI()
        {
            _sceneFolders.OnGui();
        }

        private void Awake()
        {
            _sceneFolders = new SceneFolders();
            //StudioCharaFolders.Init();
        }
    }
}
