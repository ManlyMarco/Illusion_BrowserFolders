using FreeH;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    [BrowserType(BrowserType.FreeH)]
    [SuppressMessage("KK.Compatibility", "KKANAL04:Type is missing in KK Party.", Justification = "Library not used in KKP")]
    [SuppressMessage("KK.Compatibility", "KKANAL03:Member is missing or has a different signature in KK Party.", Justification = "Library not used in KKP")]
    public class FreeHFolders : IFolderBrowser
    {
        private static FreeHClassRoomCharaFile _freeHFile;
        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _isLive;
        private static string _targetScene;
        private static bool _refreshing;

        public FreeHFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(FreeHFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        
        internal static void InitHook(FreeHClassRoomCharaFile __instance)
        {
            if (_refreshing) return;

            _folderTreeView.DefaultPath = Path.Combine(Utils.NormalizePath(UserData.Path), __instance.sex != 0 ? @"chara/female" : "chara/male");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _freeHFile = __instance;

            // todo Actually fix this instead of the workaround? Difficult
            _isLive = GameObject.Find("LiveStage") != null;

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        internal static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (string.Equals(instruction.operand as string, "chara/female/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(instruction.operand as string, "chara/male/", StringComparison.OrdinalIgnoreCase))
                {
                    //0x7E	ldsfld <field>	Push the value of the static field on the stack.
                    instruction.opcode = OpCodes.Ldsfld;
                    instruction.operand = typeof(FreeHFolders).GetField(nameof(_currentRelativeFolder), BindingFlags.NonPublic | BindingFlags.Static);
                }

                yield return instruction;
            }
        }

        private static void ClearEventInvocations(object obj, string eventName)
        {
            var fi = GetEventField(obj.GetType(), eventName);
            fi?.SetValue(obj, null);
        }

        private static FieldInfo GetEventField(Type type, string eventName)
        {
            FieldInfo field = null;
            while (type != null)
            {
                /* Find events defined as field */
                field = type.GetField(eventName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null && (field.FieldType == typeof(MulticastDelegate) || field.FieldType.IsSubclassOf(typeof(MulticastDelegate))))
                    break;

                /* Find events defined as property { add; remove; } */
                field = type.GetField("EVENT_" + eventName.ToUpper(), BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    break;
                type = type.BaseType;
            }
            return field;
        }

        private static void RefreshList()
        {
            try
            {
                _refreshing = true;

                //Everything is put into Start and some vars we need to change are locals, so we need to clean state and run start again
                var listCtrl = _freeHFile.listCtrl;
                ClearEventInvocations(listCtrl, nameof(listCtrl.OnPointerClick));
                _freeHFile.enterButton.onClick.RemoveAllListeners();
                _freeHFile.Start();

                // Fix add info toggle breaking
                var tglAddInfo = listCtrl.tglAddInfo;
                tglAddInfo.onValueChanged.Invoke(tglAddInfo.isOn);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private bool _guiActive;

        public void OnGui()
        {
            if (_freeHFile != null && !_isLive && _targetScene == Scene.Instance.AddSceneName)
            {
                _guiActive = true;
                var screenRect = ClassroomFolders.GetFullscreenBrowserRect();
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                IMGUIUtils.EatInputInRect(screenRect);
            }
            else if (_guiActive)
            {
                _folderTreeView?.StopMonitoringFiles();
                _guiActive = false;
            }
        }

        private static void OnFolderChanged()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_freeHFile == null) return;

            RefreshList();
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