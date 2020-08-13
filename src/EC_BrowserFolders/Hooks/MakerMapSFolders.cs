using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using Map;
using UnityEngine;

namespace BrowserFolders.Hooks.EC
{
    public class MakerMapSFolders : IFolderBrowser
    {
        private static MapSaveScene _mapSaveScene;

        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;

        public MakerMapSFolders()
        {
            _folderTreeView =
                new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            Harmony.CreateAndPatchAll(typeof(MakerMapSFolders));
        }

        public void OnGui()
        {
            // Check the opened category
            if (_mapSaveScene != null && _targetScene == Scene.Instance.AddSceneName)
            {
                if (_refreshList)
                {
                    OnFolderChanged();
                    _refreshList = false;
                }

                var screenRect = new Rect((int) (Screen.width * 0.004), (int) (Screen.height * 0.55f),
                    (int) (Screen.width * 0.125), (int) (Screen.height * 0.35));
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select map folder");
                IMGUIUtils.EatInputInRect(screenRect);
            }
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapSaveScene), "Awake")]
        internal static void InitHook(MapSaveScene __instance)

        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "map/data");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _mapSaveScene = __instance;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapSaveScene), "CreateList")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "map/data", StringComparison.OrdinalIgnoreCase))

                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(MakerMapSFolders).GetField(nameof(_currentRelativeFolder),
                                              BindingFlags.NonPublic | BindingFlags.Static) ??
                                          throw new MissingMethodException("could not find GetCurrentRelativeFolder");
                    ;
                }

                yield return instruction;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapInfo), "Save", typeof(string), typeof(bool))]
        internal static void Save(ref string _path)
        {
            var name = Path.GetFileName(_path);

            _path = Path.Combine(DirectoryPathModifier(_path), name);

            _refreshList = true;
        }

        internal static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int) (Screen.width * 0.015), (int) (Screen.height * 0.35f), (int) (Screen.width * 0.16),
                (int) (Screen.height * 0.4));
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_mapSaveScene == null) return;

            var ccf = Traverse.Create(_mapSaveScene);

            ccf.Method("CreateList").GetValue();
            ccf.Method("RecreateScrollerList").GetValue();

            // private bool Initialize()

            // Fix add info toggle breaking

            // Fix add info toggle breaking
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();
                Debug.Log(_folderTreeView);
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