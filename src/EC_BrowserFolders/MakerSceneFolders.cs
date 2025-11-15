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

namespace BrowserFolders
{
    public class MakerSceneFolders : BaseFolderBrowser
    {
        private static HEditLoadSceneWindow _hEditLoadSceneWindow;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerSceneFolders() : base("Scene folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enabled = config.Bind("Enable folder browser for", "Scenes", true, "Changes take effect on game restart");
            if (!enabled.Value) return false;

            Hooks.Instance = this;
            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _hEditLoadSceneWindow != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_hEditLoadSceneWindow == null) return;

            _hEditLoadSceneWindow.Create();
            _hEditLoadSceneWindow.CreateListFilter();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            internal static MakerSceneFolders Instance;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HEditLoadSceneWindow), nameof(HEditLoadSceneWindow.Start))]
            internal static void InitHook(HEditLoadSceneWindow __instance)
            {
                var treeView = Instance.TreeView;
                treeView.DefaultPath = Path.Combine(BrowserFoldersPlugin.UserDataPath, "edit/scene");
                treeView.CurrentFolder = treeView.DefaultPath;

                _hEditLoadSceneWindow = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(HEditLoadSceneWindow), nameof(HEditLoadSceneWindow.Create))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "edit/scene", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerSceneFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static)
                                              ?? throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }
        }
    }
}