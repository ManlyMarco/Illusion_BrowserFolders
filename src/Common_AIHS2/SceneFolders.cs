using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using Studio;
using UnityEngine;

namespace BrowserFolders
{
    public class SceneFolders : BaseFolderBrowser
    {
        private static ConfigEntry<bool> _studioSaveOverride;

        private static FolderTreeView _folderTreeView;
        private static SceneLoadScene _studioInitObject;

        public SceneFolders() : base("Scene folder", BrowserFoldersPlugin.UserDataPath, Path.Combine(BrowserFoldersPlugin.UserDataPath, @"studio\scene")) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            _studioSaveOverride = config.Bind("Chara Studio", "Save scenes to current folder", false, "When you select a custom folder to load a scene from, newly saved scenes will be saved to this folder.\nIf disabled, scenes are always saved to default folder (studio/scene).");
            var enable = config.Bind("Chara Studio", "Enable folder browser in scene browser", true, "Changes take effect on game restart");

            if (!isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));
            return true;
        }

        protected override int IsVisible()
        {
            return _studioInitObject != null ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_studioInitObject != null)
            {
                _studioInitObject.OnClickCancel();
                _studioInitObject.InitInfo();
                _studioInitObject.SetPage(SceneLoadScene.page);
            }
        }

        private static string _currentRelativeFolder;

        protected override void DrawControlButtons()
        {
            base.DrawControlButtons();

            if (GUILayout.Button("Character folder", Utils.LayoutNone))
                Utils.OpenDirInExplorer(Path.Combine(BrowserFoldersPlugin.UserDataPath, "chara"));
        }

        public override Rect GetDefaultRect()
        {
            return new Rect(0, 0, Screen.width * 0.1f, Screen.height);
        }

        private static class Hooks
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.InitInfo))]
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
            [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.InitInfo))]
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
                if (!_studioSaveOverride.Value || string.IsNullOrEmpty(_folderTreeView.CurrentFolder)) return;

                try
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
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }
            }
        }
    }
}
