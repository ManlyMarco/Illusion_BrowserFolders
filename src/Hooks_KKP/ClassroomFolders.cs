using System.Diagnostics;
using System.IO;
using ActionGame;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    public class ClassroomFolders : IFolderBrowser
    {
        public BrowserType Type => BrowserType.Classroom;

        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static string _targetScene;
        private static PreviewCharaList _customCharaFile;

        public ClassroomFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Overlord.Init();
        }

        public static void Init(PreviewCharaList list, int sex)
        {
            if (_customCharaFile != list)
            {
                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCharaFile = list;
                _targetScene = Scene.Instance.AddSceneName;
            }
        }

        public void OnGui()
        {
            if (_customCharaFile != null && _customCharaFile.isVisible && _targetScene == Scene.Instance.AddSceneName)
            {
                var screenRect = GetFullscreenBrowserRect();
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            }
        }

        private static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }

        private static void OnFolderChanged()
        {
            _customCharaFile?.CharFile?.Initialize();
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if(Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();

                    if (GUILayout.Button("Refresh thumbnails"))
                        OnFolderChanged();

                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                    if (GUILayout.Button("Screenshot folder"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.NormalizePath(UserData.Path), "cap")}\"");
                    if (GUILayout.Button("Main game folder"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.NormalizePath(UserData.Path))}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}
