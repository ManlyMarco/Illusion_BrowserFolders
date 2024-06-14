using System;
using System.IO;
using System.Collections.Generic;
using AIChara;
using HarmonyLib;
using KKAPI;
using KKAPI.Chara;

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
            chaFileFullPathMap[__instance.charaFileName] = fullPath;
        }

        static Dictionary<string, string> chaFileFullPathMap = new Dictionary<string, string>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), "ConvertCharaFilePath")]
        public static bool ConvertCharaFilePath(ref ChaFileControl __instance, ref string __result, string path)
        {
            // Should only work during main game
            var gmode = KoikatuAPI.GetCurrentGameMode();
            if (gmode != GameMode.MainGame && gmode != GameMode.Maker) return true;
            if (path == null) return true;

            if (path == __instance.charaFileName && chaFileFullPathMap.TryGetValue(path, out string fullPath))
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
