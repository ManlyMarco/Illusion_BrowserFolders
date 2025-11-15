using System;
using System.IO;
using BepInEx.Configuration;
using HarmonyLib;
using KKAPI.Utilities;
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

        private const int WindowID = 362; // Assume at most one folder window can be visible at one time
        private readonly string _topmostPath;
        private readonly string _defaultPath;

        protected int GuiVisible;

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
        protected abstract int IsVisible();
        public abstract void OnListRefresh();
        public abstract Rect GetDefaultRect();

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

        public virtual void OnGui()
        {
            if (GuiVisible != 0)
            {
                var orig = GUI.skin;
                GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;

                WindowRect = GUILayout.Window(WindowID, WindowRect, DrawFolderWindow, Title);

                GUI.skin = orig;
            }
        }

        protected void DrawFolderWindow(int id)
        {
            DrawFolderWindow(id, this, DrawControlButtons);
        }

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

            // todo dial in the constant, or better measure height
            //if (WindowRect.height > 400 || isHorizontal && WindowRect.height > 200)
            {
                if (GUILayout.Button("Screenshot folder"))
                    Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                if (GUILayout.Button("Main game folder"))
                    Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
            }
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
