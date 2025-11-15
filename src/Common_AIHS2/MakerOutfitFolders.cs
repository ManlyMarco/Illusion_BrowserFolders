using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using AIChara;
using BepInEx.Configuration;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;

namespace BrowserFolders
{
    public class MakerOutfitFolders : BaseFolderBrowser
    {
        private static CvsC_ClothesLoad _charaLoad;
        private static CanvasGroup[] _charaLoadVisible;
        private static CvsC_ClothesSave _charaSave;
        private static CanvasGroup[] _charaSaveVisible;
        private static GameObject _makerCanvas;

        private static MakerOutfitFolders _instance;

        private static VisibleWindow _currentlyVisible;

        public MakerOutfitFolders() : base("Clothes folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable clothes folder browser in maker", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _instance = this;

            harmony.PatchAll(typeof(Hooks));
            return true;
        }

        protected override int IsVisible()
        {
            _currentlyVisible = WhatIsVisible();
            return (int)_currentlyVisible;
        }

        public override void OnListRefresh()
        {
            var visibleWindow = WhatIsVisible();
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
            if (resetTree) TreeView.ResetTreeCache();
        }

        public override Rect GetDefaultRect()
        {
            return MakerFolders.GetDefaultDisplayRect();
        }

        private static VisibleWindow WhatIsVisible()
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
            if (_currentlyVisible != VisibleWindow.None)
            {
                var overrideFolder = _instance?.TreeView.CurrentRelativeFolder;
                if (overrideFolder != null) return overrideFolder + '/';
            }
            return defaultPath;
        }

        private enum VisibleWindow
        {
            None = 0,
            Load,
            Save,
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile))]
            internal static void SaveFilePatch(ref string path)
            {
                if (_makerCanvas == null) return;
                var newFolder = _instance?.TreeView.CurrentFolder;
                if (newFolder == null) return;

                var name = Path.GetFileName(path);
                path = Path.Combine(newFolder, name);

                // Force reload
                _instance?.OnListRefresh();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsC_ClothesLoad), nameof(CvsC_ClothesLoad.Start))]
            internal static void InitHookLoad(CvsC_ClothesLoad __instance)
            {
                var treeView = _instance?.TreeView;
                if (treeView == null) return;
                treeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), MakerAPI.GetMakerSex() == 0 ? "coordinate/male" : "coordinate/female");
                treeView.CurrentFolder = treeView.DefaultPath;
                //_targetScene = GetAddSceneName();

                _makerCanvas = __instance.GetComponentInParent<Canvas>().gameObject;

                _charaLoad = __instance;
                _charaLoadVisible = __instance.GetComponentsInParent<CanvasGroup>(true);

                //_windowRect = MakerFolders.GetDefaultDisplayRect();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsC_ClothesSave), nameof(CvsC_ClothesSave.Start))]
            internal static void InitHookSave(CvsC_ClothesSave __instance)
            {
                _charaSave = __instance;
                _charaSaveVisible = __instance.GetComponentsInParent<CanvasGroup>(true);

                //_windowRect = MakerFolders.GetDefaultDisplayRect();
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomClothesFileInfoAssist), nameof(CustomClothesFileInfoAssist.CreateClothesFileInfoList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                var getFolderMethod = AccessTools.Method(typeof(MakerOutfitFolders), nameof(MakerOutfitFolders.GetCurrentRelativeFolder)) ??
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
}