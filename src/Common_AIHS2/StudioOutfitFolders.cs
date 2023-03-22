using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders
{
    public class StudioOutfitFolders : IFolderBrowser
    {
        private static CostumeInfoEntry _costumeInfoEntry;
        private static bool _refilterOnly;

        public StudioOutfitFolders()
        {
            //CostumeInfo is a private nested class            
            Harmony harmony = new Harmony(nameof(StudioOutfitFolders));

            var type = typeof(MPCharCtrl.CostumeInfo);
            {
                var target = AccessTools.Method(type, nameof(MPCharCtrl.CostumeInfo.InitList));
                var prefix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitCostumeListPrefix));
                var postfix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitCostumeListPostfix));
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
            {
                var target = AccessTools.Method(type, nameof(MPCharCtrl.CostumeInfo.InitFileList));
                var prefix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitListPrefix));
                var postfix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitListPostfix));
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
        }

        private bool _guiActive;
        private static Rect _windowRect;

        public void OnGui()
        {
            if (_costumeInfoEntry != null)
            {
                if (_costumeInfoEntry.isActive())
                {
                    _guiActive = true;
                    InterfaceUtils.DisplayFolderWindow(_costumeInfoEntry.FolderTreeView, () => _windowRect, r => _windowRect = r, "Folder with outfits to view", () =>
                    {
                        _costumeInfoEntry.InitOutfitList();
                        _costumeInfoEntry.FolderTreeView.CurrentFolderChanged.Invoke();
                    });
                }
                else if (_guiActive)
                {
                    _costumeInfoEntry.FolderTreeView?.StopMonitoringFiles();
                    _guiActive = false;
                }
            }
        }

        internal static void InitCostumeListPostfix(MPCharCtrl.CostumeInfo __instance, ref int ___sex, ref int __state)
        {
            ___sex = __state;
            if (_costumeInfoEntry != null)
                _costumeInfoEntry.RefilterInProgress = false;
            _refilterOnly = false;
        }

        internal static void InitCostumeListPrefix(MPCharCtrl.CostumeInfo __instance, int _sex, ref int ___sex, ref int __state)
        {
            //If the CostumeInfo.sex field is equal to the parameter _sex, the method doesn't do anything
            __state = _sex;
            ___sex = 99;
            if (_costumeInfoEntry == null || _costumeInfoEntry.GetSex() != _sex)
                _costumeInfoEntry = new CostumeInfoEntry(__instance);
            _refilterOnly = _costumeInfoEntry.RefilterInProgress;

            _windowRect = new Rect((int) (Screen.width * 0.06f), (int) (Screen.height * 0.32f),
                                   (int) (Screen.width * 0.13f), (int) (Screen.height * 0.4f));
        }

        private static void InitListPostfix()
        {
            if (_costumeInfoEntry != null)
            {
                if (!_refilterOnly)
                {
                    // don't update results if we didn't get new ones
                    _costumeInfoEntry.SaveFullList();
                }

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

        private class CostumeInfoEntry
        {
            private readonly MPCharCtrl.CostumeInfo _costumeInfo;
            public bool RefilterInProgress;
            private List<CharaFileInfo> _backupFileInfos;
            private string _currentFolder;
            private FolderTreeView _folderTreeView;
            private int _sex = -1;
            private readonly GameObject _clothesRoot;

            public CostumeInfoEntry(MPCharCtrl.CostumeInfo costumeInfo)
            {
                _costumeInfo = costumeInfo;
                _clothesRoot = (GameObject)costumeInfo.GetType().GetField("objRoot", AccessTools.all).GetValue(costumeInfo);
            }

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? UserData.Path);

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            Utils.NormalizePath(UserData.Path),
                            Path.Combine(Utils.NormalizePath(UserData.Path), GetSex() != 0 ? "coordinate/female" : "coordinate/male"));
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
                var currentFolder = CurrentFolder;
                GetCharaFileInfos().RemoveAll(cfi => Utils.GetNormalizedDirectoryName(cfi.file) != currentFolder);
            }

            public void InitOutfitList()
            {
                _costumeInfo.InitList(GetSex());
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
                return _costumeInfo?.fileSort?.cfiList;
            }
            public int GetSex()
            {
                if (_sex == -1)
                    _sex = _costumeInfo.sex;
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