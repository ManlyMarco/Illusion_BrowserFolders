using AIChara;
using BepInEx;
using BepInEx.Configuration;
using GameLoadCharaFileSystem;
using HarmonyLib;
using HS2;
using KKAPI.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BrowserFolders
{

    using Debug = UnityEngine.Debug;
    [BepInProcess("HoneySelect2")]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {

        private IFolderBrowser _hs2MainGameFolders;

        public static ConfigEntry<bool> EnableMainGame { get; private set; }


    }

    public class HS2_MainGameFolders : IFolderBrowser
    {

        private static CanvasGroup _charaLoadVisible;
        private static GameObject _bookCanvas;
        private static GroupCharaSelectUI _charaLoad;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;
        public HS2_MainGameFolders()
        {
            string pathDefault = Path.Combine(Utils.NormalizePath(UserData.Path), "chara/female");
            _folderTreeView = new FolderTreeView(pathDefault, pathDefault);
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;
            Harmony.CreateAndPatchAll(typeof(HS2_MainGamePatch));
            Harmony.CreateAndPatchAll(typeof(HS2_MainGameFolders));
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
                        Traverse tra = Traverse.Create(_charaLoad);
                        tra.Field<List<GameCharaFileInfo>>("charaLists").Value.Clear();
                        List<GameCharaFileInfo> list = new List<GameCharaFileInfo>();
                        var method = typeof(GameCharaFileInfoAssist).GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);
                        byte b = 1;
                        method.Invoke(obj: null, parameters: new object[] { list, _folderTreeView.CurrentFolder, 0, b, true, true, false, false, true, false });
                        tra.Field<List<GameCharaFileInfo>>("charaLists").Value = list;
                        _charaLoad.ReDrawListView();

                        resetTree = true;
                    }
                    break;
            }

            // clear tree cache
            if (resetTree)
            {

                _folderTreeView.ResetTreeCache();

            }

        }

        internal static Rect GetDisplayRect()
        {
            const float x = 0.02f;
            const float y = 0.59f;
            const float w = 0.200f;
            const float h = 0.35f;

            return new Rect((int)(Screen.width * x), (int)(Screen.height * y),
                (int)(Screen.width * w), (int)(Screen.height * h));
        }

        public void OnGui()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastRefreshed = VisibleWindow.None;
                _folderTreeView?.StopMonitoringFiles();
                return;
            }

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

            if (_bookCanvas == null) return VisibleWindow.None;
            if (!_bookCanvas.activeSelf) return VisibleWindow.None;

            if (IsLoadVisible()) return VisibleWindow.Load;

            return VisibleWindow.None;

            bool IsLoadVisible()
            {

                return _charaLoad != null && (_charaLoadVisible.alpha == 1);
            }
        }
        private enum VisibleWindow
        {
            None,
            Load
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaEditUI), "Start")]
        private static void BookOpenHook(ref CharaEditUI __instance)
        {
            _bookCanvas = __instance.gameObject.transform.Find("Group")?.gameObject;
            _charaLoadVisible = __instance.gameObject.transform.Find("Group")?.GetComponent<CanvasGroup>();
            Traverse tra = Traverse.Create(__instance);
            _charaLoad = tra.Field<GroupCharaSelectUI>("groupCharaSelectUI").Value;
        }

        public static string GetRelativePath(string fromFile, string toFolder)
        {
            Uri pathUri = new Uri(fromFile);
            if (!toFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                toFolder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(toFolder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString()).Remove(0, 3);
        }

    }

    public class HS2_MainGamePatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Path), "GetFileName")]
        public static bool GetFileNamePatch(ref string __result, string path)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true; //Skip if not maingame
            }
            try
            {
                Uri basePathForChara = new Uri(UserData.Path + "chara/female/");
                Uri lookedAtPath = new Uri(path);
                if (basePathForChara.IsBaseOf(lookedAtPath))
                {
                    __result = HS2_MainGameFolders.GetRelativePath(path,UserData.Path + "chara/female/");
                    return false;
                }
                else if(File.Exists(UserData.Path + "chara/female/"+path))
                {
                    __result = path;
                    return false;
                }
                return true;

            }
            catch
            {
                return true;
            }


        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), "ConvertCharaFilePath")]
        public static bool ConvertCharaFilePath(ref ChaFileControl __instance, ref string __result, string path)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;//So you skip if not maingame
            }
            if (path == null)
            {
                return true;
            }
            else
            {
                if (!path.EndsWith(".png"))
                {
                    path += ".png";
                }
                if (File.Exists(path))
                {
                    __result = path;
                    return false;
                }
                else if (File.Exists(UserData.Path + "chara/female/" + path))
                {
                    __result = UserData.Path + "chara/female/" + path;
                    return false;
                }
                else
                {
                    return true;
                }
            }

        }

    }
}
