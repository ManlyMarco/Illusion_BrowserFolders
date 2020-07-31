using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using AIChara;
using BepInEx.Harmony;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;

namespace BrowserFolders
{
    public class MakerOutfitFolders : IFolderBrowser
    {
        private static CvsC_ClothesLoad _charaLoad;
        private static CanvasGroup[] _charaLoadVisible;
        private static CvsC_ClothesSave _charaSave;
        private static CanvasGroup[] _charaSaveVisible;
        private static GameObject _makerCanvas;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;

        public MakerOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(AI_BrowserFolders.UserDataPath, AI_BrowserFolders.UserDataPath);
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;

            HarmonyWrapper.PatchAll(typeof(MakerOutfitFolders));
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch(ref string path)
        {
            try
            {
                if (_makerCanvas == null) return;
                var newFolder = _folderTreeView?.CurrentFolder;
                if (newFolder == null) return;

                var name = Path.GetFileName(path);
                path = Path.Combine(newFolder, name);

                // Force reload
                _lastRefreshed = VisibleWindow.None;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
            }
        }

        private static VisibleWindow IsVisible()
        {
            if (_makerCanvas == null) return VisibleWindow.None;
            if (!_makerCanvas.activeSelf) return VisibleWindow.None;
            if (IsLoadVisible()) return VisibleWindow.Load;
            if (IsSaveVisible()) return VisibleWindow.Save;
            return VisibleWindow.None;

            bool IsSaveVisible()
            {
                return _charaSave != null && _charaSaveVisible.All(x => x.interactable);
            }

            bool IsLoadVisible()
            {
                return _charaLoad != null && _charaLoadVisible.All(x => x.interactable);
            }
        }

        private static string GetCurrentRelativeFolder(string defaultPath)
        {
            if (IsVisible() != VisibleWindow.None)
            {
                var overrideFolder = _folderTreeView?.CurrentRelativeFolder;
                if (overrideFolder != null) return overrideFolder + '/';
            }
            return defaultPath;
        }

        private static void RefreshCurrentWindow()
        {
            var visibleWindow = IsVisible();
            _lastRefreshed = visibleWindow;
            var resetTree = false;

            switch (visibleWindow)
            {
                case VisibleWindow.Load:
                    if (_charaLoad != null)
                    {
                        _charaLoad.UpdateClothesList();
                        resetTree = true;
                    }
                    break;
                case VisibleWindow.Save:
                    if (_charaSave != null)
                    {
                        _charaSave.UpdateClothesList();
                        resetTree = true;
                    }
                    break;
            }

            // clear tree cache
            if (resetTree) _folderTreeView.ResetTreeCache();
        }

        public void OnGui()
        {//todo  When loading a coordinate it resets to the main folder without deselect in menu
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastRefreshed = VisibleWindow.None;
                return;
            }

            if (_lastRefreshed != visibleWindow) RefreshCurrentWindow();

            var screenRect = MakerFolders.GetDisplayRect();
            IMGUIUtils.DrawSolidBox(screenRect);
            GUILayout.Window(362, screenRect, TreeWindow, "Select clothes folder");
            IMGUIUtils.EatInputInRect(screenRect);
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

        private enum VisibleWindow
        {
            None,
            Load,
            Save,
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsC_ClothesLoad), "Start")]
        internal static void InitHookLoad(CvsC_ClothesLoad __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path),
                MakerAPI.GetMakerSex() == 0 ? "coordinate/male" : @"coordinate/female");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
            //_targetScene = GetAddSceneName();

            _makerCanvas = __instance.GetComponentInParent<Canvas>().gameObject;

            _charaLoad = __instance;
            _charaLoadVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsC_ClothesSave), "Start")]
        internal static void InitHookSave(CvsC_ClothesSave __instance)
        {
            _charaSave = __instance;
            _charaSaveVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            var getFolderMethod = AccessTools.Method(typeof(MakerOutfitFolders), nameof(GetCurrentRelativeFolder)) ??
                                  throw new MissingMethodException("could not find GetCurrentRelativeFolder");

            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (string.Equals(instruction.operand as string, "coordinate/male/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(instruction.operand as string, "coordinate/female/", StringComparison.OrdinalIgnoreCase))
                    // Will eat the string that just got pushed and produce a replacement
                    yield return new CodeInstruction(OpCodes.Call, getFolderMethod);
            }
        }
    }
}