using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace AI_BrowserFolders
{
    public class FolderTreeView
    {
        private bool _scrollTreeToSelected;

        public void ScrollListToSelected()
        {
            _scrollTreeToSelected = true;
        }

        private readonly HashSet<string> _openedObjects = new HashSet<string>();
        private Vector2 _treeScrollPosition;
        private string _currentFolder;

        public FolderTreeView(string topmostPath, string defaultPath)
        {
            if (!defaultPath.StartsWith(topmostPath, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("default path has to be inside topmost path");

            DefaultPath = defaultPath;
            _topmostPath = Path.GetFullPath(topmostPath.ToLowerInvariant().TrimEnd('\\'));
        }

        public string CurrentFolder
        {
            get => _currentFolder;
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = DefaultPath;

                var lowVal = Path.GetFullPath(value.TrimEnd('\\')).ToLower() + "/";
                if (_currentFolder == lowVal) return;

                _currentFolder = lowVal;
                CurrentRelativeFolder = _currentFolder.Length > _topmostPath.Length ? _currentFolder.Substring(_topmostPath.Length) : "/";

                CurrentFolderChanged?.Invoke();
            }
        }

        public string CurrentRelativeFolder { get; private set; }

        public string DefaultPath
        {
            get { return _defaultPath; }
            set
            {
                if (value != null) _defaultPath = Path.GetFullPath(value.TrimEnd('\\'));
                else _defaultPath = null;
            }
        }

        public Action CurrentFolderChanged;
        private readonly string _topmostPath;
        private string _defaultPath;

        public void DrawDirectoryTree()
        {
            ExpandToCurrentFolder();

            _treeScrollPosition = GUILayout.BeginScrollView(
                _treeScrollPosition, GUI.skin.box,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            {
                DisplayObjectTreeHelper(new DirectoryInfo(DefaultPath), 0);
            }
            GUILayout.EndScrollView();
        }

        private void ExpandToCurrentFolder()
        {
            var path = CurrentFolder;
            var defaultPath = DefaultPath;
            _openedObjects.Add(defaultPath.ToLowerInvariant());
            while (!string.IsNullOrEmpty(path) && path.Length > defaultPath.Length)
            {
                _openedObjects.Add(path.ToLowerInvariant());
                path = Path.GetDirectoryName(path)?.TrimEnd('\\');
            }
        }

        private void DisplayObjectTreeHelper(DirectoryInfo dir, int indent)
        {
            var fullNameLower = dir.FullName.ToLower();
            var subDirs = dir.GetDirectories();

            if (indent == 0 && subDirs.Length == 0)
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

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(indent * 20f);

                var c = GUI.color;
                if (fullNameLower.TrimEnd('/', '\\') == CurrentFolder.TrimEnd('/'))
                {
                    GUI.color = Color.cyan;
                    if (_scrollTreeToSelected && Event.current.type == EventType.Repaint)
                    {
                        _scrollTreeToSelected = false;
                        _treeScrollPosition.y = GUILayoutUtility.GetLastRect().y - 50;
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    if (subDirs.Length > 0)
                    {
                        if (GUILayout.Toggle(_openedObjects.Contains(fullNameLower), "", GUILayout.ExpandWidth(false)))
                            _openedObjects.Add(fullNameLower);
                        else
                            _openedObjects.Remove(fullNameLower);
                    }
                    else
                    {
                        GUILayout.Space(20f);
                    }

                    if (GUILayout.Button(dir.Name, GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(100)))
                    {
                        if (string.Equals(CurrentFolder, fullNameLower, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_openedObjects.Contains(fullNameLower) == false)
                                _openedObjects.Add(fullNameLower);
                            else
                                _openedObjects.Remove(fullNameLower);
                        }
                        CurrentFolder = fullNameLower;
                    }
                }
                GUILayout.EndHorizontal();

                GUI.color = c;
            }
            GUILayout.EndHorizontal();

            if (_openedObjects.Contains(fullNameLower))
            {
                foreach (var subDir in subDirs.OrderBy(x => x.Name, new Utils.WindowsStringComparer()))
                    DisplayObjectTreeHelper(subDir, indent + 1);
            }
        }
    }
}