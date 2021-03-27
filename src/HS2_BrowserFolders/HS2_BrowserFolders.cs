using AIChara;
using BepInEx;
using BepInEx.Configuration;
using CharaCustom;
using GameLoadCharaFileSystem;
using HarmonyLib;
using HS2;
using KK_Lib;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Utilities;
using Manager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BrowserFolders
{

    using Debug = UnityEngine.Debug;
    [BepInProcess("HoneySelect2")]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {

        private IFolderBrowser _hs2MainGameFolders;

        public static ConfigEntry<bool> EnableMainGame { get; private set; }


    }

    public class HS2_MainGameFolders : IFolderBrowser
    {
        
        private static CanvasGroup _charaLoadVisible;
        private static GameObject _bookCanvas;
        private static GroupCharaSelectUI _charaLoad;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;
        public HS2_MainGameFolders()
        {
            string pathDefault = Path.Combine(Utils.NormalizePath(UserData.Path), "chara/female");
            _folderTreeView = new FolderTreeView(pathDefault, pathDefault);
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;
            //Harmony.CreateAndPatchAll(typeof(HS2_MainGamePatch));
            Harmony.CreateAndPatchAll(typeof(HS2_MainGameFolders));
        }


        private static string GetCurrentRelativeFolder(string defaultPath)
        {
            if (IsVisible() == VisibleWindow.None) return defaultPath;
            return _folderTreeView?.CurrentRelativeFolder ?? defaultPath;
        }

        private static void RefreshCurrentWindow()
        {

            var visibleWindow = IsVisible();
            _lastRefreshed = visibleWindow;
            var resetTree = false;

            switch (visibleWindow)
            {
                case VisibleWindow.Load:
                    if (_charaLoad != null)
                    {
                        Traverse tra = Traverse.Create(_charaLoad);
                        tra.Field<List<GameCharaFileInfo>>("charaLists").Value.Clear();
                        List<GameCharaFileInfo> list = new List<GameCharaFileInfo>();
                        var method = typeof(GameCharaFileInfoAssist).GetMethod("AddList", BindingFlags.Static | BindingFlags.NonPublic);
                        byte b = 1;
                        method.Invoke(obj: null, parameters: new object[] { list, _folderTreeView.CurrentFolder, 0, b, true, true, false, false, true, false });
                        list.ForEach(charaFileInfo => { charaFileInfo.FileName = Path.GetRelativePath(UserData.Path, _folderTreeView.CurrentFolder)+charaFileInfo.FileName; });//the filename has to have no .png extension in it, that is why we get the relative path from the folder and then add the name
                        tra.Field<List<GameCharaFileInfo>>("charaLists").Value = list;
                        _charaLoad.ReDrawListView();

                        resetTree = true;
                    }
                    break;
            }

            // clear tree cache
            if (resetTree)
            {

                _folderTreeView.ResetTreeCache();

            }

        }

        internal static Rect GetDisplayRect()
        {
            const float x = 0.02f;
            const float y = 0.59f;
            const float w = 0.200f;
            const float h = 0.35f;

            return new Rect((int)(Screen.width * x), (int)(Screen.height * y),
                (int)(Screen.width * w), (int)(Screen.height * h));
        }

        public void OnGui()
        {
            var visibleWindow = IsVisible();
            if (visibleWindow == VisibleWindow.None)
            {
                _lastRefreshed = VisibleWindow.None;
                _folderTreeView?.StopMonitoringFiles();
                return;
            }

            if (_lastRefreshed != visibleWindow) RefreshCurrentWindow();
            var screenRect = GetDisplayRect();
            IMGUIUtils.DrawSolidBox(screenRect);
            GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            IMGUIUtils.EatInputInRect(screenRect);
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {

                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh thumbnails"))
                        RefreshCurrentWindow();

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

        private static VisibleWindow IsVisible()
        {

            if (_bookCanvas == null) return VisibleWindow.None;
            if (!_bookCanvas.activeSelf) return VisibleWindow.None;

            if (IsLoadVisible()) return VisibleWindow.Load;

            return VisibleWindow.None;

            bool IsLoadVisible()
            {

                return _charaLoad != null && (_charaLoadVisible.alpha == 1);
            }
        }
        private enum VisibleWindow
        {
            None,
            Load
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaEditUI), "Start")]
        private static void BookOpenHook(ref CharaEditUI __instance)
        {
            _bookCanvas = __instance.gameObject.transform.Find("Group")?.gameObject;
            //Todo : Improve by getting canvas group of the child groupedit.

            _charaLoadVisible = __instance.gameObject.transform.Find("Group")?.GetComponent<CanvasGroup>();
            Traverse tra = Traverse.Create(__instance);
            _charaLoad = tra.Field<GroupCharaSelectUI>("groupCharaSelectUI").Value;
        }

    }

    public class HS2_MainGamePatch
    {

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GroupListUI), "AddList")]
        public static bool AddList(List<GameCharaFileInfo> _list, List<string> _lstFileName)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;
            }
            GroupListUI someVariable = new GroupListUI();
            int num = 0;
            int i;
            for (i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();

                string source = Path.Combine(UserData.Path + "chara/female/", _lstFileName[i]);
                if (File.Exists(source))
                {

                    StringBuilder stringBuilder = new StringBuilder(source);

                    if (!chaFileControl.LoadCharaFile(stringBuilder.ToString(), 255, false, true))
                    {
                        chaFileControl.GetLastErrorCode();
                    }
                    else if (chaFileControl.parameter.sex == 1)
                    {
                        VoiceInfo.Param param;
                        string personality;
                        if (!Voice.infoTable.TryGetValue(chaFileControl.parameter2.personality, out param))
                        {
                            personality = "不明";
                        }
                        else
                        {
                            personality = param.Personality;
                        }
                        HashSet<int> hashSet = new HashSet<int>(from value in Singleton<Game>.Instance.saveData.roomList.Select((List<string> name, int index) => new
                        {
                            name,
                            index
                        })
                                                                where value.name.Contains(_lstFileName[i])
                                                                select value.index + 1);
                        if (hashSet.Count == 0)
                        {
                            hashSet.Add(0);
                        }
                        _list.Add(new GameCharaFileInfo
                        {
                            index = num++,
                            name = chaFileControl.parameter.fullname,
                            personality = personality,
                            voice = chaFileControl.parameter2.personality,
                            hair = chaFileControl.custom.hair.kind,
                            birthMonth = (int)chaFileControl.parameter.birthMonth,
                            birthDay = (int)chaFileControl.parameter.birthDay,
                            strBirthDay = chaFileControl.parameter.strBirthDay,
                            sex = (int)chaFileControl.parameter.sex,
                            FullPath = stringBuilder.ToString(),
                            FileName = _lstFileName[i],
                            state = chaFileControl.gameinfo2.nowDrawState,
                            trait = chaFileControl.parameter2.trait,
                            hAttribute = chaFileControl.parameter2.hAttribute,
                            resistH = chaFileControl.gameinfo2.resistH,
                            resistPain = chaFileControl.gameinfo2.resistPain,
                            resistAnal = chaFileControl.gameinfo2.resistAnal,
                            broken = chaFileControl.gameinfo2.Broken,
                            dependence = chaFileControl.gameinfo2.Dependence,
                            usedItem = chaFileControl.gameinfo2.usedItem,
                            lockNowState = chaFileControl.gameinfo2.lockNowState,
                            lockBroken = chaFileControl.gameinfo2.lockBroken,
                            lockDependence = chaFileControl.gameinfo2.lockDependence,
                            hcount = chaFileControl.gameinfo2.hCount,
                            lstFilter = hashSet,
                            cateKind = CategoryKind.Female,
                            data_uuid = chaFileControl.dataID
                        });
                    }
                }
            }
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(STRCharaFileInfoAssist), "AddList")]
        private static bool AddList2(List<STRCharaFileInfo> _list, List<string> _lstFileName, int _state)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;
            }
            string userUUID = Singleton<GameSystem>.Instance.UserUUID;
            SaveData saveData = Singleton<Game>.Instance.saveData;
            int num = 0;

            for (int i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();

                string source = Path.Combine(UserData.Path + "chara/female/", _lstFileName[i]);
                if (File.Exists(source))
                {

                    StringBuilder stringBuilder = new StringBuilder(source);
                    if (!chaFileControl.LoadCharaFile(stringBuilder.ToString(), 255, false, true))
                    {
                        chaFileControl.GetLastErrorCode();
                    }
                    else if (chaFileControl.parameter.sex == 1 && chaFileControl.gameinfo2.hCount != 0 && (_state == -1 || chaFileControl.gameinfo2.nowDrawState == (ChaFileDefine.State)_state))
                    {
                        VoiceInfo.Param param;
                        if (Voice.infoTable.TryGetValue(chaFileControl.parameter2.personality, out param))
                        {
                            string personality = param.Personality;
                        }
                        _list.Add(new STRCharaFileInfo
                        {
                            index = num++,
                            name = chaFileControl.parameter.fullname,
                            personality = chaFileControl.parameter2.personality,
                            FullPath = stringBuilder.ToString(),
                            FileName = _lstFileName[i],
                            time = File.GetLastWriteTime(stringBuilder.ToString()),
                            futanari = chaFileControl.parameter.futanari,
                            state = chaFileControl.gameinfo2.nowDrawState,
                            trait = chaFileControl.parameter2.trait,
                            hAttribute = chaFileControl.parameter2.hAttribute
                        });
                    }
                }
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(STRCharaFileInfoAssist1), "AddList")]
        private static bool AddList3(List<STRCharaFileInfo1> _list, List<string> _lstFileName, int _state)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;
            }
            string userUUID = Singleton<GameSystem>.Instance.UserUUID;
            SaveData saveData = Singleton<Game>.Instance.saveData;
            string value = UserData.Path + "chara/female/";
            int num = 0;
            for (int i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();
                string source = Path.Combine(UserData.Path + "chara/female/", _lstFileName[i]);
                if (File.Exists(source))
                {

                    StringBuilder stringBuilder = new StringBuilder(source);
                    if (!chaFileControl.LoadCharaFile(stringBuilder.ToString(), 255, false, true))
                    {
                        chaFileControl.GetLastErrorCode();
                    }
                    else if (chaFileControl.parameter.sex == 1 && chaFileControl.gameinfo2.hCount != 0 && (_state == -1 || chaFileControl.gameinfo2.nowDrawState == (ChaFileDefine.State)_state))
                    {
                        VoiceInfo.Param param;
                        if (Voice.infoTable.TryGetValue(chaFileControl.parameter2.personality, out param))
                        {
                            string personality = param.Personality;
                        }
                        _list.Add(new STRCharaFileInfo1
                        {
                            index = num++,
                            name = chaFileControl.parameter.fullname,
                            personality = chaFileControl.parameter2.personality,
                            FullPath = stringBuilder.ToString(),
                            FileName = _lstFileName[i],
                            time = File.GetLastWriteTime(stringBuilder.ToString()),
                            futanari = chaFileControl.parameter.futanari,
                            state = chaFileControl.gameinfo2.nowDrawState,
                            trait = chaFileControl.parameter2.trait,
                            hAttribute = chaFileControl.parameter2.hAttribute
                        });
                    }
                }
            }
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveData), "RoomListCharaExists")]
        public static bool RoomListCharaExists(ref SaveData __instance)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;
            }
            foreach (List<string> list in __instance.roomList)
            {
                List<string> list2 = new List<string>(list);
                for (int j = 0; j < list2.Count; j++)
                {
                    string source = Path.Combine(UserData.Path + "chara/female/", list2[j]);
                    if (File.Exists(source))
                    {
                        StringBuilder stringBuilder = new StringBuilder(source);
                        if (!File.Exists(stringBuilder.ToString()))
                        {
                            list.Remove(list2[j]);
                        }
                    }
                }
            }
            for (int k = 0; k < __instance.dicCloths.Length; k++)
            {
                foreach (KeyValuePair<string, ClothPngInfo> keyValuePair in new Dictionary<string, ClothPngInfo>(__instance.dicCloths[k]))
                {
                    string source = Path.Combine(UserData.Path + "chara/female/", keyValuePair.Key);
                    if (File.Exists(source))
                    {
                        StringBuilder stringBuilder2 = new StringBuilder(source[0]);
                        if (!File.Exists(stringBuilder2.ToString()))
                        {
                            __instance.dicCloths[k].Remove(keyValuePair.Key);
                        }
                    }
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileControl), "ConvertCharaFilePath")]
        public static bool ConvertCharaFilePath(ref ChaFileControl __instance, ref string __result, string path, byte _sex, bool newFile = false)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return true;
            }
            byte b = (byte.MaxValue == _sex) ? __instance.parameter.sex : _sex;
            if (path == "")
            {
                __result = "";
                return false;
            }
            string searchPath = Path.GetFileName(path);
            if (searchPath.IsNullOrEmpty())
            {
                return true;
            }
            string source = Path.Combine(UserData.Path + "chara/female/", path);
            if (File.Exists(source))
            {
                __result = source;
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        private static void updateImage(ref MapSelectUI __instance)
        {
            if (!KKAPI.KoikatuAPI.GetCurrentGameMode().Equals(KKAPI.GameMode.MainGame))
            {
                return;
            }
            Traverse tra = Traverse.Create(__instance);
            
            string source = Path.Combine(UserData.Path + "chara/female/", tra.Field("firstCharaFile").GetValue().ToString() + ".png");
            if (File.Exists(source))
            {
                tra.Field("firstCharaThumbnailUI").Field("rawimg").Field("texture").SetValue(PngAssist.ChangeTextureFromByte(PngFile.LoadPngBytes(source), 0, 0, TextureFormat.ARGB32, false));
            }
            string source2 = Path.Combine(UserData.Path + "chara/female/", tra.Field("secondCharaFile").GetValue().ToString() + ".png");
            if (File.Exists(source2))
            {
                tra.Field("secondCharaThumbnailUI").Field("rawimg").Field("texture").SetValue(PngAssist.ChangeTextureFromByte(PngFile.LoadPngBytes(source2), 0, 0, TextureFormat.ARGB32, false));
            }

        }
        */
    }
}
