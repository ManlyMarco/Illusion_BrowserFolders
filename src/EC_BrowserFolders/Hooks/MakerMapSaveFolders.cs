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
    public class MakerMapSaveFolders : IFolderBrowser
    {
        private static MapSaveScene _mapSaveScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerMapSaveFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
                {
                    CurrentFolderChanged = OnFolderChanged
                };

            Harmony.CreateAndPatchAll(typeof(MakerMapSaveFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_mapSaveScene != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int) (Screen.width * 0.004), (int) (Screen.height * 0.55f),
                                           (int) (Screen.width * 0.125), (int) (Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select map folder", OnFolderChanged);
            }
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapSaveScene), "Awake")]
        internal static void InitHook(MapSaveScene __instance)

        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "map/data");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _mapSaveScene = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapSaveScene), "CreateList")]
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
        [HarmonyPatch(typeof(MapInfo), "Save", typeof(string), typeof(bool))]
        internal static void Save(ref string _path)
        {
            var name = Path.GetFileName(_path);

            _path = Path.Combine(DirectoryPathModifier(_path), name);

            _refreshList = true;
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_mapSaveScene == null) return;

            _mapSaveScene.CreateList();
            _mapSaveScene.RecreateScrollerList();
        }
    }
}