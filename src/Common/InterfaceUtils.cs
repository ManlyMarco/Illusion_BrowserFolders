using System;
using KKAPI.Utilities;
using System.IO;
using UnityEngine;

namespace BrowserFolders
{
    public static class InterfaceUtils
    {
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