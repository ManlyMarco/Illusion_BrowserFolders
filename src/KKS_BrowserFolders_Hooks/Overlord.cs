using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using ActionGame;
using ChaCustom;
using FreeH;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrowserFolders.Hooks.KKS
{
    internal static partial class Overlord
    {
        private static bool _wasInit;
        public static void Init()
        {
            if (_wasInit) return;
            _wasInit = true;

            var hi = Harmony.CreateAndPatchAll(typeof(Overlord));

            if (!KKAPI.Studio.StudioAPI.InsideStudio)
                ImportHooks.ApplyHooks(hi);
        }

        private static class ImportHooks
        {
            public static void ApplyHooks(Harmony hi)
            {
                var target = AccessTools.Method(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.GetMoveFile)) ?? throw new ArgumentException(nameof(ConvertChaFileScene.GetMoveFile));
                hi.Patch(target.MakeGenericMethod(typeof(ConvertChaFileScene.PackData)),
                    transpiler: new HarmonyMethod(typeof(ImportHooks), nameof(ImportHooks.ImportCardsBackupSubdirsTpl)));

                hi.Patch(AccessTools.Method(typeof(ConvertChaFileScene), nameof(ConvertChaFileScene.Start)),
                    prefix: new HarmonyMethod(typeof(ImportHooks), nameof(ImportHooks.ConvertStartHookPre)));
            }

            private static IEnumerable<CodeInstruction> ImportCardsBackupSubdirsTpl(IEnumerable<CodeInstruction> instructions)
            {
                return new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Path), nameof(Path.GetFileNameWithoutExtension)) ?? throw new Exception("method1 not found")))
                    .ThrowIfInvalid("call not found")
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                    .ThrowIfNotMatch("not a call", new CodeMatch(OpCodes.Call))
                    .SetOperandAndAdvance(AccessTools.Method(typeof(ImportHooks), nameof(ImportHooks.GetSubdirFilenameForImport)) ?? throw new Exception("method2 not found"))
                    .Instructions();
            }

            /// <summary>
            /// Takes the card relate directory inside userdata and transplants it into the out directory path that the game uses for backing up cards being imported
            /// Relative path inside the out directory is returned (can be userdata\out\female or male so if card is in userdata\out\female\kek\card.png this will return kek\card)
            /// </summary>
            private static string GetSubdirFilenameForImport(string cardPath, string targetDir)
            {
                var cardName = Path.GetFileNameWithoutExtension(cardPath);
                var origTargetDir = targetDir;
                if (origTargetDir.StartsWith(UserData.Path))
                {
                    targetDir = Path.GetFullPath(targetDir);
                    var oldIndex = targetDir.IndexOf(Path.DirectorySeparatorChar + "old" + Path.DirectorySeparatorChar, Path.GetFullPath(UserData.Path).Length, StringComparison.Ordinal);
                    if (oldIndex > 0)
                    {
                        cardPath = Path.GetFullPath(cardPath);

                        targetDir = targetDir.Remove(oldIndex, "old/".Length);

                        if (cardPath.StartsWith(targetDir))
                        {
                            // I hate working with paths, need that trim in case there ends up being a / at the start because Path.Combine will think it's a unix root
                            var relativeCardDir = Path.GetDirectoryName(cardPath).Substring(targetDir.Length - 1).Trim('/', '\\');

                            // Create old directories to copy stuff into later, because game doesn't create subdirs
                            var fullCardDir = Path.Combine(origTargetDir, relativeCardDir).TrimEnd('/', '\\');
                            Utils.Logger.LogDebug("Setting import backup directory to " + fullCardDir + "/" + cardName + ".png");
                            Directory.CreateDirectory(fullCardDir);

                            return Path.Combine(relativeCardDir, cardName);
                        }
                    }
                }

                Utils.Logger.LogWarning($"Unknown target dir passed to GetMoveFile or card path is outside of UserData! targetDir={targetDir}   cardPath={cardPath}");
                return cardName;
            }

            private static void ConvertStartHookPre(ConvertChaFileScene __instance)
            {
                var sexList = __instance.convKoikatsuCha;
                for (int i = 0; i < sexList.Length; i++)
                {
                    var dir = "UserData/chara/" + (i == 1 ? "female" : "male");
                    GetDirs(dir, __instance, i);
                }
            }

            private static void GetDirs(string dir, ConvertChaFileScene instance, int sex)
            {
                dir = dir.Replace('\\', '/');
                if (!dir.Equals("UserData/chara/female/_autosave") && !dir.Equals("UserData/chara/male/_autosave"))
                {
                    foreach (var folder in Directory.GetDirectories(dir))
                        GetDirs(folder, instance, sex);

                    if (!dir.Equals("UserData/chara/female") && !dir.Equals("UserData/chara/male"))
                        AddFilesToConvList(dir, instance, sex);
                }
            }

            private static void AddFilesToConvList(string dir, ConvertChaFileScene instance, int sex)
            {
                foreach (var file in Directory.GetFiles(dir))
                {
                    using (var reader = File.Open(file, FileMode.Open))
                    {
                        var charFile = new ChaFile();
                        if (charFile.LoadFileKoikatsu(new BinaryReader(reader), false, true))
                            instance.convKoikatsuCha[sex].Add(new ConvertChaFileScene.PackData(file, 1)); //Mode must be 1
                    }
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