using FreeH;
using HarmonyLib;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using UnityEngine;

namespace BrowserFolders.Hooks.KK
{
    public class FreeHFolders : BaseFolderBrowser
    {
        private static FreeHClassRoomCharaFile _freeHFile;
        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        private static bool _isLive;
        private static string _targetScene;
        private static bool _refreshing;

        public FreeHFolders() : base("Character folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in Free H browser", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));
            return true;
        }

        protected override int IsVisible()
        {
            return _freeHFile != null && !_isLive && _targetScene == Scene.Instance.AddSceneName ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            _currentRelativeFolder = _folderTreeView.CurrentRelativeFolder;

            if (_freeHFile == null) return;

            RefreshList();
        }
        
        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f),
                            (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
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
            
            void ClearEventInvocations(object obj, string eventName)
            {
                var fi = GetEventField(obj.GetType(), eventName);
                fi?.SetValue(obj, null);
            }

            FieldInfo GetEventField(Type type, string eventName)
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
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(FreeHClassRoomCharaFile), nameof(FreeHClassRoomCharaFile.Start))]
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
            [HarmonyPatch(typeof(FreeHClassRoomCharaFile), nameof(FreeHClassRoomCharaFile.Start))]
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
        }
    }
}