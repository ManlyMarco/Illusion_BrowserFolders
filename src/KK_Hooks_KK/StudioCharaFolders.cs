﻿using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.StudioChara)]
    public class StudioCharaFolders : IFolderBrowser
    {
        private static readonly Dictionary<string, CharaListEntry> _charaListEntries = new Dictionary<string, CharaListEntry>();
        private static bool _refilterOnly;
        private static CharaListEntry _lastEntry;

        public StudioCharaFolders()
        {
            Harmony.CreateAndPatchAll(typeof(StudioCharaFolders));
        }

        public void OnGui()
        {
            var entry = _charaListEntries.Values.FirstOrDefault(x => x.isActiveAndEnabled);
            if (_lastEntry != null && _lastEntry != entry)
            {
                _lastEntry.FolderTreeView?.StopMonitoringFiles();
                _lastEntry = null;
            }

            if (entry == null) return;
            _lastEntry = entry;
            var windowRect = new Rect((int) (Screen.width * 0.06f), (int) (Screen.height * 0.32f), (int) (Screen.width * 0.13f), (int) (Screen.height * 0.4f));
            IMGUIUtils.DrawSolidBox(windowRect);
            GUILayout.Window(363, windowRect, id => TreeWindow(entry), "Select folder with cards to view");
            IMGUIUtils.EatInputInRect(windowRect);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPostfix(CharaList __instance)
        {
            if (_charaListEntries.TryGetValue(__instance.name, out var entry))
                entry.RefilterInProgress = false;
            _refilterOnly = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPrefix(CharaList __instance, bool _force)
        {
            if (!_charaListEntries.ContainsKey(__instance.name))
                _charaListEntries[__instance.name] = new CharaListEntry(__instance);
            _refilterOnly = _force && _charaListEntries[__instance.name].RefilterInProgress;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static void InitFemaleLisPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static bool InitFemaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static void InitMaleListPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static bool InitMaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        private static void InitListPostfix(string name)
        {
            if (_charaListEntries.TryGetValue(name, out var entry))
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
            if (_charaListEntries.TryGetValue(name, out var entry))
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

        private static void TreeWindow(CharaListEntry entry)
        {
            GUILayout.BeginVertical();
            {
                entry.FolderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh characters"))
                    {
                        entry.InitCharaList(true);
                        entry.FolderTreeView.ResetTreeCache();
                        entry.FolderTreeView.CurrentFolderChanged.Invoke();
                    }
                    GUILayout.Space(1);

                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(entry.CurrentFolder);
                    if (GUILayout.Button("Screenshot folder"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    if (GUILayout.Button("Main game folder"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
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

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? UserData.Path);

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            Utils.NormalizePath(UserData.Path),
                            Path.Combine(Utils.NormalizePath(UserData.Path), GetSex() != 0 ? "chara/female" : "chara/male"));
                        _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
                        _folderTreeView.CurrentFolderChanged = OnFolderChanged;
                        OnFolderChanged();
                    }
                    return _folderTreeView;
                }
            }

            public bool isActiveAndEnabled => _charaList.isActiveAndEnabled;

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
                return _charaList.charaFileSort.cfiList;
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
