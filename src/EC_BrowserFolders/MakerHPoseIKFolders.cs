using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using HEdit;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks
{
    public class MakerHPoseIKFolders : BaseFolderBrowser
    {
        private static MotionIKUI _motionIKUI;
        private static string _targetScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;

        public MakerHPoseIKFolders() : base("H IK folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            throw new NotImplementedException("this class is not finished");

            var enable = config.Bind("Enable folder browser for", "Scenes", true, "Changes take effect on game restart");
            if (!enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _motionIKUI != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_motionIKUI == null) return;

            // todo this doesnt work
            _motionIKUI.Init();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        //todo no need to modify save dir?
        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(MotionIKUI), "Start")]
            internal static void InitHook(MotionIKUI __instance)
            {
                _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "edit/IK");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _motionIKUI = __instance;
                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(HEditIndividualLoadWindow), "Create")]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "edit/IK", StringComparison.OrdinalIgnoreCase))

                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerHPoseIKFolders).GetField(nameof(_currentRelativeFolder),
                                                                                   BindingFlags.NonPublic | BindingFlags.Static) ??
                                              throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }
        }
    }
}