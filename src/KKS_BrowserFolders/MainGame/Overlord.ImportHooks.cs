using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using HarmonyLib;

namespace BrowserFolders.Hooks.KKS
{
    internal static partial class Overlord
    {
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
                    if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
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
        }
    }
}