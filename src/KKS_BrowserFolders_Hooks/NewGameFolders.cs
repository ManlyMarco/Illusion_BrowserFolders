using KKAPI.Utilities;
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
                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _newGame = list;
                _targetScene = Scene.AddSceneName;
            }
        }

        public void OnGui()
        {
            if (_newGame != null && _targetScene == Scene.AddSceneName)
            {
                var screenRect = GetFullscreenBrowserRect();
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                IMGUIUtils.EatInputInRect(screenRect);
            }
            else
            {
                _folderTreeView?.StopMonitoringFiles();
            }
        }

        private static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(0), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.133), (int)(Screen.height * 0.5));
        }

        private static void OnFolderChanged()
        {
            if (_newGame != null)
                _newGame.CreateMaleList(false);
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();

                    if (GUILayout.Button("Refresh thumbnails"))
                    {
                        _folderTreeView.ResetTreeCache();
                        OnFolderChanged();
                    }

                    if (GUILayout.Button("Open folder in explorer"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}