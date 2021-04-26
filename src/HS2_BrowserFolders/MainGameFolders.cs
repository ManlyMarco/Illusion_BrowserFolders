using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GameLoadCharaFileSystem;
using HarmonyLib;
using HS2;
using KKAPI.Utilities;
using UnityEngine;

namespace BrowserFolders
{
    public class MainGameFolders : IFolderBrowser
    {
        private static CanvasGroup _charaLoadVisible;
        private static GameObject _bookCanvas;
        private static GroupCharaSelectUI _charaLoad;

        private static VisibleWindow _lastVisibleWindow;
        private static FolderTreeView _folderTreeView;

        public MainGameFolders()
        {
            var pathDefault = Path.Combine(Utils.NormalizePath(UserData.Path), "chara/female");
            _folderTreeView = new FolderTreeView(pathDefault, pathDefault);
            _folderTreeView.CurrentFolder = pathDefault;
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;

            Harmony.CreateAndPatchAll(typeof(MainGameFolders));
            //todo split out into a separate thing?
            Harmony.CreateAndPatchAll(typeof(NestedFilenamesMainGamePatch));
        }

        public void OnGui()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastVisibleWindow = VisibleWindow.None;
                _folderTreeView?.StopMonitoringFiles();
                return;
            }

            if (_lastVisibleWindow != visibleWindow) RefreshCurrentWindow();

            var screenRect = GetDisplayRect();
            IMGUIUtils.DrawSolidBox(screenRect);
            GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            IMGUIUtils.EatInputInRect(screenRect);
        }

        private static void RefreshCurrentWindow()
        {
            var visibleWindow = IsVisible();
            _lastVisibleWindow = visibleWindow;
            var resetTree = false;

            if (visibleWindow == VisibleWindow.Load)
            {
                if (_charaLoad != null)
                {
                    var tra = Traverse.Create(_charaLoad);
                    tra.Field<List<GameCharaFileInfo>>("charaLists").Value.Clear();

                    var list = new List<GameCharaFileInfo>();
                    var method = typeof(GameCharaFileInfoAssist).GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);
                    if (method == null) throw new ArgumentNullException(nameof(method));
                    method.Invoke(null, new object[] { list, _folderTreeView.CurrentFolder, 0, (byte)1, true, true, false, false, true, false });

                    tra.Field<List<GameCharaFileInfo>>("charaLists").Value = list;

                    _charaLoad.ReDrawListView();

                    resetTree = true;
                }
            }

            if (resetTree) _folderTreeView.ResetTreeCache();
        }

        internal static Rect GetDisplayRect()
        {
            const float x = 0.02f, y = 0.59f, w = 0.200f, h = 0.35f;
            return new Rect((int)(Screen.width * x), (int)(Screen.height * y), (int)(Screen.width * w), (int)(Screen.height * h));
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh thumbnails"))
                        RefreshCurrentWindow();

                    GUILayout.Space(1);

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

        private static VisibleWindow IsVisible()
        {
            if (_bookCanvas != null && _bookCanvas.activeSelf)
            {
                var isLoadVisible = _charaLoad != null && _charaLoadVisible.alpha > 0.99;
                if (isLoadVisible) return VisibleWindow.Load;
            }
            return VisibleWindow.None;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaEditUI), "Start")]
        private static void BookOpenHook(ref CharaEditUI __instance)
        {
            _bookCanvas = __instance.gameObject.transform.Find("Group")?.gameObject;
            _charaLoadVisible = __instance.gameObject.transform.Find("Group")?.GetComponent<CanvasGroup>();

            var tra = Traverse.Create(__instance);
            _charaLoad = tra.Field<GroupCharaSelectUI>("groupCharaSelectUI").Value;
        }

        public static string GetRelativePath(string fromFile, string toFolder)
        {
            var pathUri = new Uri(fromFile);

            if (!toFolder.EndsWith(Path.DirectorySeparatorChar.ToString())) toFolder += Path.DirectorySeparatorChar;
            var folderUri = new Uri(toFolder);

            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()).Remove(0, 3);
        }

        private enum VisibleWindow
        {
            None,
            Load
        }
    }
}