using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace BrowserFolders
{
    internal static class Utils
    {
        #region IMGUI

        public static readonly GUILayoutOption[] LayoutNone = { };
        public static readonly GUILayoutOption[] LayoutNoExpand = { GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false) };
        public static readonly GUILayoutOption[] LayoutExpand = { GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true) };

        #endregion

        #region Paths

        private static readonly Dictionary<string, string> _NormalizedDirectoryNames = new Dictionary<string, string>();

        public static string GetNormalizedDirectoryName(string filePath)
        {
            if (_NormalizedDirectoryNames.TryGetValue(filePath, out var result)) return result;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                if (!_NormalizedDirectoryNames.TryGetValue(dir, out result))
                {
                    _NormalizedDirectoryNames[dir] = result = NormalizePath(dir);
                }
            }
            else
            {
                result = string.Empty;
            }

            _NormalizedDirectoryNames[filePath] = result;
            return result;
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('/', '\\').TrimEnd('\\').ToLower();
        }

        public static void OpenDirInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
            }
            catch (Exception e)
            {
                BrowserFoldersPlugin.Logger.LogError(e);
                BrowserFoldersPlugin.Logger.LogMessage("Failed to open the folder - " + e.Message);
            }
        }

        #endregion
        
        public static IEnumerable<T2> Attempt<T, T2>(this IEnumerable<T> items, Func<T, T2> action)
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

        #region AutoTranslator compat

        private static GameObject _xua;
        private static bool _xuaChecked;

        private static GameObject XuaObject
        {
            get
            {
                if (_xuaChecked) return _xua;
                _xua = GameObject.Find("___XUnityAutoTranslator");
                _xuaChecked = true;
                return _xua;
            }
        }

        public static bool NonTranslatedButton(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            if (XuaObject == null)
                return GUILayout.Button(text, style, options);

            try
            {
                XuaObject.SendMessage("DisableAutoTranslator");
                return GUILayout.Button(text, style, options);
            }
            finally
            {
                XuaObject.SendMessage("EnableAutoTranslator");
            }
        }

        #endregion
    }
}