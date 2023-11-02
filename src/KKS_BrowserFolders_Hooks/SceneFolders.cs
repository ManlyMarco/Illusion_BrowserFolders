using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
{
    [BrowserType(BrowserType.Scene)]
    public class SceneFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        private static SceneLoadScene _studioInitObject;

        public SceneFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Path.Combine(Utils.NormalizePath(UserData.Path), @"studio\scene"))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(SceneFolders));
        }

        private bool _guiActive;

        public void OnGui()
        {
            if (_studioInitObject != null)
            {
                _guiActive = true;
                var screenRect = new Rect(0, 0, Screen.width * 0.1f, Screen.height);
                var orig = GUI.skin;
                GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
                GUILayout.Window(362, screenRect, TreeWindow, "Select folder to view");
                IMGUIUtils.EatInputInRect(screenRect);
                GUI.skin = orig;
            }
            else if (_guiActive)
            {
                _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        internal static IEnumerable<CodeInstruction> StudioInitInfoPatch(IEnumerable<CodeInstruction> instructions)
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
        internal static void StudioInitInfoPost(SceneLoadScene __instance)
        {
            _studioInitObject = __instance;
            if (_folderTreeView.CurrentFolder == null)
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;
            _folderTreeView.ScrollListToSelected();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), typeof(string))]
        internal static void SavePrefix(ref string _path)
        {
            try
            {
                if (KKS_BrowserFolders.StudioSaveOverride.Value && !string.IsNullOrEmpty(_folderTreeView.CurrentFolder))
                {
                    // Compatibility with autosave plugin
                    if (_path.Contains("/_autosave")) return;

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

            _studioInitObject.SafeProc(sls =>
            {
                sls.OnClickCancel();
                sls.InitInfo();
                sls.SetPage(SceneLoadScene.page);
            });
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                if (GUILayout.Button("Refresh scenes"))
                {
                    _folderTreeView.ResetTreeCache();
                    OnFolderChanged();
                }

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Open in explorer:");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Open current folder"))
                    Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                if (GUILayout.Button("Open screenshot folder"))
                    Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                if (GUILayout.Button("Open character folder"))
                    Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "chara"));
                if (GUILayout.Button("Open main game folder"))
                    Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
            }
            GUILayout.EndVertical();
        }
    }
}
