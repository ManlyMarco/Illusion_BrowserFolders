using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HEdit;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks
{
    public class MakerSceneFolders : IFolderBrowser
    {
        private static HEditLoadSceneWindow _hEditLoadSceneWindow;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerSceneFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
                {
                    CurrentFolderChanged = OnFolderChanged
                };

            Harmony.CreateAndPatchAll(typeof(MakerSceneFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_hEditLoadSceneWindow != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.55f),
                                           (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select scene folder", OnFolderChanged);
            }
        }

        //todo no need to modify save dir?
        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HEditLoadSceneWindow), "Start")]
        internal static void InitHook(HEditLoadSceneWindow __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "edit/scene");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _hEditLoadSceneWindow = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HEditLoadSceneWindow), "Create")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "edit/scene", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(MakerSceneFolders).GetField(nameof(_currentRelativeFolder),
                                              BindingFlags.NonPublic | BindingFlags.Static) ??
                                          throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                }

                yield return instruction;
            }
        }

        internal static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16),
                (int)(Screen.height * 0.4));
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_hEditLoadSceneWindow == null) return;

            _hEditLoadSceneWindow.Create();
            _hEditLoadSceneWindow.CreateListFilter();
        }
    }
}