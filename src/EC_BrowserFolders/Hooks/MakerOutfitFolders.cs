using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using HEdit;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.EC
{
    public class MakerOutfitFolders : IFolderBrowser
    {
        private static Toggle _catToggle;
        private static CustomCoordinateFile _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _saveOutfitToggle;
        private static Toggle _loadOutfitToggle;
        private static GameObject _saveFront;
        private static ChaFileCoordinate _ChaFileCoordinate;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;

        public MakerOutfitFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Harmony.CreateAndPatchAll(typeof(MakerOutfitFolders));
            //MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);
        }

        public void OnGui()
        {
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
                // Check opened tab
                if (_saveOutfitToggle != null && _saveOutfitToggle.isOn ||
                    _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                    {
                        if (_refreshList)
                        {
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        var screenRect = new Rect((int) (Screen.width * 0.004), (int) (Screen.height * 0.57f),
                            (int) (Screen.width * 0.125), (int) (Screen.height * 0.35));
                        IMGUIUtils.DrawSolidBox(screenRect);
                        GUILayout.Window(362, screenRect, TreeWindow, "Select outfit folder");
                        IMGUIUtils.EatInputInRect(screenRect);
                    }
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "Start")]
        internal static void InitHook(CustomCoordinateFile __instance)
        {
            Traverse.Create("ConvertChaFileScene").Method("Start").GetValue();
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
        [HarmonyPatch(typeof(HEditIndividualLoadWindow), "Create")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldelem_Ref)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldsfld,
                        typeof(MakerHPoseIKFolders).GetField(nameof(_currentRelativeFolder),
                            BindingFlags.NonPublic | BindingFlags.Static));
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch1(ChaFileCoordinate _ChaFileCoordinate)
        {
            Traverse.Create("ConvertChaFileScene").Method("Convert").GetValue();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch(ref string path)
        {
            var name = Path.GetFileName(path);

            path = Path.Combine(DirectoryPathModifier(path), name);

            _refreshList = true;
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return;

            if (_saveOutfitToggle != null && _saveOutfitToggle.isOn ||
                _loadOutfitToggle != null && _loadOutfitToggle.isOn)
                // private bool Initialize()                
                Traverse.Create(_customCoordinateFile).Method("Initialize").GetValue();
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh thumbnails"))
                        OnFolderChanged();

                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Utils.OpenDirInExplorer(_folderTreeView.CurrentFolder);
                    if (GUILayout.Button("Screenshot folder"))
                        Utils.OpenDirInExplorer(Path.Combine(Utils.NormalizePath(UserData.Path), "cap"));
                    if (GUILayout.Button("Main game folder"))
                        Utils.OpenDirInExplorer(Path.GetDirectoryName(Utils.NormalizePath(UserData.Path)));
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}