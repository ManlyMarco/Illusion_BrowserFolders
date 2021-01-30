using System.IO;
using BepInEx.Harmony;
using ChaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.HOutfit)]
    public class HOutfitFolders : IFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        public HOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Harmony.CreateAndPatchAll(typeof(HOutfitFolders));
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(clothesFileControl), "Start")]
        public static void InitHook(clothesFileControl __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;
        }

        public void OnGui()
        {
            bool guiShown = false;
            //
            if (GameObject.Find("Canvas/clothesFileWindow").activeSelf)
            {
                var screenRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(36, screenRect, TreeWindow, "Select outfit folder");
                IMGUIUtils.EatInputInRect(screenRect);
                guiShown = true;

            }
            if (!guiShown) _folderTreeView?.StopMonitoringFiles();
        }

        private static void OnFolderChanged()
        {
            if (_customCoordinateFile == null) return;

            // private bool Initialize()                
            Traverse.Create(_customCoordinateFile).Method("Initialize").GetValue();
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
