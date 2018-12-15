using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FolderBrowser
{
    internal static class Utils
    {
        public static string GetUserDataPath()
        {
            return Path.GetFullPath(UserData.Path).Replace('/', '\\').TrimEnd('\\').ToLower();
        }

        private static Texture2D WindowBackground { get; set; }

        public static void DrawSolidWindowBackground(Rect windowRect)
        {
            if (WindowBackground == null)
            {
                var windowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                windowBackground.SetPixel(0, 0, new Color(0.84f, 0.84f, 0.84f));
                windowBackground.Apply();
                WindowBackground = windowBackground;
            }

            // It's necessary to make a new GUIStyle here or the texture doesn't show up
            GUI.Box(windowRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = WindowBackground } });
        }

        public class WindowsStringComparer : IComparer<string>
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            static extern int StrCmpLogicalW(String x, String y);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }

        }
    }
}