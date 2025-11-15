using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    // Handles both h scene outfits and dance mode
    public class HOutfitFolders : BaseFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static GameObject _uiObject;
        private static string _sceneName;

        private static string _currentRelativeFolder;

        public HOutfitFolders() : base("Outfit folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in H preset browser", true, "Changes take effect on game restart.\n Kplug doesn't support this and will restore previous outfit when not main or out of H.");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return _uiObject && _uiObject.activeSelf && _sceneName == Manager.Scene.Instance.AddSceneName ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile != null)
                _customCoordinateFile.Initialize();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.04), (int)(Screen.height * 0.57f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(clothesFileControl), nameof(clothesFileControl.Start))]
            internal static void InitHook(clothesFileControl __instance)
            {
                _folderTreeView.DefaultPath = Path.Combine(UserData.Path, "coordinate/");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCoordinateFile = __instance;
                _uiObject = __instance.gameObject;
                _sceneName = Manager.Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(clothesFileControl), nameof(clothesFileControl.Initialize))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "coordinate/", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(HOutfitFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    yield return instruction;
                }
            }
        }
    }
}
