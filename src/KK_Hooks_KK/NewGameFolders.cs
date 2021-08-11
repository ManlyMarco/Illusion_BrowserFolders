using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.NewGame)]
    public class NewGameFolders : IFolderBrowser
    {
        private static string _currentRelativeFolder;
        private static FolderTreeView _folderTreeView;

        private static EntryPlayer _customCharaFile;
        private static string _targetScene;

        public NewGameFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Path.Combine(Utils.NormalizePath(UserData.Path), "chara/male/"))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(NewGameFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntryPlayer), "Start")]
        internal static void InitHook(EntryPlayer __instance)
        {
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EntryPlayer), "CreateMaleList")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "chara/male/", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(NewGameFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        private bool _guiActive;

        public void OnGui()
        {
            if (_customCharaFile != null && _targetScene == Scene.Instance.AddSceneName)
            {
                _guiActive = true;
                var screenRect = GetFullscreenBrowserRect();
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                IMGUIUtils.EatInputInRect(screenRect);
            }
            else if (_guiActive)
            {
                _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
            }
        }

        internal static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.73), (int)(Screen.height * 0.55f), (int)(Screen.width * 0.2), (int)(Screen.height * 0.3));
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCharaFile == null) return;

            _customCharaFile.SafeProc(ccf => ccf.CreateMaleList());
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh thumbnails"))
                    {
                        _folderTreeView.ResetTreeCache();
                        OnFolderChanged();
                    }

                    if (GUILayout.Button("Open current folder in explorer"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}