using BepInEx.Configuration;
using FreeH;
using HarmonyLib;
using Localize.Translate;
using Manager;
using UnityEngine;

namespace BrowserFolders.MainGame
{
    public class FreeHFolders : BaseFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static string _targetScene;
        private static FreeHPreviewCharaList _freeHFile;
        private static CustomFileListSelecter _customFileListSelecter;

        public FreeHFolders() : base("Character folder", Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0)) { }

        public static void Init(FreeHPreviewCharaList list, int sex)
        {
            if (_freeHFile != list && _folderTreeView != null)
            {
                // Stop events from firing
                _freeHFile = null;

                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _freeHFile = list;
                _customFileListSelecter = list.GetComponentInChildren<CustomFileListSelecter>();
                _targetScene = Scene.AddSceneName;
            }
        }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in Free H browser", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            Overlord.Init();
            return true;
        }

        protected override void DrawControlButtons()
        {
            if (BrowserFoldersPlugin.DrawDefaultCardsToggle())
                OnListRefresh();

            base.DrawControlButtons();
        }

        protected override int IsVisible()
        {
            return _freeHFile != null && _targetScene == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            if (_freeHFile != null)
                _customFileListSelecter?.Initialize();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }
    }
}