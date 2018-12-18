using System.ComponentModel;
using BepInEx;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    public class KK_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "1.1";

        private SceneFolders _sceneFolders;
        private MakerFolders _makerFolders;

        [DisplayName("Enable folder browser in maker")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableMaker { get; private set; }
        [DisplayName("Enable folder browser in studio")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableStudio { get; private set; }
        [DisplayName("Save scenes to current folder")]
        [Description("When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).")]
        public static ConfigWrapper<bool> StudioSaveOverride { get; private set; }

        private void OnGUI()
        {
            if (_sceneFolders != null) _sceneFolders.OnGui();
            else if (_makerFolders != null) _makerFolders.OnGui();
        }

        private void Awake()
        {
            EnableMaker = new ConfigWrapper<bool>(nameof(EnableMaker), this, true);
            EnableStudio = new ConfigWrapper<bool>(nameof(EnableStudio), this, true);
            StudioSaveOverride = new ConfigWrapper<bool>(nameof(StudioSaveOverride), this, false);

            if (Application.productName == "CharaStudio")
            {
                if (EnableStudio.Value)
                    _sceneFolders = new SceneFolders();
            }
            else
            {
                if (EnableMaker.Value)
                    _makerFolders = new MakerFolders();
            }
        }
    }
}
