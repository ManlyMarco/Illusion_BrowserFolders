using System;
using KKAPI.Utilities;
using System.IO;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders
{
    public abstract class BaseFolderBrowser : IFolderBrowser
    {
        protected BaseFolderBrowser(string title, string topmostPath, string defaultPath)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            _topmostPath = topmostPath ?? throw new ArgumentNullException(nameof(topmostPath));
            _defaultPath = defaultPath ?? throw new ArgumentNullException(nameof(defaultPath));
        }

        private readonly int _id = 362; // Assume at most one folder window can be visible at one time
        private readonly string _topmostPath;
        private readonly string _defaultPath;

        private int _guiVisible;

        public Rect WindowRect { get; set; }
        public FolderTreeView TreeView { get; protected set; }
        public string Title { get; protected set; }

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            TreeView = new FolderTreeView(_topmostPath, _defaultPath)
            {
                CurrentFolderChanged = OnListRefresh
            };
            return OnInitialize(isStudio, config, harmony);
        }

        protected abstract bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony);

        public virtual void Update()
        {
            var newVisible = IsVisible();
            if (newVisible != _guiVisible)
            {
                if (newVisible == 0 && _guiVisible != 0)
                    TreeView.StopMonitoringFiles();

                OnListRefresh();
            }
            _guiVisible = newVisible;
        }

        public virtual void OnGui()
        {
            if (_guiVisible != 0)
            {
                var orig = GUI.skin;
                GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

                WindowRect = GUILayout.Window(_id, WindowRect, DrawFolderWindow, Title);

                GUI.skin = orig;
            }
        }
        protected abstract int IsVisible();
        protected abstract void OnListRefresh();

        protected virtual void DrawControlButtons()
        {
            if (GUILayout.Button("Refresh thumbnails"))
            {
                TreeView.ResetTreeCache();
                OnListRefresh();
            }

            GUILayout.Space(1);

            if (GUILayout.Button("Current folder"))
                Utils.OpenDirInExplorer(TreeView.CurrentFolder);

            // todo dial in the constant, or better measure height
            //if (WindowRect.height > 400 || isHorizontal && WindowRect.height > 200)
            {
                if (GUILayout.Button("Screenshot folder"))
                    Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                if (GUILayout.Button("Main game folder"))
                    Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
            }
        }

        public abstract Rect GetDefaultRect();

        protected virtual void DrawFolderWindow(int id)
        {
            var borderTop = GUI.skin.window.border.top - 4;
            if (GUI.Button(new Rect(WindowRect.width - borderTop - 2, 2, borderTop + 4, borderTop), "R"))
                WindowRect = GetDefaultRect();

            //TODO fix searchbox losing focus

            var isHorizontal = WindowRect.width > WindowRect.height;
            if (isHorizontal)
                GUILayout.BeginHorizontal();
            else
                GUILayout.BeginVertical();
            {
                TreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    DrawControlButtons();
                }
                GUILayout.EndVertical();
            }
            if (isHorizontal)
                GUILayout.EndHorizontal();
            else
                GUILayout.EndVertical();

            WindowRect = IMGUIUtils.DragResizeEatWindow(id, WindowRect);
        }
    }

    public static class InterfaceUtils
    {
        public static void DisplayFolderWindow(FolderTreeView tree, Func<Rect> getWindowRect, Action<Rect> setWindowRect, string title, Action onRefresh, Func<Rect> getDefaultRect, Action drawAdditionalButtons = null, bool hideCapAndGameFolderBtns = false)
        {
            var orig = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

            var windowRect = GUILayout.Window(362, getWindowRect(), id => setWindowRect(DisplayFolderWindowInt(id, tree, getWindowRect(), onRefresh, getDefaultRect, drawAdditionalButtons, hideCapAndGameFolderBtns)), title);
            setWindowRect(windowRect);

            GUI.skin = orig;
        }
        private static Rect DisplayFolderWindowInt(int id, FolderTreeView tree, Rect windowRect, Action onRefresh, Func<Rect> getDefaultRect, Action drawAdditionalButtons, bool hideCapAndGameFolderBtns)
        {
            var borderTop = GUI.skin.window.border.top - 4;
            if (GUI.Button(new Rect(windowRect.width - borderTop - 2, 2, borderTop + 4, borderTop), "R"))
                windowRect = getDefaultRect();

            var isHorizontal = windowRect.width > windowRect.height;
            if (isHorizontal)
                GUILayout.BeginHorizontal();
            else
                GUILayout.BeginVertical();
            {
                tree.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    drawAdditionalButtons?.Invoke();

                    if (GUILayout.Button("Refresh thumbnails"))
                    {
                        tree.ResetTreeCache();
                        onRefresh();
                    }

                    GUILayout.Space(1);

                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(tree.CurrentFolder);

                    // todo show based on height and remove this param
                    if (!hideCapAndGameFolderBtns)
                    {
                        if (GUILayout.Button("Screenshot folder"))
                            Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                        if (GUILayout.Button("Main game folder"))
                            Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                    }
                }
                GUILayout.EndVertical();
            }
            if (isHorizontal)
                GUILayout.EndHorizontal();
            else
                GUILayout.EndVertical();

            return IMGUIUtils.DragResizeEatWindow(id, windowRect);
        }
    }
}
