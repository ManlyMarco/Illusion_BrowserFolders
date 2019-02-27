using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using Harmony;
using Manager;
using UnityEngine;

namespace BrowserFolders
{
    public class NewGameFolders
    {
        private static string _currentRelativeFolder;
        private static FolderTreeView _folderTreeView;

        private static EntryPlayer _customCharaFile;
        //private static Canvas _canvas;
        private static string _targetScene;

        public NewGameFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.GetUserDataPath(), Path.Combine(Utils.GetUserDataPath(), "chara/male/"));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyInstance.Create(KK_BrowserFolders.Guid + "." + nameof(NewGameFolders)).PatchAll(typeof(NewGameFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntryPlayer), "Start")]
        public static void InitHook(EntryPlayer __instance)
        {
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;
            //_canvas = __instance.transform.GetComponentInParent<Canvas>();

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EntryPlayer), "CreateMaleList")]
        public static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
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

        public void OnGui()
        {
            if (_customCharaFile != null && _targetScene == Scene.Instance.AddSceneName)
            {
                var screenRect = GetFullscreenBrowserRect();
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
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

            AccessTools.Method(typeof(EntryPlayer), "CreateMaleList").Invoke(_customCharaFile, null);
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
                    
                    if (GUILayout.Button("Open current folder in explorer"))
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}