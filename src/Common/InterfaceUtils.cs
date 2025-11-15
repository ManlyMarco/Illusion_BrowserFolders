using System;
using System.IO;
using KKAPI.Utilities;
using UnityEngine;

namespace BrowserFolders
{
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

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
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
