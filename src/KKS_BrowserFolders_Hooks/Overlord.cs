using System;
using System.IO;
using ActionGame;
using ChaCustom;
using FreeH;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrowserFolders.Hooks.KKS
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

        //In case of duplicate names in different folders
        [HarmonyPrefix]
        [HarmonyPatch(typeof(File), nameof(File.Move), typeof(string), typeof(string))]
        internal static void MoveHook(string sourceFileName, string destFileName)
        {
            if(File.Exists(destFileName))
                File.Delete(destFileName);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConvertChaFileScene), "Start")]
        internal static void ConvertStartHookPre(ref ConvertChaFileScene __instance)
        {
            var sexList = __instance.convKoikatsuCha;
            for (int i = 0; i < sexList.Length; i++)
            {
                string dir = "UserData/chara";
                if (i == 1)
                    dir = Path.Combine(dir, "female");
                else
                    dir = Path.Combine(dir, "male");

                GetDirs(dir, ref __instance, i);
            }

        }

        private static void GetDirs(string dir, ref ConvertChaFileScene instance, int sex)
        {
            if (!dir.Equals("UserData/chara\\female\\_autosave") && !dir.Equals("UserData/chara\\male\\_autosave"))
            {
                foreach (var folder in Directory.GetDirectories(dir))
                {
                    GetDirs(folder, ref instance, sex);
                }

                if(!dir.Equals("UserData/chara\\female") && !dir.Equals("UserData/chara\\male"))
                    AddFilesToConvList(dir, ref instance, sex);
            }
        }

        private static void AddFilesToConvList(string dir, ref ConvertChaFileScene instance, int sex)
        {
            foreach (var file in Directory.GetFiles(dir))
            {
                using (var reader = File.Open(file, FileMode.Open))
                {
                    var charFile = new ChaFile();
                    if(charFile.LoadFileKoikatsu(new BinaryReader(reader), false, true))
                        instance.convKoikatsuCha[sex].Add(new ConvertChaFileScene.PackData(file, 1)); //Mode must be 1
                }
            }
        }


        //FreeH caches male and female causing window to be stuck on the most last opened sex. Forcing recycle to be false fixes this
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FreeHCharaSelect), nameof(FreeHCharaSelect.Create), typeof(int), typeof(bool))]
        internal static void CreateHook(int sex, ref bool recycle)
        {
            recycle = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Localize.Translate.Manager.DefaultData), nameof(Localize.Translate.Manager.DefaultData.UserDataAssist), typeof(string), typeof(bool))]
        internal static void FilenameHook(ref string path, ref bool useDefaultData)
        {
            var sex = path == "chara/female/" ? 1 : 0;

            useDefaultData = useDefaultData && KKS_BrowserFolders.ShowDefaultCharas.Value;

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
                var HOutfit = Object.FindObjectOfType<clothesFileControl>();
                if (HOutfit != null)
                {
                    var overridePath = HOutfitFolders.CurrentRelativeFolder;
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
            var newVal = GUILayout.Toggle(KKS_BrowserFolders.ShowDefaultCharas.Value, "Show default cards");
            if (GUI.changed)
            {
                KKS_BrowserFolders.ShowDefaultCharas.Value = newVal;
                return true;
            }
            return false;
        }
    }
}