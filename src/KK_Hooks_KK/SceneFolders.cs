using HarmonyLib;
using KKAPI.Utilities;
using Studio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
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
                var screenRect = new Rect((int)(Screen.width / 11.3f), (int)(Screen.height / 90f), (int)(Screen.width / 2.5f), (int)(Screen.height / 5f));
                var orig = GUI.skin;
                GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
                GUILayout.Window(362, screenRect, TreeWindow, "Select folder with scenes to view");
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
                if (KK_BrowserFolders.StudioSaveOverride.Value && !string.IsNullOrEmpty(_folderTreeView.CurrentFolder))
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
            GUILayout.BeginHorizontal();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
                {
                    if (GUILayout.Button("Refresh scene thumbnails"))
                    {
                        _folderTreeView?.ResetTreeCache();
                        OnFolderChanged();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open current folder in explorer"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                    if (GUILayout.Button("Open screenshot folder in explorer"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    if (GUILayout.Button("Open character folder in explorer"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "chara"));
                    if (GUILayout.Button("Open main game folder in explorer"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }
}
