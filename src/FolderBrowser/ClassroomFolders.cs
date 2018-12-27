using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using Harmony;
using UnityEngine;

namespace BrowserFolders
{
    public class ClassroomFolders
    {
        private static string _currentRelativeFolder;
        private static FolderTreeView _folderTreeView;

        private static ClassRoomCharaFile _customCharaFile;
        private static Canvas _canvas;

        public ClassroomFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.GetUserDataPath(), Path.Combine(Utils.GetUserDataPath(), "chara/female/"));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyInstance.Create(KK_BrowserFolders.Guid + "." + nameof(ClassroomFolders)).PatchAll(typeof(ClassroomFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClassRoomCharaFile), "Start")]
        public static void InitHook(ClassRoomCharaFile __instance)
        {
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;
            _canvas = __instance.transform.GetComponentInParent<Canvas>();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ClassRoomCharaFile), "InitializeList")]
        public static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
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
            if (_canvas != null && _canvas.enabled)
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
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                    if (GUILayout.Button("Screenshot folder"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.GetUserDataPath(), "cap")}\"");
                    if (GUILayout.Button("Main game folder"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.GetUserDataPath())}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}
