using System.IO;
using ChaCustom;
using HarmonyLib;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.HOutfit)]
    public class HOutfitFolders : IFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;
        private static bool _hToggle;//doesn't initialize to true here and at least in "public HOutfitFolders()" as true as it crashes the game on startup

        public HOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;
            
            Harmony.CreateAndPatchAll(typeof(HOutfitFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(clothesFileControl), "Start")]
        public static void InitHook(clothesFileControl __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;

            _hToggle = true; //weirdly enough required so file system would open the first time you use the preset per encounter 
            GameObject.Find("Canvas/SubMenu/DressCategory/ClothChange").GetComponent<Button>().onClick.AddListener(EnablePreset);//guessing cause listener is added after first click and doesnt execute but the other two seem fine
            GameObject.Find("Canvas/clothesFileWindow/Window/WinRect/Load/btnCancel").GetComponent<Button>().onClick.AddListener(DisablePreset);//maybe cause there are two disables
            GameObject.Find("Canvas/clothesFileWindow/Window/BasePanel/MenuTitle/btnClose").GetComponent<Button>().onClick.AddListener(DisablePreset);
        }

        public void OnGui()
        {
            bool guiShown = false;
            if (_hToggle)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F1))//if right click click or F1 close
                {
                    GameObject.Find("Canvas/clothesFileWindow").SetActive(false);
                    DisablePreset();
                }
                var screenRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                IMGUIUtils.DrawSolidBox(screenRect);
                GUILayout.Window(36, screenRect, TreeWindow, "Select outfit folder");
                IMGUIUtils.EatInputInRect(screenRect);
                guiShown = true;
            }
            if (!guiShown) _folderTreeView?.StopMonitoringFiles();
        }

        private static void OnFolderChanged()
        {
            if (_customCoordinateFile == null) return;
            Traverse.Create(_customCoordinateFile).Method("Initialize").GetValue();
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

        private static void EnablePreset()//listen to preset button
        {
            _hToggle = true;
        }
        private static void DisablePreset()//exit if either close button is clicked or right click
        {
            _hToggle = false;
        }
    }
}
