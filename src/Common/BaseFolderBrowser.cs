using System;
using System.IO;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;

namespace BrowserFolders
{
    /// <inheritdoc cref="IFolderBrowser"/>
    public abstract class BaseFolderBrowser : IFolderBrowser
    {
        protected BaseFolderBrowser(string title, string topmostPath, string defaultPath)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            _topmostPath = topmostPath ?? throw new ArgumentNullException(nameof(topmostPath));
            _defaultPath = defaultPath ?? throw new ArgumentNullException(nameof(defaultPath));
        }

        private const int WindowID = 362; // Assume at most one folder window can be visible at one time
        private readonly string _topmostPath;
        private readonly string _defaultPath;

        protected int GuiVisible;

        /// <inheritdoc/>
        public Rect WindowRect { get; set; }
        /// <inheritdoc/>
        public FolderTreeView TreeView { get; protected set; }
        /// <inheritdoc/>
        public string Title { get; protected set; }

        /// <inheritdoc/>
        public bool Initialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            TreeView = new FolderTreeView(_topmostPath, _defaultPath)
            {
                CurrentFolderChanged = OnListRefresh
            };
            return OnInitialize(isStudio, config, harmony);
        }

        /// <inheritdoc cref="IFolderBrowser.Initialize"/>
        protected abstract bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony);
        /// <summary>
        /// Check if this browser should be visible.
        /// 0 = not visible, any number above is visible.
        /// If the returned number changes the list is refreshed.
        /// </summary>
        protected abstract int IsVisible();
        /// <inheritdoc/>
        public abstract void OnListRefresh();
        /// <inheritdoc/>
        public abstract Rect GetDefaultRect();

        /// <inheritdoc/>
        public virtual void Update()
        {
            var newVisible = IsVisible();
            if (newVisible != GuiVisible)
            {
                if (newVisible == 0 && GuiVisible != 0)
                    TreeView.StopMonitoringFiles();

                OnListRefresh();
            }
            GuiVisible = newVisible;
        }

        /// <inheritdoc/>
        public virtual void OnGui()
        {
            if (GuiVisible != 0)
            {
                var orig = GUI.skin;
                GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

                WindowRect = GUILayout.Window(WindowID, WindowRect, id => DrawFolderWindow(id, this, DrawControlButtons), Title);

                GUI.skin = orig;
            }
        }

        /// <summary>
        /// Override to draw additional control buttons. Draw behind or after the base buttons by placing the base call accordingly.
        /// </summary>
        protected virtual void DrawControlButtons()
        {
            DrawBaseControlButtons(this);
        }

        private static void DrawFolderWindow(int id, IFolderBrowser instance, Action drawControlButtons)
        {
            var borderTop = GUI.skin.window.border.top - 4;
            if (GUI.Button(new Rect(instance.WindowRect.width - borderTop - 2, 2, borderTop + 4, borderTop), "R"))
                instance.WindowRect = instance.GetDefaultRect();

            var isHorizontal = instance.WindowRect.width > instance.WindowRect.height;
            if (isHorizontal)
                GUILayout.BeginHorizontal();
            else
                GUILayout.BeginVertical();
            {
                instance.TreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                {
                    drawControlButtons();
                }
                GUILayout.EndVertical();
            }
            if (isHorizontal)
                GUILayout.EndHorizontal();
            else
                GUILayout.EndVertical();

            instance.WindowRect = IMGUIUtils.DragResizeEatWindow(id, instance.WindowRect);
        }

        private static void DrawBaseControlButtons(IFolderBrowser instance)
        {
            if (GUILayout.Button("Refresh thumbnails"))
            {
                instance.TreeView.ResetTreeCache();
                instance.OnListRefresh();
            }

            GUILayout.Space(1);

            if (GUILayout.Button("Current folder"))
                Utils.OpenDirInExplorer(instance.TreeView.CurrentFolder);
            if (GUILayout.Button("Screenshot folder"))
                Utils.OpenDirInExplorer(Path.Combine(BrowserFoldersPlugin.UserDataPath, "cap"));
            if (GUILayout.Button("Main game folder"))
                Utils.OpenDirInExplorer(Path.GetDirectoryName(BrowserFoldersPlugin.UserDataPath));
        }

        public static void DisplayFolderWindow(IFolderBrowser instance, Action drawAdditionalButtons = null)
        {
            var orig = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

            instance.WindowRect = GUILayout.Window(WindowID, instance.WindowRect, id =>
            {
                DrawFolderWindow(id, instance, () =>
                {
                    drawAdditionalButtons?.Invoke();
                    DrawBaseControlButtons(instance);
                });
            }, instance.Title);

            GUI.skin = orig;
        }
    }
}
