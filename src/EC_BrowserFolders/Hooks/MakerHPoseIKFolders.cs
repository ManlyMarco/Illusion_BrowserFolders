using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Harmony;
using Pose;
using HarmonyLib;
using KKAPI.Maker;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Logging;
using UniRx;
using YS_Node;


namespace BrowserFolders.Hooks.EC
{
    [BrowserType(BrowserType.MakerHPoseIK)]
    public class MakerHPoseIKFolders : IFolderBrowser
    {


        private static HEdit.MotionIKUI _MotionIKUI;



        private static FolderTreeView _folderTreeView;


        private static GameObject _loadikToggle;

        private static string _currentRelativeFolder;
        private static bool _refreshList;
        private static string _targetScene;

        public MakerHPoseIKFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyWrapper.PatchAll(typeof(MakerHPoseIKFolders));

        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HEdit.MotionIKUI), "Start")]
        internal static void InitHook(HEdit.MotionIKUI __instance)

        {

            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), "edit/ik");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _MotionIKUI = __instance;

            _loadikToggle = GameObject.Find("HEditIndividualLoadWindow");






            _targetScene = Scene.Instance.AddSceneName;
        }






        [HarmonyTranspiler]
        [HarmonyPatch(typeof(HEdit.HEditIndividualLoadWindow), "Create")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {


            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "edit/ik", StringComparison.OrdinalIgnoreCase))

                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(MakerHPoseIKFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static) ??
                                  throw new MissingMethodException("could not find GetCurrentRelativeFolder"); ;
                }


                yield return instruction;
            }
        }



        public void OnGui()
        {
            // Check the opened category


            if (_MotionIKUI != null)
            {

                if (_refreshList)
                {
                   
                    _refreshList = false;
                }

                var screenRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select hik folder");

            }



        }

        internal static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }



        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_MotionIKUI == null) return;




            var ccf = Traverse.Create(_MotionIKUI);


            ccf.Method("Init").GetValue();








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
