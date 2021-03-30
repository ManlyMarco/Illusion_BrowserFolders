using System;
using System.IO;
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
            if (!((KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame)|| (KoikatuAPI.GetCurrentGameMode() == GameMode.Maker))) return true;

            try
            {
                var basePathForChara = new Uri(UserData.Path + "chara/female/");
                var lookedAtPath = new Uri(path);
                if (basePathForChara.IsBaseOf(lookedAtPath))
                {
                    __result = MainGameFolders.GetRelativePath(path, UserData.Path + "chara/female/");
                    return false;
                }

                if (File.Exists(UserData.Path + "chara/female/" + path))
                {
                    __result = path;
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
        [HarmonyPatch(typeof(ChaFileControl), "ConvertCharaFilePath")]
        public static bool ConvertCharaFilePath(ref ChaFileControl __instance, ref string __result, string path)
        {
            // Should only work during main game
            if (!((KoikatuAPI.GetCurrentGameMode() == GameMode.MainGame) || (KoikatuAPI.GetCurrentGameMode() == GameMode.Maker))) return true;
            if (path == null) return true;

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