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
    public class MakerPoseFolders : IFolderBrowser
    {
        private static PoseLoadScene _poseLoadScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerPoseFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
                {
                    CurrentFolderChanged = OnFolderChanged
                };

            Harmony.CreateAndPatchAll(typeof(MakerPoseFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_poseLoadScene != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int) (Screen.width * 0.004), (int) (Screen.height * 0.55f),
                                           (int) (Screen.width * 0.125), (int) (Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select pose folder", OnFolderChanged);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PoseLoadScene), "Awake")]
        internal static void InitHook(PoseLoadScene __instance)

        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "pose/data");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _poseLoadScene = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PoseLoadScene), "CreateList")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "pose/data", StringComparison.OrdinalIgnoreCase))

                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(MakerPoseFolders).GetField(nameof(_currentRelativeFolder),
                                              BindingFlags.NonPublic | BindingFlags.Static) ??
                                          throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                }

                yield return instruction;
            }
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_poseLoadScene == null) return;

            _poseLoadScene.CreateList();
            _poseLoadScene.RecreateScrollerList();
        }
    }
}