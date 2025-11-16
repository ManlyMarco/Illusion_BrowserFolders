using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using BepInEx.Configuration;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using UnityEngine;

namespace BrowserFolders.MainGame
{
    public class ClassroomFolders : BaseFolderBrowser
    {
        internal static ConfigEntry<bool> EnableClassroom { get; private set; }
        private static ConfigEntry<bool> _randomCharaSubfolders;
        private static FolderTreeView _folderTreeView;

        private static ClassRoomCharaFile _customCharaFile;
        private static Canvas _canvas;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public ClassroomFolders() : base("Character folder", BrowserFoldersPlugin.UserDataPath, Path.Combine(BrowserFoldersPlugin.UserDataPath, "chara/female/")) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            EnableClassroom = config.Bind("Main game", "Enable folder browser in classroom/new game browser", true, "Changes take effect on game restart");

            _randomCharaSubfolders = config.Bind("Main game", "Search subfolders for random characters", true, "When filling the class with random characters (or in other cases where a random character is picked) choose random characters from the main directory AND all of its subdirectories. If false, only search in the main directory (UserData/chara/female).");

            _folderTreeView = TreeView;
            
            if (isStudio) return false;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        protected override int IsVisible()
        {
            return EnableClassroom.Value && _canvas != null && _canvas.enabled && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _currentRelativeFolder = TreeView.CurrentRelativeFolder;

            if (_customCharaFile == null) return;

            _customCharaFile.InitializeList();

            // Fix add info toggle breaking
            var tglInfo = _customCharaFile.listCtrl.tglAddInfo;
            tglInfo.onValueChanged.Invoke(tglInfo.isOn);
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f),
                            (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ClassRoomCharaFile), nameof(ClassRoomCharaFile.Start))]
            internal static void InitHook(ClassRoomCharaFile __instance)
            {
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCharaFile = __instance;
                _canvas = __instance.transform.GetComponentInParent<Canvas>();

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(ClassRoomCharaFile), nameof(ClassRoomCharaFile.InitializeList))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "chara/female/", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(ClassroomFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    yield return instruction;
                }
            }

            /// <summary>
            /// Make it possible to fill in class with random characters from all subfolders
            /// ChaControl.GetRandomFemaleCard(int) : ChaFileControl[]
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.GetRandomFemaleCard), typeof(int))]
            internal static bool RandomCharaPickOverride(int num, ref ChaFileControl[] __result)
            {
                if (!_randomCharaSubfolders.Value) return true;

                try
                {
                    var path = Path.Combine(UserData.Path, "chara/female");
                    if (!Directory.Exists(path))
                    {
                        __result = new ChaFileControl[0];
                        return false;
                    }

                    // Grab from all subdirs
                    var results = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
                    // Try to load cards until enough load successfully
                    __result = results.Shuffle().Attempt(f =>
                    {
                        var chaFileControl = new ChaFileControl();
                        if (chaFileControl.LoadCharaFile(f, 1, true, true))
                        {
                            if (chaFileControl.parameter.sex != 0)
                                return chaFileControl;
                        }
                        return null;
                    }).Where(x => x != null).Take(num).ToArray();
                    return false;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    return true;
                }
            }
        }
    }
}
