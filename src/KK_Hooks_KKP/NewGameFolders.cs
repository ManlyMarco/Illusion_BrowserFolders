using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.NewGame)]
    public class NewGameFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;

        private static string _targetScene;

        private static EntryPlayer _newGame;
        private Rect _windowRect;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        public NewGameFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Overlord.Init();
        }

        public static void Init(EntryPlayer list, int sex)
        {
            if (_newGame != list)
            {
                // Stop events from firing
                _newGame = null;

                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _newGame = list;
                _targetScene = Scene.Instance.AddSceneName;
            }
        }

        public void OnGui()
        {
            if (_newGame != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_windowRect.IsEmpty())
                    _windowRect = GetFullscreenBrowserRect();

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select character folder", OnFolderChanged, drawAdditionalButtons: () =>
                {
                    if (Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();
                });
            }
            else
            {
                _folderTreeView?.StopMonitoringFiles();
            }
        }

        private static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.73), (int)(Screen.height * 0.55f), (int)(Screen.width * 0.2), (int)(Screen.height * 0.3));
        }

        private static void OnFolderChanged()
        {
            if (_newGame != null)
                _newGame.CreateMaleList();
        }
    }
}