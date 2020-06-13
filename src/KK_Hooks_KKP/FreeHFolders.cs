using System.IO;
using FreeH;
using Localize.Translate;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.FreeH)]
    public class FreeHFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        //private static bool _isLive;
        private static string _targetScene;
        private static FreeHPreviewCharaList _freeHFile;
        private static CustomFileListSelecter _customFileListSelecter;

        public FreeHFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Overlord.Init();
        }

        public static void Init(FreeHPreviewCharaList list, int sex)
        {
            if (_freeHFile != list)
            {
                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _freeHFile = list;
                _customFileListSelecter = list.GetComponentInChildren<CustomFileListSelecter>();
                _targetScene = Scene.Instance.AddSceneName;
            }
        }

        private static void OnFolderChanged()
        {
            if (_freeHFile != null)
                _customFileListSelecter?.Initialize();
        }

        public void OnGui()
        {
            if (_freeHFile != null
                //&& !_isLive 
                && _targetScene == Scene.Instance.AddSceneName)
            {
                var screenRect = GetFullscreenBrowserRect();
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                Utils.EatInputInRect(screenRect);
            }
        }

        private static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
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
                        OnFolderChanged();

                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                    if (GUILayout.Button("Screenshot folder"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    if (GUILayout.Button("Main game folder"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}