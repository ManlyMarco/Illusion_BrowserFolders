using System.IO;
using BepInEx.Harmony;
using ChaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.MakerOutfit)]
    public class MakerOutfitFolders : IFolderBrowser
    {
        private static Toggle _catToggle;
        private static CustomCoordinateFile _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _loadOutfitToggle;
        private static Toggle _saveOutfitToggle;
        private static GameObject _saveFront;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static bool _refreshList;
        private static string _targetScene;

        public MakerOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyWrapper.PatchAll(typeof(MakerOutfitFolders));

            //MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);

            //Overlord.Init();
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCoordinateFile), "Start")]
        public static void InitHook(CustomCoordinateFile __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;

            var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
            _loadOutfitToggle = gt.transform.Find("tglLoadCos").GetComponent<Toggle>();
            _saveOutfitToggle = gt.transform.Find("tglSaveCos").GetComponent<Toggle>();

            var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
            _catToggle = mt.GetComponent<Toggle>();

            _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

            _targetScene = Scene.Instance.AddSceneName;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileCoordinate), "SaveFile")]
        internal static void SaveFilePatch(ref string path)
        {
            var name = Path.GetFileName(path);
            path = Path.Combine(DirectoryPathModifier(path), name);

            _refreshList = true;
        }

        public void OnGui()
        {
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_loadOutfitToggle != null && _loadOutfitToggle.isOn || _saveOutfitToggle != null && _saveOutfitToggle.isOn)
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
                        IMGUIUtils.DrawSolidBox(screenRect);
                        GUILayout.Window(362, screenRect, TreeWindow, "Select outfit folder");
                        Utils.EatInputInRect(screenRect);
                    }
                }
            }
        }

        private static void OnFolderChanged()
        {
            if (_customCoordinateFile == null) return;

            if (_loadOutfitToggle != null && _loadOutfitToggle.isOn || _saveOutfitToggle != null && _saveOutfitToggle.isOn)
            {
                // private bool Initialize(bool isDefaultDataAdd, bool reCreate)
                Traverse.Create(_customCoordinateFile).Method("Initialize", _loadOutfitToggle?.isOn == true, false).GetValue();
            }
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();

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
