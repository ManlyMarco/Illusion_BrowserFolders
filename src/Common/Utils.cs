using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BrowserFolders
{
    public static class Utils
    {
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

        public static void OpenDirInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
            }
            catch (Exception e)
            {
#if KK
                KK_BrowserFolders.Logger.LogError(e);
                KK_BrowserFolders.Logger.LogMessage("Failed to open the folder - " + e.Message);
#elif EC
                EC_BrowserFolders.Logger.LogError(e);
                EC_BrowserFolders.Logger.LogMessage("Failed to open the folder - " + e.Message);
#else
                AI_BrowserFolders.Logger.LogError(e);
                AI_BrowserFolders.Logger.LogMessage("Failed to open the folder - " + e.Message);
#endif
            }
        }
    }
}