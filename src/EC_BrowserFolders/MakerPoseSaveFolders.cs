using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using Pose;
using UnityEngine;

namespace BrowserFolders
{
    public class MakerPoseSaveFolders : BaseFolderBrowser
    {
        private static PoseSaveScene _poseSaveScene;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerPoseSaveFolders() : base("Pose folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Enable folder browser for", "Poses", true, "Changes take effect on game restart");
            if (!enable.Value) return false;

            Hooks.Instance = this;
            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _poseSaveScene != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_poseSaveScene == null) return;

            _poseSaveScene.CreateList();
            _poseSaveScene.RecreateScrollerList();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            internal static MakerPoseSaveFolders Instance;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PoseSaveScene), nameof(PoseSaveScene.Awake))]
            internal static void InitHook(PoseSaveScene __instance)
            {
                var treeView = Instance.TreeView;
                treeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "pose/data");
                treeView.CurrentFolder = treeView.DefaultPath;

                _poseSaveScene = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PoseSaveScene), nameof(PoseSaveScene.CreateList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "pose/data", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerPoseSaveFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static)
                                              ?? throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }

            [HarmonyPrefix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(PoseInfo), nameof(PoseInfo.Save), typeof(string), typeof(bool))]
            internal static void Save(ref string _path)
            {
                var name = Path.GetFileName(_path);

                _path = Path.Combine(DirectoryPathModifier(_path), name);

                Instance.OnListRefresh();
            }

            private static string DirectoryPathModifier(string currentDirectoryPath)
            {
                var treeView = Instance.TreeView;
                return treeView != null ? treeView.CurrentFolder : currentDirectoryPath;
            }
        }
    }
}