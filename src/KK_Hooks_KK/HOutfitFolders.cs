using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.HOutfit)]
    public class HOutfitFolders : IFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _hToggle;//doesn't initialize to true at start or at least in "public HOutfitFolders()" as true as it crashes the game on startup

        public HOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;
            
            Harmony.CreateAndPatchAll(typeof(HOutfitFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(clothesFileControl), "Start")]
        internal static void InitHook(clothesFileControl __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;

            _hToggle = true; //weirdly enough required so file system would open the first time you use the preset per encounter
            GameObject.Find("Canvas/SubMenu/DressCategory/ClothChange").GetComponent<Button>().onClick.AddListener(EnablePreset);
            GameObject.Find("Canvas/clothesFileWindow/Window/WinRect/Load/btnCancel").GetComponent<Button>().onClick.AddListener(DisablePreset);
            GameObject.Find("Canvas/clothesFileWindow/Window/BasePanel/MenuTitle/btnClose").GetComponent<Button>().onClick.AddListener(DisablePreset);
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

        public void OnGui()
        {
            var guiShown = false;
            if (_hToggle) //if preset window is active draw file select
            {
                if (Input.GetMouseButtonDown(1))//if right click close
                {
                    DisablePreset();
                }
                var screenRect = new Rect((int)(Screen.width * 0.04), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select outfit folder");
                IMGUIUtils.EatInputInRect(screenRect);
                guiShown = true;
            }
            if (!guiShown) _folderTreeView?.StopMonitoringFiles();
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCoordinateFile == null) return; //if failed not initializing in "start"

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
                    {
                        _folderTreeView.ResetTreeCache();
                        OnFolderChanged();
                    }

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

        private static void EnablePreset()//listen to preset button
        {
            _hToggle = true;
        }

        private static void DisablePreset()//exit if either close button is clicked or right click
        {
            _hToggle = false;
        }
    }
}
