using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    // Handles both h scene outfits and dance mode
    [BrowserType(BrowserType.HOutfit)]
    public class HOutfitFolders : IFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static GameObject _uiObject;
        private static string _sceneName;

        private static string _currentRelativeFolder;

        public HOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(HOutfitFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(clothesFileControl), "Start")]
        internal static void InitHook(clothesFileControl __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;
            _uiObject = __instance.gameObject;
            _sceneName = Manager.Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(clothesFileControl), "Initialize")]
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

        private bool _guiActive;
        private Rect _windowRect;

        public void OnGui()
        {
            if (_uiObject && _uiObject.activeSelf && _sceneName == Manager.Scene.Instance.AddSceneName)
            {
                _guiActive = true;

                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int)(Screen.width * 0.04), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select outfit folder", OnFolderChanged);
            }
            else if (_guiActive)
            {
                _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
            }
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return; //if failed not initializing in "start"

            _customCoordinateFile.Initialize();
        }
    }
}
