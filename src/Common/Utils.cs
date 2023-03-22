using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;

namespace BrowserFolders
{
    public static class Utils
    {
        private static readonly Dictionary<string, string> _normalizedDirectoryNames = new Dictionary<string, string>();
        
        public static string GetNormalizedDirectoryName(string filePath)
        {
            if (_normalizedDirectoryNames.TryGetValue(filePath, out var result)) return result;

            var dir = Path.GetDirectoryName(filePath);
            if (!dir.IsNullOrEmpty())
            {
                if (!_normalizedDirectoryNames.TryGetValue(dir, out result))
                {
                    _normalizedDirectoryNames[dir] = result = NormalizePath(dir);
                }
            }
            else
            {
                result = string.Empty;
            }

            _normalizedDirectoryNames[filePath] = result;
            return result;
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('/', '\\').TrimEnd('\\').ToLower();
        }

        public static IEnumerable<Type> GetTypesSafe(this Assembly ass)
        {
            try { return ass.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(x => x != null); }
            catch { return Enumerable.Empty<Type>(); }
        }

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

        public static ManualLogSource Logger
        {
            get
            {
#if KK
                return KK_BrowserFolders.Logger;
#elif KKS         
                return KKS_BrowserFolders.Logger;
#elif EC
                return EC_BrowserFolders.Logger;
#else
                return AI_BrowserFolders.Logger;
#endif
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
                Logger.LogError(e);
                Logger.LogMessage("Failed to open the folder - " + e.Message);
            }
        }

        public static bool IsEmpty(this UnityEngine.Rect value)
        {
            return value.height == 0 || value.width == 0;
        }
    }
}