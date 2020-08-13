using System;
using System.IO;
using ActionGame;
using BepInEx.Harmony;
using ChaCustom;
using FreeH;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrowserFolders.Hooks.KKP
{
    internal static class Overlord
    {
        private static bool _wasInit;
        public static void Init()
        {
            if (_wasInit) return;
            _wasInit = true;

            Harmony.CreateAndPatchAll(typeof(Overlord));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Localize.Translate.Manager.DefaultData), nameof(Localize.Translate.Manager.DefaultData.UserDataAssist), typeof(string), typeof(bool))]
        internal static void FilenameHook(ref string path, ref bool useDefaultData)
        {
            var sex = path == "chara/female/" ? 1 : 0;

            useDefaultData = useDefaultData && KK_BrowserFolders.ShowDefaultCharas.Value;

            var freeh = Object.FindObjectOfType<FreeHPreviewCharaList>();
            if (freeh != null)
            {
                FreeHFolders.Init(freeh, sex);

                var overridePath = FreeHFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            var newGame = Object.FindObjectOfType<EntryPlayer>();
            if (newGame != null)
            {
                NewGameFolders.Init(newGame, sex);

                var overridePath = NewGameFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            var classroom = Object.FindObjectOfType<PreviewCharaList>();
            if (classroom != null)
            {
                ClassroomFolders.Init(classroom, sex);

                var overridePath = ClassroomFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            // Prevents breaking the kkp coordinate list in maker
            var isCoord = path.TrimEnd('\\', '/').EndsWith("coordinate", StringComparison.OrdinalIgnoreCase);
            if (!isCoord)
            {
                var maker = Object.FindObjectOfType<CustomCharaFile>();
                if (maker != null)
                {
                    var overridePath = MakerFolders.CurrentRelativeFolder;
                    if (!string.IsNullOrEmpty(overridePath))
                        path = overridePath;
                }
            }
            else
            {
                var makerOutfit = Object.FindObjectOfType<CustomCoordinateFile>();
                if (makerOutfit != null)
                {
                    var overridePath = MakerOutfitFolders.CurrentRelativeFolder;
                    if (!string.IsNullOrEmpty(overridePath))
                        path = overridePath;
                }
            }
        }

        public static string GetDefaultPath(int sex)
        {
            return Path.Combine(Utils.NormalizePath(UserData.Path), sex == 0 ? "chara/male" : @"chara/female");
        }

        public static string GetUserDataRootPath()
        {
            return Utils.NormalizePath(UserData.Path);
        }

        public static bool DrawDefaultCardsToggle()
        {
            GUI.changed = false;
            var newVal = GUILayout.Toggle(KK_BrowserFolders.ShowDefaultCharas.Value, "Show default cards");
            if (GUI.changed)
            {
                KK_BrowserFolders.ShowDefaultCharas.Value = newVal;
                return true;
            }
            return false;
        }
    }
}