using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Manager;
using Pose;
using UnityEngine;

namespace BrowserFolders.Hooks
{
    public class MakerPoseSaveFolders : IFolderBrowser
    {
        private static PoseSaveScene _poseSaveScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerPoseSaveFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
                {
                    CurrentFolderChanged = OnFolderChanged
                };

            Harmony.CreateAndPatchAll(typeof(MakerPoseSaveFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_poseSaveScene != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                                           (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select pose folder", OnFolderChanged);
            }
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PoseSaveScene), "Awake")]
        internal static void InitHook(PoseSaveScene __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "pose/data");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _poseSaveScene = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PoseSaveScene), "CreateList")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "pose/data", StringComparison.OrdinalIgnoreCase))

                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(MakerPoseSaveFolders).GetField(nameof(_currentRelativeFolder),
                                              BindingFlags.NonPublic | BindingFlags.Static) ??
                                          throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                }

                yield return instruction;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PoseInfo), "Save", typeof(string), typeof(bool))]
        internal static void Save(ref string _path)
        {
            var name = Path.GetFileName(_path);

            _path = Path.Combine(DirectoryPathModifier(_path), name);

            _refreshList = true;
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_poseSaveScene == null) return;

            _poseSaveScene.CreateList();
            _poseSaveScene.RecreateScrollerList();
        }
    }
}