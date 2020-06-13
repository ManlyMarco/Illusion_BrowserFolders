using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using BepInEx.Harmony;
using CharaCustom;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine;

namespace BrowserFolders
{
    public class MakerFolders : IFolderBrowser
    {
        private static readonly float _x = 0.623f;
        private static readonly float _y = 0.17f;
        private static readonly float _w = 0.125f;
        private static readonly float _h = 0.4f;

        //private static string _targetScene;

        private static CvsO_CharaLoad _charaLoad;
        private static CanvasGroup[] _charaLoadVisible;
        private static CvsO_CharaSave _charaSave;
        private static CanvasGroup[] _charaSaveVisible;
        private static CvsO_Fusion _charaFusion;
        private static CanvasGroup[] _charaFusionVisible;
        private static GameObject _makerCanvas;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;

        public MakerFolders()
        {
            _folderTreeView = new FolderTreeView(AI_BrowserFolders.UserDataPath, AI_BrowserFolders.UserDataPath);
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;

            HarmonyWrapper.PatchAll(typeof(MakerFolders));
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

            switch (visibleWindow)
            {
                case VisibleWindow.Load:
                    if (_charaLoad != null) _charaLoad.UpdateCharasList();
                    break;
                case VisibleWindow.Save:
                    if (_charaSave != null) _charaSave.UpdateCharasList();
                    break;
                case VisibleWindow.Fuse:
                    if (_charaFusion != null) _charaFusion.UpdateCharasList();
                    break;
            }
        }

        public void OnGui()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None
                //|| _targetScene != GetAddSceneName()
            ) return;

            // Hacky way to detect if a dialog/config screen is shown
            //if (Time.timeScale == 0) return;
            // Check if the character picture take screen is displayed
            //if (_saveFront == null || !_saveFront.activeSelf)
            {
                if (_lastRefreshed != visibleWindow) RefreshCurrentWindow();

                var screenRect = new Rect((int) (Screen.width * _x), (int) (Screen.height * _y),
                    (int) (Screen.width * _w), (int) (Screen.height * _h));
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            }
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

//        private static string GetAddSceneName()
//        {
//#if HS2
//            return Scene.AddSceneName;
//#elif AI
//            return Scene.Instance.AddSceneName;
//#endif
//        }

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsO_Fusion), "Start")]
        internal static void InitHookFuse(CvsO_Fusion __instance)
        {
            _charaFusion = __instance;
            _charaFusionVisible = __instance.GetComponentsInParent<CanvasGroup>(true);
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