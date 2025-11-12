using System;
using System.IO;
using System.Collections.Generic;
using AIChara;
using HarmonyLib;
using KKAPI;

namespace BrowserFolders
{
    internal static class NestedFilenamesMainGamePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Path), "GetFileName")]
        public static bool GetFileNamePatch(ref string __result, string path)
        {
            // Should only work during main game
            var gmode = KoikatuAPI.GetCurrentGameMode();
            if (gmode != GameMode.MainGame && gmode != GameMode.Maker) return true;

            try
            {
                if (File.Exists(UserData.Path + "chara/female/" + path))
                {
                    __result = path;
                    return false;
                }

                var lookedAtPath = new Uri(path);
                var basePathForChara = new Uri(UserData.Path + "chara/female/");
                if (basePathForChara.IsBaseOf(lookedAtPath))
                {
                    __result = MainGameFolders.GetRelativePath(path, UserData.Path + "chara/female/");
                    return false;
                }

                return true;
            }
            catch
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFile), nameof(ChaFile.LoadFile), typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
        private static void ChaFileLoadHook(ChaFile __instance, BinaryReader br)
        {
            if (! (br.BaseStream is FileStream fs))
                return;
            
            var fullPath = Path.GetFullPath(fs.Name);
            _ChaFileFullPathMap[__instance.charaFileName] = fullPath;
        }

        private static readonly Dictionary<string, string> _ChaFileFullPathMap = new Dictionary<string, string>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), "ConvertCharaFilePath")]
        public static bool ConvertCharaFilePath(ref ChaFileControl __instance, ref string __result, string path)
        {
            // Should only work during main game
            var gmode = KoikatuAPI.GetCurrentGameMode();
            if (gmode != GameMode.MainGame && gmode != GameMode.Maker) return true;
            if (path == null) return true;

            // BUG - If there are multiple cards with the same file name (in different subdirs) the last added card will override all other copies
            // more info at https://github.com/ManlyMarco/Illusion_BrowserFolders/pull/52
            if (path == __instance.charaFileName && _ChaFileFullPathMap.TryGetValue(path, out string fullPath))
            {
                __result = fullPath;
                return false;
            }

            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                path += ".png";

            if (File.Exists(path))
            {
                __result = path;
                return false;
            }

            if (File.Exists(UserData.Path + "chara/female/" + path))
            {
                __result = UserData.Path + "chara/female/" + path;
                return false;
            }

            return true;
        }
    }
}
