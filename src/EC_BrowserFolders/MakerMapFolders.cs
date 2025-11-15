using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using Map;
using UnityEngine;

namespace BrowserFolders
{
    public class MakerMapFolders : BaseFolderBrowser
    {
        private static MapLoadScene _mapLoadScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerMapFolders() : base("Map folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Enable folder browser for", "Maps", true, "Changes take effect on game restart");
            if (!enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _mapLoadScene != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_mapLoadScene == null) return;

            _mapLoadScene.CreateList();
            _mapLoadScene.RecreateScrollerList();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(MapLoadScene), nameof(MapLoadScene.Awake))]
            internal static void InitHook(MapLoadScene __instance)

            {
                _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "map/data");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _mapLoadScene = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(MapLoadScene), nameof(MapLoadScene.CreateList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "map/data", StringComparison.OrdinalIgnoreCase))

                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerMapFolders).GetField(nameof(_currentRelativeFolder),
                                                                               BindingFlags.NonPublic | BindingFlags.Static) ??
                                              throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }
        }
    }
}