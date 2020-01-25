using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BrowserFolders
{
    public static class Utils
    {
        public static void EatInputInRect(Rect eatRect)
        {
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('/', '\\').TrimEnd('\\').ToLower();
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

        public static IEnumerable<Type> GetTypesSafe(this Assembly ass)
        {
            try { return ass.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            catch { return Enumerable.Empty<Type>(); }
        }

        public static IEnumerable<T2> Attempt<T, T2>(this IEnumerable<T> items, Func<T,T2> action)
        {
            foreach (var item in items)
            {
                T2 result;
                try
                {
                    result = action(item);
                }
                catch
                {
                    continue;
                }
                yield return result;
            }
        }

        public static void OpenDirInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
            }
            catch (Exception e)
            {
                KK_BrowserFolders.Logger.LogError(e);
                KK_BrowserFolders.Logger.LogMessage("Failed to open the folder - " + e.Message);
            }
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