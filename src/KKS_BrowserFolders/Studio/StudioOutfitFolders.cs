using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders.Studio
{
    public class StudioOutfitFolders : IFolderBrowser
    {
        private static CostumeInfoEntry _costumeInfoEntry;
        private static bool _refilterOnly;

        private bool _guiActive;
        public Rect WindowRect { get; set; }
        public FolderTreeView TreeView => _costumeInfoEntry?.FolderTreeView;
        public string Title => "Outfit folder";

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Chara Studio", "Enable folder browser in outfit browser", true, "Changes take effect on game restart");

            if (!isStudio || !enable.Value) return false;

            Hooks.Apply(harmony);

            return true;
        }
        public void Update()
        {
            var visible = _costumeInfoEntry != null && _costumeInfoEntry.isActive();
            if (!visible)
            {
                if (_guiActive)
                    _costumeInfoEntry?.FolderTreeView.StopMonitoringFiles();
            }
            _guiActive = visible;
        }

        public void OnGui()
        {
            if (!_guiActive) return;

            var entry = _costumeInfoEntry;
            BaseFolderBrowser.DisplayFolderWindow(this, drawAdditionalButtons: () =>
            {
                if (BrowserFoldersPlugin.DrawDefaultCardsToggle())
                    entry.InitOutfitList();
            });
        }

        public void OnListRefresh()
        {
            if (_costumeInfoEntry != null)
            {
                _costumeInfoEntry.InitOutfitList();
                _costumeInfoEntry.FolderTreeView.CurrentFolderChanged.Invoke();
            }
        }

        public Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.06f), (int)(Screen.height * 0.32f),
                            (int)(Screen.width * 0.13f), (int)(Screen.height * 0.4f));
        }

        private static class Hooks
        {
            public static void Apply(Harmony harmony)
            {
                //todo this could just be attributes
                var type = typeof(MPCharCtrl.CostumeInfo);
                {
                    var target = AccessTools.Method(type, nameof(MPCharCtrl.CostumeInfo.InitList));
                    var prefix = AccessTools.Method(typeof(Hooks), nameof(InitCostumeListPrefix));
                    var postfix = AccessTools.Method(typeof(Hooks), nameof(InitCostumeListPostfix));
                    harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                }
                {
                    var target = AccessTools.Method(type, nameof(MPCharCtrl.CostumeInfo.InitFileList));
                    var prefix = AccessTools.Method(typeof(Hooks), nameof(InitListPrefix));
                    var postfix = AccessTools.Method(typeof(Hooks), nameof(InitListPostfix));
                    harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                }
            }

            internal static void InitCostumeListPostfix(MPCharCtrl.CostumeInfo __instance, ref int ___sex, ref int __state)
            {
                ___sex = __state;
                if (_costumeInfoEntry != null)
                {
                    _costumeInfoEntry.RefilterInProgress = false;
                    StudioFileHelper.SetGetAllFilesOverride(_costumeInfoEntry.CurrentFolder, "*.png", null);
                }

                _refilterOnly = false;
            }

            internal static void InitCostumeListPrefix(MPCharCtrl.CostumeInfo __instance, int _sex, ref int ___sex, ref int __state)
            {
                //If the CostumeInfo.sex field is equal to the parameter _sex, the method doesn't do anything
                __state = _sex;
                ___sex = 99;
                if (_costumeInfoEntry == null)
                    _costumeInfoEntry = new CostumeInfoEntry(__instance);
                _refilterOnly = _costumeInfoEntry.RefilterInProgress;

                // This is such a mess
                StudioFileHelper.SetGetAllFilesOverride(_costumeInfoEntry.CurrentFolder, "*.png", _costumeInfoEntry.CurrentFolder);
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
        }

        private class CostumeInfoEntry
        {
            private readonly MPCharCtrl.CostumeInfo _costumeInfo;
            public bool RefilterInProgress;
            private List<CharaFileInfo> _backupFileInfos;
            private string _currentFolder;
            private string _currentDefaultDataFolder;
            private FolderTreeView _folderTreeView;
            private int _sex = -1;
            private readonly GameObject _clothesRoot;

            public CostumeInfoEntry(MPCharCtrl.CostumeInfo costumeInfo)
            {
                _costumeInfo = costumeInfo;
                _clothesRoot = costumeInfo.objRoot;
            }

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? UserData.Path + "coordinate/");

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            BrowserFoldersPlugin.UserDataPath,
                            Path.Combine(BrowserFoldersPlugin.UserDataPath, "coordinate/"));
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
                var currentDefaultFolder = BrowserFoldersPlugin.ShowDefaultCharas.Value ? _currentDefaultDataFolder : null;
                GetCharaFileInfos().RemoveAll(cfi =>
                {
                    var directoryName = Utils.GetNormalizedDirectoryName(cfi.file);
                    return directoryName != currentFolder && directoryName != currentDefaultFolder;
                });
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
            private int GetSex()
            {
                if (_sex == -1)
                    _sex = _costumeInfo.sex;
                return _sex;
            }
            private void OnFolderChanged()
            {
                _currentFolder = Utils.NormalizePath(FolderTreeView.CurrentFolder);

                var normalizedUserData = BrowserFoldersPlugin.UserDataPath;
                _currentDefaultDataFolder = Utils.NormalizePath(normalizedUserData + "/../DefaultData/" + _currentFolder.Remove(0, normalizedUserData.Length));
                Debug.Log(_currentDefaultDataFolder);

                //RefilterInProgress = true;
                InitOutfitList();
            }
        }
    }
}
