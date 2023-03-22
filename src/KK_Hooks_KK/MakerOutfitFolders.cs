using ChaCustom;
using HarmonyLib;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.MakerOutfit)]
    public class MakerOutfitFolders : IFolderBrowser
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

        public MakerOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(MakerOutfitFolders));
            //MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);

        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "Start")]
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
        [HarmonyPatch(typeof(CustomCoordinateFile), "Initialize")]
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
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch(ref string path)
        {
            var name = Path.GetFileName(path);
            path = Path.Combine(DirectoryPathModifier(path), name);

            _refreshList = true;
        }

        private bool _guiActive;
        private Rect _windowRect;

        public void OnGui()
        {
            var guiShown = false;
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_saveOutfitToggle != null && _saveOutfitToggle.isOn || _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                    {
                        if (_refreshList)
                        {
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        if (_windowRect.IsEmpty())
                            _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                        InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select outfit folder", OnFolderChanged);

                        _guiActive = guiShown = true;
                    }
                }
            }

            if (!guiShown && _guiActive)
            {
                _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
            }
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return;

            if (_saveOutfitToggle != null && _saveOutfitToggle.isOn || _loadOutfitToggle != null && _loadOutfitToggle.isOn)
            {
                _customCoordinateFile.Initialize();
            }
        }
    }
}
