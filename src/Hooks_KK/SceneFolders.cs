using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BrowserFolders.Common;
using Harmony;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    public class SceneFolders : IFolderBrowser
    {
        public BrowserType Type => BrowserType.Scene;

        private static FolderTreeView _folderTreeView;
        private static SceneLoadScene _studioInitObject;

        public SceneFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Path.Combine(Utils.NormalizePath(UserData.Path), @"studio\scene"));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyInstance.Create(GetType().FullName).PatchAll(typeof(SceneFolders));
        }

        public void OnGui()
        {
            if (_studioInitObject != null)
            {
                var screenRect = new Rect((int)(Screen.width / 11.3f), (int)(Screen.height / 90f), (int)(Screen.width / 2.5f), (int)(Screen.height / 5f));
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select folder with scenes to view");
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        public static IEnumerable<CodeInstruction> StudioInitInfoPatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "studio/scene", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(SceneFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        public static void StudioInitInfoPost(SceneLoadScene __instance)
        {
            _studioInitObject = __instance;
            if (_folderTreeView.CurrentFolder == null)
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
            _folderTreeView.ScrollListToSelected();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), new[] { typeof(string) })]
        public static void SavePrefix(ref string _path)
        {
            try
            {
                if (Settings.StudioSaveOverride() && !string.IsNullOrEmpty(_folderTreeView.CurrentFolder))
                {
                    var name = Path.GetFileName(_path);
                    if (!string.IsNullOrEmpty(name) &&
                        // Play nice with other mods if they want to save outside
                        _path.ToLowerInvariant().Replace('\\', '/').Contains("userdata/studio/scene"))
                    {
                        _path = Path.Combine(_folderTreeView.CurrentFolder, name);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static string _currentRelativeFolder;

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_studioInitObject == null) return;

            var sls = typeof(SceneLoadScene);
            sls.GetMethod("OnClickCancel", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, null);
            sls.GetMethod("InitInfo", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, null);
            sls.GetMethod("SetPage", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_studioInitObject, new object[] { SceneLoadScene.page });
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginHorizontal();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
                {
                    if (GUILayout.Button("Refresh scene thumbnails"))
                        OnFolderChanged();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open current folder in explorer"))
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                    if (GUILayout.Button("Open screenshot folder in explorer"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.NormalizePath(UserData.Path), "cap")}\"");
                    if (GUILayout.Button("Open character folder in explorer"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.NormalizePath(UserData.Path), "chara")}\"");
                    if (GUILayout.Button("Open main game folder in explorer"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.NormalizePath(UserData.Path))}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }
}
