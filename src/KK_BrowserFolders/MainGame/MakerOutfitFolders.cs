using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KK
{
    public class MakerOutfitFolders : BaseFolderBrowser
    {
        private static Toggle _catToggle;
        private static CustomCoordinateFile _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _saveOutfitToggle;
        private static Toggle _loadOutfitToggle;
        private static GameObject _saveFront;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;

        public MakerOutfitFolders() : base("Outfit folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in maker for outfits", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        public override void Update()
        {
            base.Update();

            if (_refreshList && GuiVisible != 0)
            {
                _folderTreeView.ResetTreeCache();
                OnListRefresh();
                _refreshList = false;
            }
        }

        protected override int IsVisible()
        {
            // Check UI visibility and the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_saveOutfitToggle != null && _saveOutfitToggle.isOn || _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                        return 1;
                }
            }

            return 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return;

            if (_saveOutfitToggle != null && _saveOutfitToggle.isOn || _loadOutfitToggle != null && _loadOutfitToggle.isOn)
            {
                _customCoordinateFile.Initialize();
            }
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomCoordinateFile), nameof(CustomCoordinateFile.Start))]
            internal static void InitHook(CustomCoordinateFile __instance)
            {
                _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCoordinateFile = __instance;

                var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
                _loadOutfitToggle = gt.transform.Find("tglLoadCos").GetComponent<Toggle>();
                _saveOutfitToggle = gt.transform.Find("tglSaveCos").GetComponent<Toggle>();

                var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
                _catToggle = mt.GetComponent<Toggle>();

                _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomCoordinateFile), nameof(CustomCoordinateFile.Initialize))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "coordinate/", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerOutfitFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    yield return instruction;
                }
            }

            [HarmonyPrefix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile))]
            internal static void SaveFilePatch(ref string path)
            {
                var name = Path.GetFileName(path);
                path = Path.Combine(DirectoryPathModifier(path), name);

                _refreshList = true;
            }
        }
    }
}
