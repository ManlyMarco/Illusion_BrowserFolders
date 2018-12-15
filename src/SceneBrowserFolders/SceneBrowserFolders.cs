using System.Diagnostics;
using BepInEx;
using UnityEngine;

namespace SceneBrowserFolders
{
    [BepInPlugin(Guid, "Scene Browser Folders", "1.0")]
    internal class SceneBrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.SceneBrowserFolders";

        private SceneFolders _sceneFolders;
        private MakerFolders _makerFolders;

        private void OnGUI()
        {
            if (_sceneFolders != null) _sceneFolders.OnGui();
            else if (_makerFolders != null) _makerFolders.OnGui();
        }

        private void Awake()
        {
            if (Application.productName == "CharaStudio")
                _sceneFolders = new SceneFolders();
            else
                _makerFolders = new MakerFolders();
        }
    }
}
