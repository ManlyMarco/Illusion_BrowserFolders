using System.ComponentModel;
using BepInEx;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    public class KK_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "1.2";

        private SceneFolders _sceneFolders;
        private MakerFolders _makerFolders;
        private ClassroomFolders _classroomFolders;
        private FreeHFolders _freeHFolders;

        [DisplayName("Enable folder browser in maker")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableMaker { get; private set; }
        [DisplayName("Enable folder browser in classroom/new game browser")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableClassroom { get; private set; }

        [DisplayName("Enable folder browser in Free H browser")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableFreeH { get; private set; }

        [DisplayName("Enable folder browser in scene browser")]
        [Category("Chara Studio")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableStudio { get; private set; }

        [DisplayName("Save scenes to current folder")]
        [Category("Chara Studio")]
        [Description("When you select a custom folder to load a scene from, " +
                     "newly saved scenes will be saved to this folder.\n" +
                     "If disabled, scenes are always saved to default folder (studio/scene).")]
        public static ConfigWrapper<bool> StudioSaveOverride { get; private set; }

        private void OnGUI()
        {
            if (_sceneFolders != null) _sceneFolders.OnGui();
            else
            {
                _makerFolders?.OnGui();
                _classroomFolders?.OnGui();
                _freeHFolders?.OnGui();
            }
        }

        private void Awake()
        {
            EnableMaker = new ConfigWrapper<bool>(nameof(EnableMaker), this, true);
            EnableClassroom = new ConfigWrapper<bool>(nameof(EnableClassroom), this, true);
            EnableFreeH = new ConfigWrapper<bool>(nameof(EnableFreeH), this, true);
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
                if (EnableClassroom.Value)
                    _classroomFolders = new ClassroomFolders();
                if (EnableFreeH.Value)
                    _freeHFolders = new FreeHFolders();
            }
        }
    }
}
