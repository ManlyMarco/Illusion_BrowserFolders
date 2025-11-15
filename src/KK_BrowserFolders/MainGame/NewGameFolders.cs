using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace BrowserFolders.MainGame
{
    public class NewGameFolders : BaseFolderBrowser
    {
        private static string _currentRelativeFolder;
        private static FolderTreeView _folderTreeView;

        private static EntryPlayer _customCharaFile;
        private static string _targetScene;

        public NewGameFolders() : base("New Game Folders", BrowserFoldersPlugin.UserDataPath, Path.Combine(BrowserFoldersPlugin.UserDataPath, "chara/male/")){}

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            if (isStudio) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));
            return true;
        }

        protected override int IsVisible()
        {
            return ClassroomFolders.EnableClassroom.Value && _customCharaFile != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCharaFile == null) return;

            _customCharaFile.SafeProc(ccf => ccf.CreateMaleList());
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.73), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.2), (int)(Screen.height * 0.3));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(EntryPlayer), nameof(EntryPlayer.Start))]
            internal static void InitHook(EntryPlayer __instance)
            {
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCharaFile = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(EntryPlayer), nameof(EntryPlayer.CreateMaleList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "chara/male/", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(NewGameFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    yield return instruction;
                }
            }
        }
    }
}