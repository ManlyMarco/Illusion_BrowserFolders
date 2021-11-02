using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders
{
    public class MakerFolders : IFolderBrowser
    {
        private static CvsO_CharaLoad _charaLoad;
        private static CanvasGroup[] _charaLoadVisible;
        private static CvsO_CharaSave _charaSave;
        private static CanvasGroup[] _charaSaveVisible;
        private static CvsO_Fusion _charaFusion;
        private static CanvasGroup[] _charaFusionVisible;
        private static GameObject _makerCanvas;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;

        private bool _guiActive;

        public MakerFolders()
        {
            _folderTreeView = new FolderTreeView(AI_BrowserFolders.UserDataPath, AI_BrowserFolders.UserDataPath)
            {
                CurrentFolderChanged = RefreshCurrentWindow
            };

            Harmony.CreateAndPatchAll(typeof(MakerFolders));
            MakerCardSave.RegisterNewCardSavePathModifier(CardSavePathModifier, null);
        }

        private static VisibleWindow IsVisible()
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
            var newFolder = _folderTreeView?.CurrentFolder;
            if (newFolder != null)
            {
                // Force reload
                _lastRefreshed = VisibleWindow.None;
                return newFolder;
            }

            return currentDirectoryPath;
        }

        private static string GetCurrentRelativeFolder(string defaultPath)
        {
            if (IsVisible() == VisibleWindow.None) return defaultPath;
            return _folderTreeView?.CurrentRelativeFolder ?? defaultPath;
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
            if (resetTree) _folderTreeView.ResetTreeCache();

        }

        internal static Rect GetDisplayRect()
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

        public void OnGui()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastRefreshed = VisibleWindow.None;
                if (!_guiActive)
                {
                    _folderTreeView?.StopMonitoringFiles();
                    _guiActive = false;
                }
                return;
            }

            _guiActive = true;
            if (_lastRefreshed != visibleWindow) RefreshCurrentWindow();

            var screenRect = GetDisplayRect();
            IMGUIUtils.DrawSolidBox(screenRect);
            GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
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
                    {
                        _folderTreeView?.ResetTreeCache();
                        RefreshCurrentWindow();
                    }

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

        private enum VisibleWindow
        {
            None,
            Load,
            Save,
            Fuse
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsO_CharaLoad), "Start")]
        internal static void InitHookLoad(CvsO_CharaLoad __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path),
                MakerAPI.GetMakerSex() == 0 ? "chara/male" : @"chara/female");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
            //_targetScene = GetAddSceneName();

            _makerCanvas = __instance.GetComponentInParent<Canvas>().gameObject;

            _charaLoad = __instance;
            _charaLoadVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsO_CharaSave), "Start")]
        internal static void InitHookSave(CvsO_CharaSave __instance)
        {
            _charaSave = __instance;
            _charaSaveVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
        }

#if AI
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CvsO_Fusion), "Start")]
        internal static void InitHookFuse(CvsO_Fusion __instance, Button ___btnFusion,
            CustomCharaWindow ___charaLoadWinA, CustomCharaWindow ___charaLoadWinB)
        {
            InitFusion(__instance, ___btnFusion, ___charaLoadWinA, ___charaLoadWinB);
        }
#elif HS2
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CvsO_Fusion), "Start")]
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
            var getFolderMethod = AccessTools.Method(typeof(MakerFolders), nameof(GetCurrentRelativeFolder)) ??
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