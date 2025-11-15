using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;

namespace BrowserFolders.Studio
{
    public static class StudioFileHelper
    {
        private static bool _hooked;
        private static readonly object _lock = new object();

        // GetAllFilesOverrideFolders[searchPattern][defaultFolder] => overrideFolder
        private static readonly Dictionary<string, Dictionary<string, string>> GetAllFilesOverrideFolders =
            new Dictionary<string, Dictionary<string, string>>();

        public static void SetGetAllFilesOverride(string defaultFolder, string searchPattern, string overrideFolder)
        {
            lock (_lock)
            {
                if (!_hooked) 
                    InitHooks();
            }

            // keyed by searchPattern, then defaultFolder to avoid any excess calls to NormalizePath

            var normalizedDefaultFolder = Utils.NormalizePath(Path.GetFullPath(defaultFolder));

            if (overrideFolder.IsNullOrEmpty() && GetAllFilesOverrideFolders.TryGetValue(searchPattern, out var activeOverrides))
            {
                activeOverrides.Remove(normalizedDefaultFolder);
            }
            else
            {
                if (!GetAllFilesOverrideFolders.TryGetValue(searchPattern, out activeOverrides))
                {
                    GetAllFilesOverrideFolders[searchPattern] = activeOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                activeOverrides[normalizedDefaultFolder] = Utils.NormalizePath(overrideFolder);
            }
        }

        public static bool TryGetAllFilesOverride(string defaultFolder, string searchPattern, out string overrideFolder)
        {
            overrideFolder = null;
            return GetAllFilesOverrideFolders.TryGetValue(searchPattern, out var activeOverrides) &&
                   activeOverrides.TryGetValue(Utils.NormalizePath(Path.GetFullPath(defaultFolder)), out overrideFolder);
        }

        private static void InitHooks()
        {
            lock (_lock)
            {
                if (_hooked) return;
                _hooked = true;
                Harmony.CreateAndPatchAll(typeof(Hooks));
            }
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Illusion.Utils.File), nameof(Illusion.Utils.File.GetAllFiles))]
            private static bool GetAllFilesPrefix(string folder, string searchPattern, ref List<string> files)
            {
                if (!TryGetAllFilesOverride(folder, searchPattern, out var overrideFolder)) return true;
                string[] backupFiles = null;
                try
                {
                    // backup existing entries and restore if something goes wrong
                    backupFiles = files.ToArray();

                    if (BrowserFoldersPlugin.ShowDefaultCards.Value)
                    {
                        var defaultDataSubfolder = ToDefaultDataPath(overrideFolder, true);
                        if (defaultDataSubfolder != null)
                            files.AddRange(Directory.GetFiles(defaultDataSubfolder, searchPattern).Select(Illusion.Utils.File.ConvertPath));
                    }

                    files.AddRange(Directory.GetFiles(overrideFolder, searchPattern).Select(Illusion.Utils.File.ConvertPath));
                    return false;
                }
                catch (Exception err)
                {
                    if (backupFiles != null)
                    {
                        files.Clear();
                        files.AddRange(backupFiles);
                    }
                    UnityEngine.Debug.LogException(err);
                }
                return true;
            }
        }


        public static string ToDefaultDataPath(string userDataPath, bool onlyIfExists)
        {
            if (userDataPath == null) return null;

            userDataPath = Path.GetFullPath(userDataPath);
            var userDataRootPath = Path.GetFullPath(UserData.Path);
            if (userDataPath.StartsWith(userDataRootPath, StringComparison.OrdinalIgnoreCase))
            {
                var defaultDataPath = Path.GetFullPath(global::Studio.DefaultData.Path);
                // Check if the subfolder is inside default data, add the defaul files if it is
                var subfolder = userDataPath.Substring(userDataRootPath.Length);
                var defaultDataSubfolder = Path.Combine(defaultDataPath.TrimEnd('/', '\\'), subfolder.TrimStart('/', '\\'));

                return !onlyIfExists || Directory.Exists(defaultDataSubfolder) ? defaultDataSubfolder : null;
            }

            return null;
        }
    }
}
