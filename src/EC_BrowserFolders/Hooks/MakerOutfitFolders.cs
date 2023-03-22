using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks
{
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
        private Rect _windowRect;

        public MakerOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(MakerOutfitFolders));
        }

        private static bool IsVisible()
        {
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_saveOutfitToggle != null && _saveOutfitToggle.isOn ||
                    _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                {
                    if (_saveFront == null || !_saveFront.activeSelf)
                        return true;
                }
            }
            return false;
        }

        public void OnGui()
        {
            if (!IsVisible()) return;

            if (_refreshList)
            {
                OnFolderChanged();
                _refreshList = false;
            }

            if (_windowRect.IsEmpty())
                _windowRect =new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f),
                                      (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

            InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select outfit folder", OnFolderChanged);
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "Start")]
        internal static void InitHook(CustomCoordinateFile __instance)
        {
            var instance = CustomBase.Instance;
            _folderTreeView.DefaultPath = Path.Combine(UserData.Path,
                instance.chaCtrl.sex != 0 ? @"coordinate/female" : "coordinate/male");
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
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch(ref string path)
        {
            if (!IsVisible()) return;

            var name = Path.GetFileName(path);

            path = Path.Combine(DirectoryPathModifier(path), name);

            _refreshList = true;
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

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
        }
}