using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace BrowserFolders.MainGame
{
    public class NewGameFolders : IFolderBrowser
    {
        private bool _guiVisible;
        private static FolderTreeView _folderTreeView;

        private static string _targetScene;

        private static EntryPlayer _newGame;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        public static void Init(EntryPlayer list, int sex)
        {
            if (_newGame != list && _folderTreeView != null)
            {
                // Stop events from firing
                _newGame = null;

                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _newGame = list;
                _targetScene = Scene.AddSceneName;
            }
        }

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            if (isStudio) return false;

            _folderTreeView = new FolderTreeView(BrowserFoldersPlugin.UserDataPath, Overlord.GetDefaultPath(0))
            {
                CurrentFolderChanged = OnListRefresh
            };

            Overlord.Init();

            return true;
        }

        public void Update()
        {
            var visible = ClassroomFolders.EnableClassroom.Value && _newGame != null && _targetScene == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade;
            
            if(_guiVisible && !visible)
                _folderTreeView?.StopMonitoringFiles();

            _guiVisible = visible;
        }

        public void OnGui()
        {
            if (!_guiVisible) return;

            BaseFolderBrowser.DisplayFolderWindow(this, drawAdditionalButtons: () =>
            {
                if (BrowserFoldersPlugin.DrawDefaultCardsToggle())
                    OnListRefresh();
            });
        }

        public FolderTreeView TreeView => _folderTreeView;

        public string Title => "Character folder";

        public Rect WindowRect { get; set; }

        public Rect GetDefaultRect()
        {
            return new Rect(0, (int)(Screen.height * 0.35f), (int)(Screen.width * 0.133), (int)(Screen.height * 0.5));
        }

        public void OnListRefresh()
        {
            if (_newGame != null)
                _newGame.CreateMaleList(false);
        }
    }
}