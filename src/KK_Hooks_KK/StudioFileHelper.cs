using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrowserFolders.Hooks.KK
{
    public static class StudioFileHelper
    {
        private static bool _hooked = false;
        private static object _lock = new object();


        // GetAllFilesOverrideFolders[searchPattern][defaultFolder] => overrideFolder
        private static readonly Dictionary<string, Dictionary<string, string>> GetAllFilesOverrideFolders =
            new Dictionary<string, Dictionary<string, string>>();

        public static void SetGetAllFilesOverride(string defaultFolder, string searchPattern, string overrideFolder)
        {
            if (!_hooked) InitHooks();

            // keyed by searchPattern, then defaultFolder to avoid any excess calls to NormalizePath

            if (overrideFolder.IsNullOrEmpty() && GetAllFilesOverrideFolders.TryGetValue(searchPattern, out var activeOverrides))
            {
                activeOverrides.Remove(Utils.NormalizePath(defaultFolder));
            }
            else
            {
                if (!GetAllFilesOverrideFolders.TryGetValue(searchPattern, out activeOverrides))
                {
                    GetAllFilesOverrideFolders[searchPattern] = activeOverrides = new Dictionary<string, string>();
                }

                activeOverrides[Utils.NormalizePath(defaultFolder)] = Utils.NormalizePath(overrideFolder);
            }
        }

        public static bool TryGetAllFilesOverride(string defaultFolder, string searchPattern, out string overrideFolder)
        {
            overrideFolder = null;
            return GetAllFilesOverrideFolders.TryGetValue(searchPattern, out var activeOverrides) &&
                   activeOverrides.TryGetValue(Utils.NormalizePath(defaultFolder), out overrideFolder);
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
        

        
    }
}
