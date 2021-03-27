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
        Harmony harmonyInstance;
        private static CanvasGroup _charaLoadVisible;
        private static GameObject _bookCanvas;
        private static GroupCharaSelectUI _charaLoad;

        private static VisibleWindow _lastRefreshed;
        private static FolderTreeView _folderTreeView;
        public HS2_MainGameFolders()
        {
            _folderTreeView = new FolderTreeView(AI_BrowserFolders.UserDataPath, AI_BrowserFolders.UserDataPath);
            _folderTreeView.CurrentFolderChanged = RefreshCurrentWindow;

            //Todo : If in main game
            //harmonyInstance = Harmony.CreateAndPatchAll(typeof(HS2_MainGamePatch));
            //Todo :If Leaving main game
            //harmonyInstance.UnpatchAll();
            //////
            Debug.LogWarning("Patching !!!");
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
                        tra.Field<List<GameCharaFileInfo>>("charaLists").Value = list;
                        Debug.Log("RefreshCurrentWindow.load");
                        resetTree = true;
                    }
                    break;
            }

            // clear tree cache
            if (resetTree)
            {
                Debug.Log("RefreshCurrentWindow.resetTree");
                _folderTreeView.ResetTreeCache();
                Debug.Log("RefreshCurrentWindow.resetTree(2)");
            }

        }

        internal static Rect GetDisplayRect()
        {
            const float x = 0.623f;
            const float y = 0.17f;
            const float w = 0.125f;
            const float h = 0.4f;

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
            Debug.Log("Ongui(1)");
            var screenRect = GetDisplayRect();
            IMGUIUtils.DrawSolidBox(screenRect);
            GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
            IMGUIUtils.EatInputInRect(screenRect);
            Debug.Log("Ongui(2)");
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                Debug.Log("TreeWindow(1)");
                _folderTreeView.DrawDirectoryTree();
                Debug.Log("TreeWindow(2)");
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
            Debug.Log("IsVisible()");
            if (_bookCanvas == null) return VisibleWindow.None;
            if (!_bookCanvas.activeSelf) return VisibleWindow.None;
            Debug.Log("IsVisible(2)");
            if (IsLoadVisible()) return VisibleWindow.Load;
            Debug.Log("IsVisible(3)");
            return VisibleWindow.None;

            bool IsLoadVisible()
            {
                return _charaLoad != null && _charaLoadVisible.interactable;
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
            Debug.LogWarning("Hook !!");
            _bookCanvas = __instance.gameObject.transform.parent.parent.gameObject;
            //Todo : Improve by getting canvas group of the child groupedit.
            _charaLoadVisible = __instance.gameObject.GetComponent<CanvasGroup>();
            Traverse tra = Traverse.Create(__instance);
            _charaLoad = tra.Field<GroupCharaSelectUI>("groupCharaSelectUI").Value;
            Debug.LogWarning(_bookCanvas);
            Debug.LogWarning(_charaLoadVisible);
            Debug.LogWarning(_charaLoad);
        }

    }

    public class HS2_MainGamePatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GroupListUI), "AddList")]
        public static bool AddList(List<GameCharaFileInfo> _list, List<string> _lstFileName)
        {
            GroupListUI someVariable = new GroupListUI();
            int num = 0;
            int i;
            for (i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();

                string[] source = Directory.GetFiles(UserData.Path + "chara/female/", _lstFileName[i] + ".png", SearchOption.AllDirectories).ToArray<string>();
                if (source.Any<string>())
                {

                    StringBuilder stringBuilder = new StringBuilder(source[0]);

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

            string userUUID = Singleton<GameSystem>.Instance.UserUUID;
            SaveData saveData = Singleton<Game>.Instance.saveData;
            int num = 0;

            for (int i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();

                string[] source = Directory.GetFiles(UserData.Path + "chara/female/", _lstFileName[i] + ".png", SearchOption.AllDirectories).ToArray<string>();
                if (source.Any<string>())
                {

                    StringBuilder stringBuilder = new StringBuilder(source[0]);
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

            string userUUID = Singleton<GameSystem>.Instance.UserUUID;
            SaveData saveData = Singleton<Game>.Instance.saveData;
            string value = UserData.Path + "chara/female/";
            int num = 0;
            for (int i = 0; i < _lstFileName.Count; i++)
            {
                ChaFileControl chaFileControl = new ChaFileControl();
                string[] source = Directory.GetFiles(UserData.Path + "chara/female/", _lstFileName[i] + ".png", SearchOption.AllDirectories).ToArray<string>();
                if (source.Any<string>())
                {

                    StringBuilder stringBuilder = new StringBuilder(source[0]);
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
            foreach (List<string> list in __instance.roomList)
            {
                List<string> list2 = new List<string>(list);
                for (int j = 0; j < list2.Count; j++)
                {
                    string[] source = Directory.GetFiles(UserData.Path + "chara/female/", list2[j] + ".png", SearchOption.AllDirectories).ToArray<string>();
                    if (source.Any<string>())
                    {
                        StringBuilder stringBuilder = new StringBuilder(source[0]);
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
                    string[] source = Directory.GetFiles(UserData.Path + "chara/female/", keyValuePair.Key + ".png", SearchOption.AllDirectories).ToArray<string>();
                    if (source.Any<string>())
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
            string[] source = Directory.GetFiles(UserData.Path + "chara/female/", searchPath, SearchOption.AllDirectories).ToArray<string>();
            if (source.Any<string>())
            {
                __result = source[0].ToString();
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FilePath), "GetFiles")]
        public static bool GetFiles(ref List<FilePath> __result, string _path)
        {
            List<KeyValuePair<DateTime, string>> list = (from s in Directory.GetFiles(_path, "*.png", SearchOption.AllDirectories)
                                                         select new KeyValuePair<DateTime, string>(File.GetLastWriteTime(s), s)).ToList<KeyValuePair<DateTime, string>>();
            using (new GameSystem.CultureScope())
            {
                list.Sort((KeyValuePair<DateTime, string> a, KeyValuePair<DateTime, string> b) => b.Key.CompareTo(a.Key));
            }
            __result = (from v in list
                        select new FilePath(v.Value, v.Key, FilePath.KindEN.Preset)).ToList<FilePath>();
            return false;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        private static void updateImage(ref MapSelectUI __instance)
        {
            Traverse tra = Traverse.Create(__instance);
            string[] source = Directory.GetFiles(UserData.Path + "chara/female/", tra.Field("firstCharaFile").GetValue().ToString() + ".png", SearchOption.AllDirectories).ToArray<string>();
            if (source.Any<string>())
            {
                tra.Field("firstCharaThumbnailUI").Field("rawimg").Field("texture").SetValue(PngAssist.ChangeTextureFromByte(PngFile.LoadPngBytes(source[0]), 0, 0, TextureFormat.ARGB32, false));
            }
            string[] source2 = Directory.GetFiles(UserData.Path + "chara/female/", tra.Field("secondCharaFile").GetValue().ToString() + ".png", SearchOption.AllDirectories).ToArray<string>();
            if (source2.Any<string>())
            {
                tra.Field("secondCharaThumbnailUI").Field("rawimg").Field("texture").SetValue(PngAssist.ChangeTextureFromByte(PngFile.LoadPngBytes(source2[0]), 0, 0, TextureFormat.ARGB32, false));
            }

        }
    }
}
