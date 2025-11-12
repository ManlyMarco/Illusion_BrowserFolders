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

        private int _id = 362; // Assume at most one folder window can be visible at one time
        private int _guiVisible;
        private readonly string _topmostPath;
        private readonly string _defaultPath;

        //public bool Enabled { get; set; } = true;
        public Rect WindowRect { get; protected set; }
        public FolderTreeView TreeView { get; protected set; }
        public string Title { get; protected set; }

        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            // todo store WindowRect in a setting
            WindowRect = GetDefaultRect();
            TreeView = new FolderTreeView(_topmostPath, _defaultPath);
            TreeView.CurrentFolderChanged = OnListRefresh;
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
        protected abstract Rect GetDefaultRect();

        protected virtual void DrawFolderWindow(int id)
        {
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
        //todo
        // convert into an instance class to avoid passing all those lambdas
        // take in get default rect lambda and keep rect handling internal
        // save rect to settings?
        // ensure rect is on screen
        // a way to reset to default rect
        // try to make completely generic, only needs to be called in ongui
        public static void DisplayFolderWindow(FolderTreeView tree, Func<Rect> getWindowRect, Action<Rect> setWindowRect, string title, Action onRefresh, Action drawAdditionalButtons = null, bool hideCapAndGameFolderBtns = false)
        {
            var orig = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

            var windowRect = GUILayout.Window(362, getWindowRect(), id => setWindowRect(DisplayFolderWindowInt(id, tree, getWindowRect(), onRefresh, drawAdditionalButtons, hideCapAndGameFolderBtns)), title);
            setWindowRect(windowRect);

            GUI.skin = orig;
        }
        private static Rect DisplayFolderWindowInt(int id, FolderTreeView tree, Rect windowRect, Action onRefresh, Action drawAdditionalButtons, bool hideCapAndGameFolderBtns)
        {
            // todo switch to horizontal based on aspect ratio, show more buttons then, needed for scenes
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
            GUILayout.EndVertical();

            return IMGUIUtils.DragResizeEatWindow(id, windowRect);
        }
    }
}