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
    public class MakerPoseFolders : BaseFolderBrowser
    {
        private static PoseLoadScene _poseLoadScene;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerPoseFolders() : base("Pose folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

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
            return _poseLoadScene != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_poseLoadScene == null) return;

            _poseLoadScene.CreateList();
            _poseLoadScene.RecreateScrollerList();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            internal static MakerPoseFolders Instance;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PoseLoadScene), nameof(PoseLoadScene.Awake))]
            internal static void InitHook(PoseLoadScene __instance)
            {
                var treeView = Instance.TreeView;
                treeView.DefaultPath = Path.Combine(BrowserFoldersPlugin.UserDataPath, "pose/data");
                treeView.CurrentFolder = treeView.DefaultPath;

                _poseLoadScene = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PoseLoadScene), nameof(PoseLoadScene.CreateList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "pose/data", StringComparison.OrdinalIgnoreCase))

                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerPoseFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static)
                                              ?? throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }
        }
    }
}