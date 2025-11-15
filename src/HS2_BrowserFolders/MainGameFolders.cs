using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using GameLoadCharaFileSystem;
using HarmonyLib;
using HS2;
using KKAPI.Maker;
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

        private bool _guiActive;

        public Rect WindowRect { get; set; }
        public FolderTreeView TreeView => _folderTreeView;
        public string Title => "Character folder";

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable character folder browser in main game", true, "NOTE: This will patch the game to allow nested folder paths in the save file. If you turn this feature off or remove this plugin, any cards that are in subfolders will be removed from your game save (the cards themselves will not be affected, you will just have to readd them to your groups). Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            var pathDefault = Path.Combine(Utils.NormalizePath(UserData.Path), "chara/female");
            _folderTreeView = new FolderTreeView(pathDefault, pathDefault)
            {
                CurrentFolder = pathDefault,
                CurrentFolderChanged = OnListRefresh
            };

            //todo split out into a separate thing?
            harmony.PatchAll(typeof(Hooks));
            harmony.PatchAll(typeof(NestedFilenamesMainGamePatch));
            return true;
        }

        public void Update()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastVisibleWindow = VisibleWindow.None;
                if (_guiActive) _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
                return;
            }

            _guiActive = true;
            if (_lastVisibleWindow != visibleWindow) OnListRefresh();
        }

        public void OnGui()
        {
            if (!_guiActive) return;

            BaseFolderBrowser.DisplayFolderWindow(this);
        }

        public Rect GetDefaultRect()
        {
            const float x = 0.02f, y = 0.59f, w = 0.200f, h = 0.35f;
            return new Rect((int)(Screen.width * x), (int)(Screen.height * y), (int)(Screen.width * w), (int)(Screen.height * h));
        }

        public void OnListRefresh()
        {
            var visibleWindow = IsVisible();
            _lastVisibleWindow = visibleWindow;
            var resetTree = false;

            if (visibleWindow == VisibleWindow.Load)
            {
                if (_charaLoad != null)
                {
                    _charaLoad.charaLists.Clear();
                    // can we pass _charaLoad.charaLists directly rather than allocating new list and assigning?
                    var list = new List<GameCharaFileInfo>();
                    GameCharaFileInfoAssist.AddList(list, _folderTreeView.CurrentFolder, 0, 1, true, true, false, false, true, false);
                    _charaLoad.charaLists = list;

                    _charaLoad.ReDrawListView();

                    resetTree = true;
                }
            }

            if (resetTree) _folderTreeView.ResetTreeCache();
        }

        private static VisibleWindow IsVisible()
        {
            if (!MakerAPI.InsideMaker && _bookCanvas != null && _bookCanvas.activeSelf)
            {
                var isLoadVisible = _charaLoad != null && _charaLoadVisible.alpha > 0.99;
                if (isLoadVisible && !Manager.Scene.IsFadeNow) return VisibleWindow.Load;
            }
            return VisibleWindow.None;
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

        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CharaEditUI), nameof(CharaEditUI.Start))]
            private static void BookOpenHook(ref CharaEditUI __instance)
            {
                _bookCanvas = __instance.gameObject.transform.Find("Group")?.gameObject;
                _charaLoadVisible = __instance.gameObject.transform.Find("Group")?.GetComponent<CanvasGroup>();

                _charaLoad = __instance.groupCharaSelectUI;
            }
        }
    }
}