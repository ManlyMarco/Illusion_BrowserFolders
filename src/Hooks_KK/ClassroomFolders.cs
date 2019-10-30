using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using BepInEx.Harmony;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    public class ClassroomFolders : IFolderBrowser
    {
        public BrowserType Type => BrowserType.Classroom;

        private static string _currentRelativeFolder;
        private static FolderTreeView _folderTreeView;

        private static ClassRoomCharaFile _customCharaFile;
        private static Canvas _canvas;
        private static string _targetScene;

        public ClassroomFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Path.Combine(Utils.NormalizePath(UserData.Path), "chara/female/"));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyWrapper.PatchAll(typeof(ClassroomFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
        internal static void InitHook(ClassRoomCharaFile __instance)
        {
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;
            _canvas = __instance.transform.GetComponentInParent<Canvas>();

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "chara/female/", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(ClassroomFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        public void OnGui()
        {
            if (_canvas != null && _canvas.enabled && _targetScene == Scene.Instance.AddSceneName)
            {
                var screenRect = GetFullscreenBrowserRect();
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            }
        }

        internal static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCharaFile == null) return;

            _customCharaFile.InitializeList();
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh thumbnails"))
                        OnFolderChanged();

                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                    if (GUILayout.Button("Screenshot folder"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    if (GUILayout.Button("Main game folder"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}
