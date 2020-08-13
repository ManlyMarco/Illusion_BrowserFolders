using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using HEdit;
using KKAPI.Utilities;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.EC
{
    public class OutfitFolders : IFolderBrowser
    {
        private static PartInfoClothSetUI _partInfoClothSetUI;
        private static FolderTreeView _folderTreeView;

        private static GameObject _loadOutfitToggle;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;

        public OutfitFolders()
        {
            //_folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            //_folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Harmony.CreateAndPatchAll(typeof(OutfitFolders));
            //MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);
        }

        public void OnGui()
        {
            // Check the opened category
            if (_partInfoClothSetUI != null && _loadOutfitToggle.activeSelf &&
                _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                    //OnFolderChanged();
                    _refreshList = false;

                var screenRect = new Rect((int) (Screen.width * 0.344), (int) (Screen.height * 0.57f),
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
        [HarmonyPatch(typeof(PartInfoClothSetUI), "Start")]
        [Obsolete]
        internal static void InitHook(PartInfoClothSetUI __instance)
        {
            //_folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "coordinate/female");
            //_folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            //_partInfoClothSetUI = __instance;

            //_loadOutfitToggle = GameObject.Find("UI/CoordinateWindowCanvas/loadCoordinateWindowS");

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PartInfoClothSetUI), "OpenItemCategory")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "path", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    //instruction.opcode = OpCodes.Ldsfld;
                    //instruction.operand = typeof(OutfitFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        private static void OnFolderChanged()
        {
            //_currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            //if (_partInfoClothSetUI == null) return;

            // private bool Initialize()                
            //var cdf = Traverse.Create(_partInfoClothSetUI);
            //cdf.Method("SetNowParameter").GetValue();
            //cdf.Method("OpenItemCategory").GetValue();
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                //_folderTreeView.DrawDirectoryTree();

                //GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
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