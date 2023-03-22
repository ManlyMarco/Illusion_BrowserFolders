using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
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
                _targetScene = Scene.AddSceneName;
            }
        }

        public void OnGui()
        {
            if (_newGame != null && _targetScene == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade)
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
            return new Rect(0, (int)(Screen.height * 0.35f), (int)(Screen.width * 0.133), (int)(Screen.height * 0.5));
        }

        private static void OnFolderChanged()
        {
            if (_newGame != null)
                _newGame.CreateMaleList(false);
        }
    }
}