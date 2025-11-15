using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders
{
    public class StudioCharaFolders : IFolderBrowser
    {
        private static readonly Dictionary<string, CharaListEntry> _CharaListEntries = new Dictionary<string, CharaListEntry>();
        private static bool _refilterOnly;
        private static CharaListEntry _lastEntry;

        public Rect WindowRect { get; set; }

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Chara Studio", "Enable folder browser in character browser", true, "Changes take effect on game restart");

            if (!isStudio || !enable.Value) return false;

            harmony.PatchAll(typeof(StudioCharaFolders));
            return true;
        }

        public void Update()
        {
            var entry = _CharaListEntries.Values.SingleOrDefault(x => x.ListIsActive);
            if (_lastEntry != null && _lastEntry != entry)
            {
                _lastEntry.FolderTreeView?.StopMonitoringFiles();

                _lastEntry = null;
            }
            else
                _lastEntry = entry;
        }

        public void OnGui()
        {
            var entry = _lastEntry;
            if (entry == null) return;

            InterfaceUtils.DisplayFolderWindow(entry.FolderTreeView, () => WindowRect, r => WindowRect = r, "Character folder", () =>
            {
                entry.InitCharaList(true);
                entry.FolderTreeView.CurrentFolderChanged.Invoke();
            }, GetDefaultRect);
        }

        public Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.06f), (int)(Screen.height * 0.32f), (int)(Screen.width * 0.13f), (int)(Screen.height * 0.4f));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitCharaList))]
        internal static void InitCharaListPostfix(CharaList __instance)
        {
            if (_CharaListEntries.TryGetValue(__instance.name, out var entry))
                entry.RefilterInProgress = false;
            _refilterOnly = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitCharaList))]
        internal static void InitCharaListPrefix(CharaList __instance, bool _force)
        {
            if (!_CharaListEntries.ContainsKey(__instance.name))
                _CharaListEntries[__instance.name] = new CharaListEntry(__instance);
            _refilterOnly = _force && _CharaListEntries[__instance.name].RefilterInProgress;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
        internal static void InitFemaleLisPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
        internal static bool InitFemaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
        internal static void InitMaleListPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
        internal static bool InitMaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        private static void InitListPostfix(string name)
        {
            if (_CharaListEntries.TryGetValue(name, out var entry))
            {
                if (!_refilterOnly)
                {
                    // don't update results if we didn't get new ones
                    entry.SaveFullList();
                }
                // list must be filtered before the rest of InitCharaList runs
                entry.ApplyFilter();
            }
        }

        private static bool InitListPrefix(string name)
        {
            if (_CharaListEntries.TryGetValue(name, out var entry))
            {
                // detect if force reload was triggered by a change of folder
                if (_refilterOnly)
                {
                    // if so just restore cached values so they can be re-filtered without
                    // going back to disk and refilter them
                    entry.RestoreUnfiltered();
                    // stop real method from running, filter in postfix
                    return false;
                }
            }
            return true;
        }

        private class CharaListEntry
        {
            private readonly CharaList _charaList;
            public bool RefilterInProgress;
            private List<CharaFileInfo> _backupFileInfos;
            private string _currentFolder;
            private FolderTreeView _folderTreeView;
            private int _sex = -1;

            public CharaListEntry(CharaList charaList)
            {
                _charaList = charaList;
            }

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? BrowserFoldersPlugin.UserDataPath);

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            BrowserFoldersPlugin.UserDataPath,
                            Path.Combine(BrowserFoldersPlugin.UserDataPath, GetSex() != 0 ? "chara/female" : "chara/male"));
                        _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
                        _folderTreeView.CurrentFolderChanged = OnFolderChanged;
                        OnFolderChanged();
                    }
                    return _folderTreeView;
                }
            }

            public bool ListIsActive => _charaList.isActiveAndEnabled;

            public void ApplyFilter()
            {
                var currentFolder = CurrentFolder;
                GetCharaFileInfos().RemoveAll(cfi => Utils.GetNormalizedDirectoryName(cfi.file) != currentFolder);
            }

            public void InitCharaList(bool force)
            {
                _charaList.InitCharaList(force);
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
                return _charaList?.charaFileSort?.cfiList;
            }

            private int GetSex()
            {
                if (_sex == -1)
                    _sex = _charaList.sex;
                return _sex;
            }

            private void OnFolderChanged()
            {
                _currentFolder = Utils.NormalizePath(FolderTreeView.CurrentFolder);
                RefilterInProgress = true;
                InitCharaList(true);
            }
        }
    }
}
