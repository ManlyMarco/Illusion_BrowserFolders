using BepInEx.Harmony;
using HarmonyLib;
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
        internal class CharaListEntry
        {
            private CharaList _charaList;
            private int _sex = -1;
            private FolderTreeView _folderTreeView = null;
            private string _currentFolder = null;
            private List<CharaFileInfo> _BackupFileInfos = null;
            public bool refilterInProgress = false;

            public CharaListEntry(CharaList charaList)
            {
                _charaList = charaList;
            }

            public List<CharaFileInfo> CharaFileInfos => Traverse.Create(_charaList)?.Field<CharaFileSort>("charaFileSort")?.Value?.cfiList;

            public int Sex
            {
                get
                {
                    if (_sex == -1)
                    {
                        _sex = Traverse.Create(_charaList).Field<int>("sex").Value;
                    }
                    return _sex;
                }
            }

            public FolderTreeView FolderTreeView
            {
                get
                {
                    if (_folderTreeView == null)
                    {
                        _folderTreeView = new FolderTreeView(
                            Utils.NormalizePath(UserData.Path),
                            Path.Combine(Utils.NormalizePath(UserData.Path), Sex != 0 ? "chara/female" : "chara/male"));
                        _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
                        _folderTreeView.CurrentFolderChanged = OnFolderChanged;
                        OnFolderChanged();
                    }
                    return _folderTreeView;
                }
            }

            public void InitCharaList(bool force)
            {
                _charaList.InitCharaList(force);
            }

            public void OnFolderChanged()
            {
                _currentFolder = Utils.NormalizePath(FolderTreeView.CurrentFolder);
                refilterInProgress = true;
                InitCharaList(true);
            }

            public bool isActiveAndEnabled => _charaList.isActiveAndEnabled;

            public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? UserData.Path);

            public void SaveFullList()
            {
                _BackupFileInfos = CharaFileInfos.ToList();
            }

            public void ApplyFilter()
            {
                CharaFileInfos.RemoveAll((cfi) => Utils.NormalizePath(Path.GetDirectoryName(cfi.file)) != CurrentFolder);
            }

            public void RestoreUnfiltered()
            {
                if (_BackupFileInfos != null)
                {
                    CharaFileInfos.Clear();
                    CharaFileInfos.AddRange(_BackupFileInfos);
                }
            }
        }

        private static Dictionary<string, CharaListEntry> CharaListEntries = null;
        private static bool refilterOnly = false;

        public StudioCharaFolders()
        {
            if (CharaListEntries == null)
            {
                CharaListEntries = new Dictionary<string, CharaListEntry>();
            }
            HarmonyWrapper.PatchAll(typeof(StudioCharaFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPrefix(CharaList __instance, bool _force)
        {
            if (!CharaListEntries.ContainsKey(__instance.name))
            {
                CharaListEntries[__instance.name] = new CharaListEntry(__instance);
            }
            refilterOnly = _force && CharaListEntries[__instance.name].refilterInProgress;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void InitCharaListPostfix(CharaList __instance)
        {
            if (CharaListEntries.TryGetValue(__instance.name, out CharaListEntry entry))
            {
                entry.refilterInProgress = false;
            }
            refilterOnly = false;
        }

        internal void Update()
        {
        }

        private static bool InitListPrefix(string name)
        {
            if (CharaListEntries.TryGetValue(name, out CharaListEntry entry))
            {
                // detect if force reload was triggered by a change of folder
                if (refilterOnly)
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

        private static void InitListPostfix(string name)
        {
            if (CharaListEntries.TryGetValue(name, out CharaListEntry entry))
            {
                if (!refilterOnly)
                {
                    // don't update results if we didn't get new ones
                    entry.SaveFullList();
                }
                // list must be filtered before the rest of InitCharaList runs
                entry.ApplyFilter();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static bool InitFemaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        internal static void InitFemaleLisPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static bool InitMaleListPrefix(CharaList __instance)
        {
            return InitListPrefix(__instance.name);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static void InitMaleListPostfix(CharaList __instance)
        {
            InitListPostfix(__instance.name);
        }

        public void OnGui()
        {
            foreach (CharaListEntry entry in CharaListEntries.Values)
            {
                if (entry.isActiveAndEnabled)
                {
                    var screenRect = new Rect((int)(Screen.width * 0.1f), (int)(Screen.height * 0.5f), (int)(Screen.width * 0.25f), (int)(Screen.height * 0.4f));
                    Utils.DrawSolidWindowBackground(screenRect);
                    GUILayout.Window(362, screenRect, (id) => TreeWindow(id, entry), "Select folder with cards to view");
                    break;
                }
            }
        }

        private static void TreeWindow(int id, CharaListEntry entry)
        {
            GUILayout.BeginVertical();
            {
                entry.FolderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh characters"))
                    {
                        entry.InitCharaList(true);

                        entry.FolderTreeView.CurrentFolderChanged.Invoke();
                    }
                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                    {
                        Utils.OpenDirInExplorer(entry.CurrentFolder);
                    }

                    if (GUILayout.Button("Screenshot folder"))
                    {
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    }

                    if (GUILayout.Button("Main game folder"))
                    {
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}