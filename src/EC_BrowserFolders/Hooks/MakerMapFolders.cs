using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Manager;
using Map;
using UnityEngine;

namespace BrowserFolders.Hooks
{
    public class MakerMapFolders : IFolderBrowser
    {
        private static MapLoadScene _mapLoadScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerMapFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
                {
                    CurrentFolderChanged = OnFolderChanged
                };

            Harmony.CreateAndPatchAll(typeof(MakerMapFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_mapLoadScene != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                                           (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select map folder", OnFolderChanged);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapLoadScene), "Awake")]
        internal static void InitHook(MapLoadScene __instance)

        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "map/data");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _mapLoadScene = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapLoadScene), "CreateList")]
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

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_mapLoadScene == null) return;

            _mapLoadScene.CreateList();
            _mapLoadScene.RecreateScrollerList();
        }
    }
}