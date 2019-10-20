using System.IO;
using ActionGame;
using BepInEx.Harmony;
using BrowserFolders.Common;
using ChaCustom;
using FreeH;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    internal static class Overlord
    {
        private static bool _wasInit;
        public static void Init()
        {
            if (_wasInit) return;
            _wasInit = true;

            HarmonyWrapper.PatchAll(typeof(Overlord));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Localize.Translate.Manager.DefaultData), nameof(Localize.Translate.Manager.DefaultData.UserDataAssist), new[] { typeof(string), typeof(bool) })]
        internal static void FilenameHook(ref string path, ref bool useDefaultData)
        {
            var sex = path == "chara/female/" ? 1 : 0;

            useDefaultData = useDefaultData && KK_BrowserFolders.ShowDefaultCharas.Value;

            var freeh = GameObject.FindObjectOfType<FreeHPreviewCharaList>();
            if (freeh != null)
            {
                FreeHFolders.Init(freeh, sex);

                var overridePath = FreeHFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            var newGame = GameObject.FindObjectOfType<EntryPlayer>();
            if (newGame != null)
            {
                NewGameFolders.Init(newGame, sex);

                var overridePath = NewGameFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            var classroom = GameObject.FindObjectOfType<PreviewCharaList>();
            if (classroom != null)
            {
                ClassroomFolders.Init(classroom, sex);

                var overridePath = ClassroomFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
            }

            var maker = GameObject.FindObjectOfType<CustomCharaFile>();
            if (maker != null)
            {
                //MakerFolders.Init(classroom, sex);

                var overridePath = MakerFolders.CurrentRelativeFolder;
                if (!string.IsNullOrEmpty(overridePath))
                    path = overridePath;

                return;
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
            if(GUI.changed)
            {
                KK_BrowserFolders.ShowDefaultCharas.Value = newVal;
                return true;
            }
            return false;
        }
    }
}