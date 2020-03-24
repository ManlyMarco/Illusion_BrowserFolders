using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.StudioOutfit)]
    public class StudioOutfitFolders : IFolderBrowser
    {
        
        private static readonly Dictionary<int, CostumeInfoEntry> _costumeInfoEntries = new Dictionary<int, CostumeInfoEntry>();
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
                var prefix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitFileListPrefix));
                var postfix = AccessTools.Method(typeof(StudioOutfitFolders), nameof(InitFileListPostfix));
                harmony.Patch(target, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            }
            
        }

        //note to self
        //InitList == InitCharaList
        //InitFileList === InitMaleList/InitFemaleList
        //InitList is the one we have to re-call to refresh the display though

        public void OnGui()
        {
            var entry = _costumeInfoEntries.Values.SingleOrDefault(x => x.isActive());
            if (entry == null) return;
            var windowRect = new Rect((int) (Screen.width * 0.06f), (int) (Screen.height * 0.32f), (int) (Screen.width * 0.13f), (int) (Screen.height * 0.4f));
            Utils.DrawSolidWindowBackground(windowRect);
            GUILayout.Window(363, windowRect, id => TreeWindow(entry), "Select folder with outfits to view");
            Utils.EatInputInRect(windowRect);
        }

        
        internal static void InitCostumeListPostfix(object __instance, ref int ___sex, ref int __state)
        {
            ___sex = __state;
            if (_costumeInfoEntries.TryGetValue(__instance.GetHashCode(), out var entry))
                entry.RefilterInProgress = false;
            _refilterOnly = false;
        }
        
        internal static void InitCostumeListPrefix(int _sex, object __instance, ref int __state, ref int ___sex)
        {
            //If the CostumeInfo.sex field is equal to the parameter _sex, the method doesn't do anything
            __state = _sex;
            ___sex = 99;
            if (!_costumeInfoEntries.ContainsKey(__instance.GetHashCode()))
                _costumeInfoEntries[__instance.GetHashCode()] = new CostumeInfoEntry(__instance);
            _refilterOnly = _costumeInfoEntries[__instance.GetHashCode()].RefilterInProgress;
        }
        
        internal static void InitFileListPrefix( object __instance) 
        {        
            InitListPrefix(__instance.GetHashCode());
        }
        
        internal static void InitFileListPostfix(object __instance) 
        {    
            InitListPostfix(__instance.GetHashCode());
        }       
          
        
        private static void InitListPostfix(int hash)
        {
            if (_costumeInfoEntries.TryGetValue(hash, out var entry))
            {
               // if (!_refilterOnly)
               // {
                    // don't update results if we didn't get new ones
                   entry.SaveFullList();
              //  }
                // list must be filtered before the rest of InitCharaList runs
               entry.ApplyFilter();                
            }
        }

        public static bool InitListPrefix(int hash)
        {
            if (_costumeInfoEntries.TryGetValue(hash, out var entry))
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
        
        private static void TreeWindow(CostumeInfoEntry entry)
        {
            GUILayout.BeginVertical();
            {
                entry.FolderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh outfits"))
                    {
                        entry.InitOutfitList();

                        entry.FolderTreeView.CurrentFolderChanged.Invoke();
                    }
                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
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
        
        private class CostumeInfoEntry
        {
            private readonly object _costumeInfo;
            public bool RefilterInProgress;
            private List<CharaFileInfo> _backupFileInfos;
            private string _currentFolder;
            private FolderTreeView _folderTreeView;
            private int _sex = -1;
            private GameObject _costumePanel;
        

            public CostumeInfoEntry(object costumeInfo)
            {
                _costumeInfo = costumeInfo;
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

            public bool isActive() 
            {
                if (_costumePanel == null) 
                {
                    var scene = GameObject.Find("StudioScene");
                    _costumePanel = scene?.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "05_Costume").gameObject;                   
                }
                if (_costumePanel == null) return false;
                return _costumePanel.activeInHierarchy;                
            }

            public void ApplyFilter()
            {
                GetCharaFileInfos().RemoveAll(cfi => Utils.NormalizePath(Path.GetDirectoryName(cfi.file)) != CurrentFolder);               
                
            }

            public void InitOutfitList()
            {
                //_charaList.InitCharaList(force);                 
                Traverse.Create(_costumeInfo).Method("InitList", new object[]{ GetSex()})?.GetValue();
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
