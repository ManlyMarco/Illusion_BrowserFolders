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
    public class MakerMapSaveFolders : BaseFolderBrowser
    {
        private static MapSaveScene _mapSaveScene;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerMapSaveFolders() : base("Map folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Enable folder browser for", "Maps", true, "Changes take effect on game restart");
            if (!enable.Value) return false;

            Hooks.Instance = this;
            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _mapSaveScene != null && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_mapSaveScene == null) return;

            _mapSaveScene.CreateList();
            _mapSaveScene.RecreateScrollerList();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            internal static MakerMapSaveFolders Instance;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MapSaveScene), nameof(MapSaveScene.Awake))]
            internal static void InitHook(MapSaveScene __instance)

            {
                var folderTreeView = Instance.TreeView;
                folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "map/data");
                folderTreeView.CurrentFolder = folderTreeView.DefaultPath;

                _mapSaveScene = __instance;

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(MapSaveScene), nameof(MapSaveScene.CreateList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "map/data", StringComparison.OrdinalIgnoreCase))

                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerMapSaveFolders).GetField(nameof(_currentRelativeFolder),
                                                                                   BindingFlags.NonPublic | BindingFlags.Static) ??
                                              throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    }

                    yield return instruction;
                }
            }

            [HarmonyPrefix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(MapInfo), nameof(MapInfo.Save), typeof(string), typeof(bool))]
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