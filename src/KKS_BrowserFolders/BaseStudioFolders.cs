using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
{
    public abstract class BaseStudioFolders<T, TSub, THelper> : IFolderBrowser
        where THelper : BaseStudioFoldersHelper<T, TSub>, new()
        where T : BaseListEntry<TSub>
    {
        private static T _lastEntry;

        private static readonly THelper _Helper = new THelper();

        protected string RefreshLabel = "Refresh files";
        protected string WindowLabel = "Select folder to view";

        public Rect WindowRect { get; set; }

        public abstract bool Initialize(bool isStudio, ConfigFile config, Harmony harmony);

        public void Update()
        {
            var entry = _Helper.GetActiveEntry();
            if (_lastEntry != null && entry == null)
                _lastEntry.FolderTreeView?.StopMonitoringFiles();

            _lastEntry = entry;
        }

        public void OnGui()
        {
            var entry = _lastEntry;
            if (entry == null) return;

            InterfaceUtils.DisplayFolderWindow(tree: entry.FolderTreeView, getWindowRect: () => WindowRect, setWindowRect: r => WindowRect = r, title: WindowLabel, onRefresh: () =>
            {
                entry.InitListRefresh();
                entry.FolderTreeView.CurrentFolderChanged.Invoke();
            }, drawAdditionalButtons: () =>
            {
                if (BrowserFoldersPlugin.DrawDefaultCardsToggle())
                    entry.InitListRefresh();
            }, getDefaultRect: GetDefaultRect);
        }

        public virtual Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.06f), (int)(Screen.height * 0.32f),
                            (int)(Screen.width * 0.13f), (int)(Screen.height * 0.4f));
        }

        public static T GetListEntry(TSub gameList)
        {
            return _Helper.GetListEntry(gameList);
        }

        public static bool TryGetListEntry(TSub gameList, out T listEntry)
        {
            return _Helper.TryGetListEntry(gameList, out listEntry);
        }

        protected static void SetRefilterOnly(TSub gameList, bool globalCheck)
        {
            _Helper.RefilterOnly = globalCheck && gameList != null && GetListEntry(gameList).RefilterInProgress;
        }

        protected static bool GetRefilterOnly(TSub gameList)
        {
            return gameList != null && _Helper.RefilterOnly;
        }

        protected static bool InitListPrefix(TSub gameList)
        {
            try
            {
                var entry = GetListEntry(gameList);

                if (!GetRefilterOnly(gameList))
                {
                    // if we're not refiltering, clear all caches
                    entry.ClearCaches();
                }
                else
                {
                    // use cached version is available, just use it
                    if (entry.TryRestoreCurrentFolderList()) return false;
                }

                var rootFolder = entry.GetRoot();
                var overrideFolder = entry.CurrentFolder;
                if (overrideFolder.IsNullOrEmpty()) overrideFolder = rootFolder;
                StudioFileHelper.SetGetAllFilesOverride(rootFolder, "*.png", overrideFolder);
            }
            catch (Exception err)
            {
                // if anything went wrong, just fall though to standard call
                Debug.LogException(err);
            }

            return true;
        }

        protected static void InitListPostfix(TSub gameList)
        {
            if (!TryGetListEntry(gameList, out var entry)) return;

            if (!GetRefilterOnly(gameList))
            {
                // don't update results if we didn't get new ones
                entry.SaveCurrentFolderList();
            }

            // list must be filtered before the returning to the calling scope
            // if everything worked, this will do nothing, but if we fell though to default code
            // or some other plugin messed with the lists, this will limit to current folder selection
            entry.ApplyFilter();

            // clear the override
            StudioFileHelper.SetGetAllFilesOverride(entry.GetRoot(), "*.png", null);
        }
    }

    public abstract class BaseListEntry<TList>
    {
        private readonly Dictionary<string, List<CharaFileInfo>> _backupFileInfos =
            new Dictionary<string, List<CharaFileInfo>>();

        internal readonly TList _list;

        private string _currentFolder;
        private FolderTreeView _folderTreeView;
        public bool RefilterInProgress;

        protected BaseListEntry(TList list)
        {
            _list = list;
        }

        public string CurrentFolder => _currentFolder ?? Utils.NormalizePath(_folderTreeView?.CurrentFolder ?? GetRoot());

        public FolderTreeView FolderTreeView
        {
            get
            {
                if (_folderTreeView == null)
                {
                    _folderTreeView = new FolderTreeView(
                        Utils.NormalizePath(UserData.Path),
                        Utils.NormalizePath(GetRoot()));
                    _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
                    _folderTreeView.CurrentFolderChanged = OnFolderChanged;
                    OnFolderChanged();
                }

                return _folderTreeView;
            }
        }

        public abstract bool isActiveAndEnabled { get; }
        public abstract List<CharaFileInfo> GetCharaFileInfos();

        /// <summary>
        /// Called to reinitialize list when FolderTreeView folder changes
        /// </summary>
        public abstract void InitListFolderChanged();

        /// <summary>
        /// Called to reinitialize list when refresh requested
        /// </summary>
        public abstract void InitListRefresh();

        protected abstract int GetSex();
        protected internal abstract string GetRoot();

        public void ApplyFilter()
        {
            // normally this does nothing, but in case something caused it to fall back to standard code path
            // this will allow per-folder filtering to still work
            var currentFolder = CurrentFolder;
            var defaultFolder = BrowserFoldersPlugin.ShowDefaultCharas.Value ? Utils.GetNormalizedDirectoryName(Path.GetFullPath(Studio.DefaultData.Path)) : null;
            GetCharaFileInfos().RemoveAll(cfi =>
            {
                var directoryName = Utils.GetNormalizedDirectoryName(cfi.file);
                return directoryName != currentFolder && (defaultFolder == null || !directoryName.StartsWith(defaultFolder, StringComparison.OrdinalIgnoreCase));
            });
        }

        public bool TryRestoreCurrentFolderList()
        {
            if (!_backupFileInfos.TryGetValue(CurrentFolder, out var cfis)) return false;
            var fileInfos = GetCharaFileInfos();
            fileInfos.Clear();
            fileInfos.AddRange(cfis);
            return true;
        }

        public void SaveCurrentFolderList()
        {
            _backupFileInfos[CurrentFolder] = GetCharaFileInfos().ToList();
        }

        public void ClearCaches()
        {
            _folderTreeView = null;
            _backupFileInfos.Clear();
        }

        private void OnFolderChanged()
        {
            _currentFolder = Utils.NormalizePath(FolderTreeView.CurrentFolder);
            RefilterInProgress = true;

            InitListFolderChanged();
        }
    }

    public abstract class BaseStudioFoldersHelper<T, TSub> where T : BaseListEntry<TSub>
    {
        private static readonly Dictionary<int, T> ListEntries = new Dictionary<int, T>();
        internal abstract int GetListEntryIndex(TSub gameList);
        protected abstract T CreateNewListEntry(TSub gameList);

        internal bool RefilterOnly;

        internal bool TryGetListEntry(TSub gameList, out T listEntry)
        {
            return ListEntries.TryGetValue(GetListEntryIndex(gameList), out listEntry);
        }

        internal T GetListEntry(TSub gameList)
        {
            if (!TryGetListEntry(gameList, out var entry))
            {
                entry = ListEntries[GetListEntryIndex(gameList)] = CreateNewListEntry(gameList);
            }

            return entry;
        }

        internal T GetActiveEntry()
        {
            return ListEntries.Values.FirstOrDefault(x => x.isActiveAndEnabled);
        }
    }
}