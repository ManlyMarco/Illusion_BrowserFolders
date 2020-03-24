using System.IO;
using System.Linq;
using ActionGame;
using BepInEx.Harmony;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.Classroom)]
    public class ClassroomFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static string _targetScene;
        private static PreviewCharaList _customCharaFile;

        public ClassroomFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Overlord.Init();

            HarmonyWrapper.PatchAll(typeof(ClassroomFolders));
        }

        /// <summary>
        /// Make it possible to fill in class with random characters from all subfolders
        /// ChaFileControl[] GetRandomUserDataFemaleCard(int num)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Localize.Translate.Manager), nameof(Localize.Translate.Manager.GetRandomUserDataFemaleCard), typeof(int))]
        internal static bool RandomCharaPickOverride(int num, ref ChaFileControl[] __result)
        {
            if (KK_BrowserFolders.RandomCharaSubfolders?.Value != true) return true;

            var path = Path.Combine(UserData.Path, "chara/female");
            if (!Directory.Exists(path))
            {
                __result = new ChaFileControl[0];
                return false;
            }

            // Grab from all subdirs
            var results = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
            // Try to load cards until enough load successfully
            __result = results.Shuffle().Attempt(f =>
            {
                var chaFileControl = new ChaFileControl();
                if (chaFileControl.LoadCharaFile(f, 1, true, true))
                {
                    if (chaFileControl.parameter.sex != 0)
                        return chaFileControl;
                }
                return null;
            }).Where(x => x != null).Take(num).ToArray();
            return false;
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
