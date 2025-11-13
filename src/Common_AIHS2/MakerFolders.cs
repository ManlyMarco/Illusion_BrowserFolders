using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Configuration;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders
{
    public class MakerFolders : BaseFolderBrowser
    {
        private static CvsO_CharaLoad _charaLoad;
        private static CanvasGroup[] _charaLoadVisible;
        private static CvsO_CharaSave _charaSave;
        private static CanvasGroup[] _charaSaveVisible;
        private static CvsO_Fusion _charaFusion;
        private static CanvasGroup[] _charaFusionVisible;
        private static GameObject _makerCanvas;

        private static MakerFolders _instance;

        private static VisibleWindow _currentlyVisible;

        public MakerFolders() : base("Character folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable character folder browser in maker", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _instance = this;

            harmony.PatchAll(typeof(Hooks));
            MakerCardSave.RegisterNewCardSavePathModifier(CardSavePathModifier, null);
            //todo? MakerAPI.MakerFinishedLoading += (sender, args) => _windowRect = GetDefaultDisplayRect();

            return true;
        }

        protected override int IsVisible()
        {
            _currentlyVisible = WhatIsVisible();
            return (int)_currentlyVisible;
        }

        protected override void OnListRefresh()
        {
            var visibleWindow = WhatIsVisible();
            var resetTree = false;
            switch (visibleWindow)
            {
                case VisibleWindow.Load:
                    if (_charaLoad != null)
                    {
                        _charaLoad.UpdateCharasList();
                        resetTree = true;
                    }
                    break;
                case VisibleWindow.Save:
                    if (_charaSave != null)
                    {
                        _charaSave.UpdateCharasList();
                        resetTree = true;
                    }
                    break;
                case VisibleWindow.Fuse:
                    if (_charaFusion != null)
                    {
                        _charaFusion.UpdateCharasList();
                        resetTree = true;
                    }
                    break;
            }

            // clear tree cache
            if (resetTree) TreeView.ResetTreeCache();

        }

        public override Rect GetDefaultRect()
        {
            return GetDefaultDisplayRect();
        }

        private static VisibleWindow WhatIsVisible()
        {
            if (_makerCanvas == null) return VisibleWindow.None;
            if (IsFusionVisible()) return VisibleWindow.Fuse;
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

            bool IsFusionVisible()
            {
                return _charaFusion != null && _charaFusionVisible.All(x => x.interactable);
            }
        }

        private static string CardSavePathModifier(string currentDirectoryPath)
        {
            if (_makerCanvas == null) return currentDirectoryPath;
            var newFolder = _instance?.TreeView.CurrentFolder;
            if (newFolder != null)
            {
                // Force reload
                _instance?.OnListRefresh();
                return newFolder;
            }

            return currentDirectoryPath;
        }

        private static string GetCurrentRelativeFolder(string defaultPath)
        {
            if (_currentlyVisible == VisibleWindow.None) return defaultPath;
            return _instance?.TreeView.CurrentRelativeFolder ?? defaultPath;
        }

        internal static Rect GetDefaultDisplayRect()
        {
#if HS2
            const float x = 0.623f;
#elif AI
            const float x = 0.607f;
#endif
            const float y = 0.17f;
            const float w = 0.125f;
            const float h = 0.4f;

            return new Rect((int)(Screen.width * x), (int)(Screen.height * y),
                (int)(Screen.width * w), (int)(Screen.height * h));
        }

        private enum VisibleWindow
        {
            None = 0,
            Load,
            Save,
            Fuse
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsO_CharaLoad), nameof(CvsO_CharaLoad.Start))]
            internal static void InitHookLoad(CvsO_CharaLoad __instance)
            {
                var treeView = _instance?.TreeView;
                if (treeView == null) return;
                treeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), MakerAPI.GetMakerSex() == 0 ? "chara/male" : "chara/female");
                treeView.CurrentFolder = treeView.DefaultPath;
                //_targetScene = GetAddSceneName();

                _makerCanvas = __instance.GetComponentInParent<Canvas>().gameObject;

                _charaLoad = __instance;
                _charaLoadVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CvsO_CharaSave), nameof(CvsO_CharaSave.Start))]
            internal static void InitHookSave(CvsO_CharaSave __instance)
            {
                _charaSave = __instance;
                _charaSaveVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
            }

#if AI
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CvsO_Fusion), nameof(CvsO_Fusion.Start))]
        internal static void InitHookFuse(CvsO_Fusion __instance, Button ___btnFusion,
            CustomCharaWindow ___charaLoadWinA, CustomCharaWindow ___charaLoadWinB)
        {
            InitFusion(__instance, ___btnFusion, ___charaLoadWinA, ___charaLoadWinB);
        }
#elif HS2
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CvsO_Fusion), nameof(CvsO_Fusion.Start))]
            internal static void InitHookFuse(CvsO_Fusion __instance, Button ___btnFusion,
                CustomCharaWindow ___charaLoadWinA, CustomCharaWindow ___charaLoadWinB, ref IEnumerator __result)
            {
                __result = __result.AppendCo(() => InitFusion(__instance, ___btnFusion, ___charaLoadWinA, ___charaLoadWinB));
            }
#endif
            private static void InitFusion(CvsO_Fusion __instance, Button ___btnFusion,
                CustomCharaWindow ___charaLoadWinA, CustomCharaWindow ___charaLoadWinB)
            {
                _charaFusion = __instance;
                _charaFusionVisible = __instance.GetComponentsInParent<CanvasGroup>(true);

                // Fix fusion button not working when cards from different folers are used
                ___btnFusion.onClick.RemoveAllListeners();
                ___btnFusion.onClick.AddListener(() =>
                {
                    var info = ___charaLoadWinA.GetSelectInfo();
                    var info2 = ___charaLoadWinB.GetSelectInfo();
                    __instance.FusionProc(info.info.FullPath, info2.info.FullPath);
                    __instance.isFusion = true;
                });
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomCharaFileInfoAssist), nameof(CustomCharaFileInfoAssist.CreateCharaFileInfoList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                var getFolderMethod = AccessTools.Method(typeof(MakerFolders), nameof(MakerFolders.GetCurrentRelativeFolder)) ??
                                      throw new MissingMethodException("could not find GetCurrentRelativeFolder");

                foreach (var instruction in instructions)
                {
                    yield return instruction;

                    if (string.Equals(instruction.operand as string, "chara/female/", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(instruction.operand as string, "chara/male/", StringComparison.OrdinalIgnoreCase))
                        // Will eat the string that just got pushed and produce a replacement
                        yield return new CodeInstruction(OpCodes.Call, getFolderMethod);
                }
            }
        }
    }
}