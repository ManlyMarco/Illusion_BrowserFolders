using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using ChaCustom;
using Harmony;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders
{
    public class MakerFolders
    {
        private static Toggle _catToggle;
        private static CustomCharaFile _customCharaFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _loadCharaToggle;
        private static Toggle _saveCharaToggle;
        private static GameObject _saveFront;

        private static string _currentRelativeFolder;
        private static bool _refreshList;

        public MakerFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.GetUserDataPath(), Utils.GetUserDataPath());
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyInstance.Create(KK_BrowserFolders.Guid + "." + nameof(MakerFolders)).PatchAll(typeof(MakerFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "Start")]
        public static void InitHook(CustomCharaFile __instance)
        {
            var instance = CustomBase.Instance;
            _folderTreeView.DefaultPath = Path.Combine(Utils.GetUserDataPath(), instance.modeSex != 0 ? @"chara/female" : "chara/male");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;

            var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
            _loadCharaToggle = gt.transform.Find("tglLoadChara").GetComponent<Toggle>();
            _saveCharaToggle = gt.transform.Find("tglSaveChara").GetComponent<Toggle>();

            var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
            _catToggle = mt.GetComponent<Toggle>();

            _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
        public static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.SaveCharaFile), new Type[] { typeof(string), typeof(byte), typeof(bool) })]
        public static void SaveCharaFilePrefix(ref string filename)
        {
            if (CustomBase.Instance != null)
            {
                if (string.IsNullOrEmpty(Path.GetDirectoryName(filename)))
                {
                    filename = Path.Combine(_folderTreeView.CurrentFolder, filename);
                    _refreshList = true;
                }
            }
        }

        public void OnGui()
        {
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn)
            {
                // Check opened tab
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                    {
                        if (_refreshList)
                        {
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        var screenRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                        Utils.DrawSolidWindowBackground(screenRect);
                        GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                    }
                }
            }
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_customCharaFile == null) return;

            if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
            {
                var sls = typeof(CustomCharaFile);
                sls.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_customCharaFile, null);
            }
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
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                    if (GUILayout.Button("Screenshot folder"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.GetUserDataPath(), "cap")}\"");
                    if (GUILayout.Button("Main game folder"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.GetUserDataPath())}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}
