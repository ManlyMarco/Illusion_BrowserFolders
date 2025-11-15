using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.MainGame
{
    public class MakerFolders : BaseFolderBrowser
    {
        private static Toggle _catToggle;
        private static CustomCharaFile _customCharaFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _loadCharaToggle;
        private static Toggle _saveCharaToggle;
        private static GameObject _saveFront;

        private static string _currentRelativeFolder;
        private static string _targetScene;

        public MakerFolders() : base("Character folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in maker", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);

            return true;
        }


        protected override int IsVisible()
        {
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
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

            if (_customCharaFile == null) return;

            if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
            {
                _customCharaFile.Initialize();

                // Fix add info toggle breaking
                var tglInfo = _customCharaFile.listCtrl.tglAddInfo;
                tglInfo.onValueChanged.Invoke(tglInfo.isOn);
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
            [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Start))]
            internal static void InitHook(CustomCharaFile __instance)
            {
                var instance = CustomBase.Instance;
                _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), instance.modeSex != 0 ? @"chara/female" : "chara/male");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCharaFile = __instance;

                var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
                _loadCharaToggle = gt.transform.Find("tglLoadChara").GetComponent<Toggle>();
                _saveCharaToggle = gt.transform.Find("tglSaveChara").GetComponent<Toggle>();

                var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
                _catToggle = mt.GetComponent<Toggle>();

                _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

                _targetScene = Scene.Instance.AddSceneName;
            }

            [HarmonyTranspiler]
            [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Initialize))]
            internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var instruction in instructions)
                {
                    if (string.Equals(instruction.operand as string, "chara/female/", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(instruction.operand as string, "chara/male/", StringComparison.OrdinalIgnoreCase))
                    {
                        //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                        instruction.opcode = OpCodes.Ldsfld;
                        instruction.operand = typeof(MakerFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                    }

                    yield return instruction;
                }
            }
        }
    }
}
