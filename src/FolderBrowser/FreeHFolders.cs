using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ActionGame;
using ChaCustom;
using FreeH;
using Harmony;
using Illusion.Game;
using Manager;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders
{
    public class FreeHFolders
    {
        private static FreeHClassRoomCharaFile _freeHFile;
        private static FolderTreeView _folderTreeView;

        private static string _currentRelativeFolder;
        // todo Actually fix this? Difficult
        private static bool _isLive;

        public FreeHFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.GetUserDataPath(), Utils.GetUserDataPath());
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyInstance.Create(KK_BrowserFolders.Guid + "." + nameof(FreeHFolders)).PatchAll(typeof(FreeHFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static void InitHook(FreeHClassRoomCharaFile __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine(Utils.GetUserDataPath(), __instance.sex != 0 ? @"chara/female" : "chara/male");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _freeHFile = __instance;

            _isLive = GameObject.Find("LiveStage") != null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FreeHClassRoomCharaFile), "Start")]
        public static IEnumerable<CodeInstruction> InitializePatch(IEnumerable<CodeInstruction> instructions)
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

        public static void ClearEventInvocations(object obj, string eventName)
        {
            var fi = GetEventField(obj.GetType(), eventName);
            if (fi == null) return;
            fi.SetValue(obj, null);
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

        /// <summary>
        /// Everything is put into Start and some vars we need to change are locals, so we need to clean state and run start again
        /// </summary>
        private static void RefreshList()
        {
            var listCtrl = (ClassRoomFileListCtrl)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "listCtrl").GetValue(_freeHFile);
            ClearEventInvocations(listCtrl, "OnPointerClick");
            var enterButton = (Button)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "enterButton").GetValue(_freeHFile);
            enterButton.onClick.RemoveAllListeners();

            //AccessTools.Method(typeof(FreeHClassRoomCharaFile), "Start").Invoke(_freeHFile, null);
            
            FolderAssist folderAssist = new FolderAssist();
            folderAssist.CreateFolderInfoEx(UserData.Path + _currentRelativeFolder, new[] { "*.png" }, true);
            listCtrl.ClearList();
            int fileCount = folderAssist.GetFileCount();
            int num = 0;
            Dictionary<int, ChaFileControl> chaFileDic = new Dictionary<int, ChaFileControl>();
            for (int i = 0; i < fileCount; i++)
            {
                FolderAssist.FileInfo fileInfo = folderAssist.lstFile[i];
                ChaFileControl chaFileControl = new ChaFileControl();
                if (chaFileControl.LoadCharaFile(fileInfo.FullPath, 255, false, true))
                {
                    if ((int)chaFileControl.parameter.sex == _freeHFile.sex)
                    {
                        string club = string.Empty;
                        string personality = string.Empty;
                        if (_freeHFile.sex != 0)
                        {
                            VoiceInfo.Param param;
                            if (!Singleton<Voice>.Instance.voiceInfoDic.TryGetValue(chaFileControl.parameter.personality, out param))
                            {
                                personality = "不明";
                            }
                            else
                            {
                                personality = param.Personality;
                            }
                            ClubInfo.Param param2;
                            if (!Game.ClubInfos.TryGetValue((int)chaFileControl.parameter.clubActivities, out param2))
                            {
                                club = "不明";
                            }
                            else
                            {
                                club = param2.Name;
                            }
                        }
                        else
                        {
                            listCtrl.DisableAddInfo();
                        }
                        listCtrl.AddList(num, chaFileControl.parameter.fullname, club, personality, fileInfo.FullPath, fileInfo.FileName, fileInfo.time, false, false);
                        chaFileDic.Add(num, chaFileControl);
                        num++;
                    }
                }
            }

            var info = (ReactiveProperty<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "info").GetValue(_freeHFile);
            listCtrl.OnPointerClick += delegate (CustomFileInfo cinfo)
            {
                info.Value = ((info != null) ? chaFileDic[cinfo.index] : null);
                Illusion.Game.Utils.Sound.Play(SystemSE.sel);
            };
            listCtrl.Create(delegate (CustomFileInfoComponent fic)
            {
                if (fic == null)
                {
                    return;
                }
                fic.transform.GetChild(0).GetOrAddComponent<PreviewDataComponent>().SetChaFile(chaFileDic[fic.info.index]);
            });
            enterButton.onClick.AddListener(() =>
            {
                var onEnter = (Action<ChaFileControl>)AccessTools.Field(typeof(FreeHClassRoomCharaFile), "onEnter").GetValue(_freeHFile);
                
                onEnter(info.Value);
            });
            Button[] source = new Button[]
            {
                enterButton
            };
            source.ToList<Button>().ForEach(delegate (Button bt)
            {
                bt.onClick.AddListener(()=>
                {
                    Illusion.Game.Utils.Sound.Play(SystemSE.ok_s);
                });
            });
        }

        public void OnGui()
        {
            if (_freeHFile != null && !_isLive)
            {
                var screenRect = ClassroomFolders.GetFullscreenBrowserRect();
                Utils.DrawSolidWindowBackground(screenRect);
                GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
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