using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders
{
    public class MakerOutfitFolders : BaseFolderBrowser
    {
        private static CustomCoordinateFile _customCoordinateFile;
        private static Toggle _catToggle;
        private static Toggle _saveOutfitToggle;
        private static Toggle _loadOutfitToggle;
        private static GameObject _saveFront;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerOutfitFolders() : base("Outfit folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enabled = config.Bind("Enable folder browser for", "Outfits", true, "Changes take effect on game restart");
            if (!enabled.Value) return false;

            Hooks.Instance = this;
            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_saveOutfitToggle != null && _saveOutfitToggle.isOn ||
                    _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                {
                    if (_saveFront == null || !_saveFront.activeSelf)
                        return 1;
                }
            }
            return 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return;

            var isLoad = _loadOutfitToggle != null && _loadOutfitToggle.isOn;
            var isSave = _saveOutfitToggle != null && _saveOutfitToggle.isOn;
            if (isLoad || isSave)
            {
                _customCoordinateFile.Initialize();

                // Fix default cards being shown when refreshing in this way
                var lctrlTrav = _customCoordinateFile.listCtrl;
                if (isSave)
                {
                    var lst = lctrlTrav.lstFileInfo;
                    var dis = lctrlTrav.cfWindow.forceHideCategoryNo;
                    if (dis != -1)
                        foreach (var customFileInfo in lst.Where(x => x.category == dis)) customFileInfo.fic.Disvisible(true);
                }
                else
                {
                    lctrlTrav.UpdateCategory();
                }
            }
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f),
                            (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            internal static MakerOutfitFolders Instance;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomCoordinateFile), nameof(CustomCoordinateFile.Start))]
            internal static void InitHook(CustomCoordinateFile __instance)
            {
                var treeView = Instance.TreeView;
                treeView.DefaultPath = Path.Combine(UserData.Path, CustomBase.Instance.chaCtrl.sex != 0 ? "coordinate/female" : "coordinate/male");
                treeView.CurrentFolder = treeView.DefaultPath;

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
                    if (string.Equals(instruction.operand as string, "coordinate/female/", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(instruction.operand as string, "coordinate/male/", StringComparison.OrdinalIgnoreCase))
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
                if (Instance.IsVisible() == 0) return;

                var name = Path.GetFileName(path);

                path = Path.Combine(DirectoryPathModifier(path), name);

                Instance.OnListRefresh();
            }

            private static string DirectoryPathModifier(string currentDirectoryPath)
            {
                var treeView = Instance.TreeView;
                return treeView != null ? treeView.CurrentFolder : currentDirectoryPath;
            }
        }
    }
}