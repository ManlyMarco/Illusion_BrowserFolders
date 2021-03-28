using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BepInEx;
using UnityEngine;

namespace BrowserFolders
{
    public class FolderTreeView
    {
        private bool _scrollTreeToSelected;
        private float _lastRefreshedTime = 0f;
        private float _refreshRequestedTime = 0f;
        private long _lastRefreshedFrame = 0;
        private long _refreshRequestedFrame = 0;
        private FileSystemWatcher _fileSystemWatcher;

        public void ScrollListToSelected()
        {
            _searchString = "";
            _scrollTreeToSelected = true;
        }

        private readonly HashSet<string> _openedObjects = new HashSet<string>();
        private Vector2 _treeScrollPosition;
        private string _currentFolder;

        private DirectoryTree _defaultPathTree;

        public FolderTreeView(string topmostPath, string defaultPath)
        {
            if (!defaultPath.StartsWith(topmostPath, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("default path has to be inside topmost path");

            DefaultPath = defaultPath;
            _topmostPath = Path.GetFullPath(topmostPath.ToLowerInvariant().TrimEnd('\\'));
            _fileSystemWatcher = null;
            this.CurrentFolder = null;//Will init it to defaultPath
        }

        public string CurrentFolder
        {
            get => _currentFolder;
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = DefaultPath;

                var newPath = NormalizePath(value);
                if (_currentFolder == newPath) return;

                _currentFolder = newPath;
                CurrentRelativeFolder = _currentFolder.Length > _topmostPath.Length ? _currentFolder.Substring(_topmostPath.Length) : "/";

                CurrentFolderChanged?.Invoke();
            }
        }

        internal static string NormalizePath(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Path.GetFullPath(value).Replace('\\', '/').TrimEnd('/').ToLower() + "/";
        }

        public string CurrentRelativeFolder { get; private set; }

        public string DefaultPath
        {
            get => _defaultPath;
            set
            {
                _defaultPathTree = null;
                _defaultPath = NormalizePath(value);
            }
        }

        public DirectoryTree DefaultPathTree => _defaultPathTree ?? (_defaultPathTree = new DirectoryTree(new DirectoryInfo(DefaultPath)));

        public Action CurrentFolderChanged;
        private readonly string _topmostPath;
        private string _defaultPath;

        private string _searchString = "";
        private int _itemHeight;
        private int _scrollviewHeight;

        public void DrawDirectoryTree()
        {
            ExpandToCurrentFolder();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            {
                _treeScrollPosition = GUILayout.BeginScrollView(_treeScrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                {
                    var itemsDrawn = 0;
                    DisplayObjectTreeHelper(DefaultPathTree, 0, ref itemsDrawn);
                }
                GUILayout.EndScrollView();

                if (_scrollviewHeight == 0 && Event.current.type == EventType.Repaint)
                    _scrollviewHeight = (int)GUILayoutUtility.GetLastRect().height;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Search: ", GUILayout.ExpandWidth(false));
                    _searchString = GUILayout.TextField(_searchString).Replace('\\', '/');
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            try
            {
                StartMonitoringFiles();
            }
            catch (InvalidOperationException)
            {
                // stop monitoring and trigger refresh
                StopMonitoringFiles();
                ResetTreeCache();
            }
        }

        private void StartMonitoringFiles()
        {
            if (_fileSystemWatcher != null && _fileSystemWatcher.EnableRaisingEvents) return;
            InitFileSystemWatcher();
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void InitFileSystemWatcher()
        {
            if (_fileSystemWatcher != null) return;
            _fileSystemWatcher = new FileSystemWatcher()
            {
                Path = Path.GetFullPath(DefaultPath),
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                SynchronizingObject = ThreadingHelper.SynchronizingObject
            };
            _fileSystemWatcher.Created += (sender, e) => ResetTreeCache(false);
            _fileSystemWatcher.Deleted += (sender, e) => ResetTreeCache(false);
            _fileSystemWatcher.Renamed += (sender, e) => ResetTreeCache(false);
        }

        public void StopMonitoringFiles()
        {
            if (_fileSystemWatcher == null || !_fileSystemWatcher.EnableRaisingEvents) return;
            _fileSystemWatcher.EnableRaisingEvents = false;
        }

        internal void OnDestroy()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Dispose();
                _fileSystemWatcher = null;
            }
        }
        public void ResetTreeCache(bool force = true)
        {
            // request refresh after 2 seconds / 10 frames
            Interlocked.Exchange(ref _refreshRequestedTime, Mathf.Ceil(Time.realtimeSinceStartup + 2f));
            Interlocked.Exchange(ref _refreshRequestedFrame, Time.frameCount + 10);
            if (!force) return;
            Interlocked.Exchange(ref _lastRefreshedTime, 0f);
            Interlocked.Exchange(ref _lastRefreshedFrame, 0);
        }

        private bool TreeNeedsUpdate()
        {
            // don't refresh twice in same second
            if (Time.realtimeSinceStartup < _refreshRequestedTime || _refreshRequestedTime <= (_lastRefreshedTime + 1f)) return false;

            var lastRefreshedFrame = Interlocked.Read(ref _lastRefreshedFrame);
            var refreshRequestedFrame = Interlocked.Read(ref _refreshRequestedFrame);

            // don't refresh twice in same frame
            return (Time.frameCount > refreshRequestedFrame && refreshRequestedFrame > (lastRefreshedFrame + 1));
        }

        private void UpdateTreeCache()
        {
            if (!TreeNeedsUpdate()) return;
            DefaultPathTree.Reset();
            Interlocked.Exchange(ref _lastRefreshedFrame, Time.frameCount);
            Interlocked.Exchange(ref _lastRefreshedTime, Mathf.Ceil(Time.realtimeSinceStartup));
        }

        private void ExpandToCurrentFolder()
        {
            if (!Directory.Exists(CurrentFolder))
            {
                // folder deleted out from under us, refresh immediately and go up to first existing parent
                ResetTreeCache();
                var parent = CurrentFolder;
                try
                {
                    while (!Directory.Exists(parent)) parent = Path.GetDirectoryName(parent);
                    CurrentFolder = parent;
                }
                catch
                {
                    CurrentFolder = DefaultPath;
                }
            }

            UpdateTreeCache();

            var path = CurrentFolder;
            var defaultPath = DefaultPath;
            _openedObjects.Add(defaultPath);
            while (!string.IsNullOrEmpty(path) && path.Length > defaultPath.Length)
            {
                if (_openedObjects.Add(path))
                    path = NormalizePath(Path.GetDirectoryName(path));
                else
                    break;
            }
        }

        private void DisplayObjectTreeHelper(DirectoryTree dir, int indent, ref int itemsDrawn)
        {
            var dirFullName = dir.FullName;
            var subDirs = dir.SubDirs;

            if (indent == 0 && subDirs.Count == 0)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label(@"You can organize your files into folders and use this window to browse them.");
                    GUILayout.Space(5);
                    GUILayout.Label($@"Folders placed inside your {DefaultPath.Substring(_topmostPath.Length)} folder will appear on this list.");
                }
                GUILayout.EndVertical();
                return;
            }

            var isSearching = !string.IsNullOrEmpty(_searchString);
            if (!isSearching || dirFullName.IndexOf(_searchString, DefaultPath.Length, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var drawnItemTotalHeight = itemsDrawn * _itemHeight;

                if (_scrollTreeToSelected ||
                    _itemHeight == 0 || _scrollviewHeight == 0 ||
                    // Only draw items that are visible at current scroll position
                    drawnItemTotalHeight >= _treeScrollPosition.y - _itemHeight &&
                    drawnItemTotalHeight <= _treeScrollPosition.y + _scrollviewHeight + _itemHeight)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(indent * 20f);

                        var c = GUI.color;
                        if (dirFullName == CurrentFolder)
                        {
                            GUI.color = Color.cyan;
                            if (_scrollTreeToSelected && Event.current.type == EventType.Repaint)
                            {
                                _scrollTreeToSelected = false;
                                _treeScrollPosition.y = Mathf.Max(0, drawnItemTotalHeight - (_scrollviewHeight - _itemHeight) / 2);
                            }
                        }

                        GUILayout.BeginHorizontal();
                        {
                            if (subDirs.Count > 0)
                            {
                                if (GUILayout.Toggle(_openedObjects.Contains(dirFullName), "", GUILayout.ExpandWidth(false)))
                                    _openedObjects.Add(dirFullName);
                                else
                                    _openedObjects.Remove(dirFullName);
                            }
                            else
                            {
                                GUILayout.Space(20f);
                            }

                            if (GUILayout.Button(dir.Name, GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100)))
                            {
                                if (string.Equals(CurrentFolder, dirFullName, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (_openedObjects.Contains(dirFullName) == false)
                                        _openedObjects.Add(dirFullName);
                                    else
                                        _openedObjects.Remove(dirFullName);
                                }
                                CurrentFolder = dirFullName;
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUI.color = c;
                    }
                    GUILayout.EndHorizontal();

                    if (_itemHeight == 0 && Event.current.type == EventType.Repaint)
                        _itemHeight = (int)GUILayoutUtility.GetLastRect().height;
                }
                else
                {
                    GUILayout.Space(_itemHeight);
                }

                itemsDrawn++;
            }

            if (isSearching || _openedObjects.Contains(dirFullName))
            {
                foreach (var subDir in subDirs)
                    DisplayObjectTreeHelper(subDir, indent + 1, ref itemsDrawn);
            }
        }
    }
}