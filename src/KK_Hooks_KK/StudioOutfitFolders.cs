using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.StudioOutfit)]
    public class StudioOutfitFolders : IFolderBrowser
    {
        private static CostumeInfoEntry _costumeInfoEntry;
        private static bool _refilterOnly;

        public StudioOutfitFolders()
        {
            //CostumeInfo is a private nested class            
            Harmony harmony = new Harmony(KK_BrowserFolders.Guid);

            var type = typeof(MPCharCtrl).GetNestedType("CostumeInfo", AccessTools.all);
            {
                var target = AccessTools.Method(type, "InitList");
                var prefix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitCostumeListPrefix));
                var postfix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitCostumeListPostfix));
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
            {
                var target = AccessTools.Method(type, "InitFileList");
                var prefix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitListPrefix));
                var postfix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitListPostfix));
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
        }

        public void OnGui()
        {
            if (_costumeInfoEntry != null)
            {
                if (_costumeInfoEntry.isActive())
                {
                    var windowRect = new Rect((int) (Screen.width * 0.06f), (int) (Screen.height * 0.32f),
                        (int) (Screen.width * 0.13f), (int) (Screen.height * 0.4f));
                    IMGUIUtils.DrawSolidBox(windowRect);
                    GUILayout.Window(363, windowRect, id => TreeWindow(), "Folder with outfits to view");
                    Utils.EatInputInRect(windowRect);
                }
                else
                {
                    _costumeInfoEntry.FolderTreeView?.StopMonitoringFiles();
                }
            }
        }

        internal static void InitCostumeListPostfix(object __instance, ref int ___sex, ref int __state)
        {
            ___sex = __state;
            if (_costumeInfoEntry != null)
                _costumeInfoEntry.RefilterInProgress = false;
            _refilterOnly = false;
        }

        internal static void InitCostumeListPrefix(object __instance, int _sex, ref int ___sex, ref int __state)
        {
            //If the CostumeInfo.sex field is equal to the parameter _sex, the method doesn't do anything
            __state = _sex;
            ___sex = 99;
            if (_costumeInfoEntry == null)
                _costumeInfoEntry = new CostumeInfoEntry(__instance);
            _refilterOnly = _costumeInfoEntry.RefilterInProgress;
        }

        private static void InitListPostfix()
        {
            if (_costumeInfoEntry != null)
            {
                _costumeInfoEntry.SaveFullList();
                _costumeInfoEntry.ApplyFilter();
            }
        }

        public static bool InitListPrefix()
        {
            // detect if force reload was triggered by a change of folder
            if (_refilterOnly && _costumeInfoEntry != null)
            {
                // if so just restore cached values so they can be re-filtered without
                // going back to disk and refilter them
                _costumeInfoEntry.RestoreUnfiltered();
                // stop real method from running, filter in postfix
                return false;
            }
            return true;
        }

        private static void TreeWindow()
        {
            GUILayout.BeginVertical();
            {
                _costumeInfoEntry.FolderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh outfits"))
                    {
                        _costumeInfoEntry.InitOutfitList();
                        _costumeInfoEntry.FolderTreeView.ResetTreeCache();
                        _costumeInfoEntry.FolderTreeView.CurrentFolderChanged.Invoke();
                    }
                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(_costumeInfoEntry.CurrentFolder);

                    if (GUILayout.Button("Screenshot folder"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));

                    if (GUILayout.Button("Main game folder"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private class CostumeInfoEntry
        {
            private readonly object _costumeInfo;
            public bool RefilterInProgress;
            private List<CharaFileInfo> _backupFileInfos;
            private string _currentFolder;
            private FolderTreeView _folderTreeView;
            private int _sex = -1;
            private readonly GameObject _clothesRoot;

            public CostumeInfoEntry(object costumeInfo)
            {
                _costumeInfo = costumeInfo;
                _clothesRoot = (GameObject)costumeInfo.GetType().GetField("objRoot", AccessTools.all).GetValue(costumeInfo);
            }

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? UserData.Path + "coordinate/");

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            Utils.NormalizePath(UserData.Path),
                            Path.Combine(Utils.NormalizePath(UserData.Path), "coordinate/"));
                        _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
                        _folderTreeView.CurrentFolderChanged = OnFolderChanged;
                        OnFolderChanged();
                    }
                    return _folderTreeView;
                }
            }

            public bool isActive() => _clothesRoot.activeInHierarchy;

            public void ApplyFilter()
            {
                GetCharaFileInfos().RemoveAll(cfi => Utils.NormalizePath(Path.GetDirectoryName(cfi.file)) != CurrentFolder);
            }

            public void InitOutfitList()
            {
                //_charaList.InitCharaList(force);                 
                Traverse.Create(_costumeInfo).Method("InitList", new object[] { GetSex() })?.GetValue();
            }

            public void RestoreUnfiltered()
            {
                if (_backupFileInfos != null)
                {
                    var fileInfos = GetCharaFileInfos();
                    fileInfos.Clear();
                    fileInfos.AddRange(_backupFileInfos);
                }
            }

            public void SaveFullList()
            {
                _backupFileInfos = GetCharaFileInfos().ToList();
            }
            private List<CharaFileInfo> GetCharaFileInfos()
            {
                return Traverse.Create(_costumeInfo)?.Field<CharaFileSort>("fileSort")?.Value?.cfiList;
            }
            private int GetSex()
            {
                if (_sex == -1)
                    _sex = Traverse.Create(_costumeInfo).Field<int>("sex").Value;
                return _sex;
            }
            private void OnFolderChanged()
            {
                _currentFolder = Utils.NormalizePath(FolderTreeView.CurrentFolder);
                RefilterInProgress = true;
                InitOutfitList();
            }
        }
    }
}
