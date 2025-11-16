using System.IO;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.MainGame
{
    public class MakerOutfitFolders : BaseFolderBrowser
    {
        private static Toggle _catToggle;
        private static CustomCoordinateFile _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _loadOutfitToggle;
        private static Toggle _saveOutfitToggle;
        private static GameObject _saveFront;
        private static CustomControl _customControl;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static bool _refreshList;
        private static string _targetScene;

        public MakerOutfitFolders() : base("Outfit folder", BrowserFoldersPlugin.UserDataPath, BrowserFoldersPlugin.UserDataPath) { }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in maker for outfits", true, "Changes take effect on game restart");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

            return true;
        }

        public override void Update()
        {
            base.Update();

            if (_refreshList && GuiVisible != 0)
            {
                _folderTreeView.ResetTreeCache();
                OnListRefresh();
                _refreshList = false;
            }
        }

        protected override void DrawControlButtons()
        {
            if (BrowserFoldersPlugin.DrawDefaultCardsToggle())
                OnListRefresh();

            base.DrawControlButtons();
        }

        protected override int IsVisible()
        {
            // Check UI visibility and the opened category
            if (_customControl && !_customControl.hideFrontUI && _catToggle != null && _catToggle.isOn && _targetScene == Scene.AddSceneName)
            {
                // Check opened tab
                if (_loadOutfitToggle != null && _loadOutfitToggle.isOn || _saveOutfitToggle != null && _saveOutfitToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if ((_saveFront == null || !_saveFront.activeSelf) && !Scene.IsOverlap && !Scene.IsNowLoadingFade)
                        return 1;
                }
            }

            return 0;
        }

        public override void OnListRefresh()
        {
            if (_customCoordinateFile == null) return;

            var loadOutfitToggleIsOn = _loadOutfitToggle != null && _loadOutfitToggle.isOn;
            if (loadOutfitToggleIsOn || _saveOutfitToggle != null && _saveOutfitToggle.isOn)
            {
                _customCoordinateFile.Initialize(loadOutfitToggleIsOn, false);
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
            [HarmonyPatch(typeof(CustomCoordinateFile), nameof(CustomCoordinateFile.Start))]
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

                _targetScene = Scene.AddSceneName;

                _customControl = UnityEngine.Object.FindObjectOfType<CustomControl>();
            }

            [HarmonyPrefix]
            [HarmonyWrapSafe]
            [HarmonyPatch(typeof(ChaFileCoordinate), nameof(ChaFileCoordinate.SaveFile))]
            internal static void SaveFilePatch(ref string path)
            {
                var name = Path.GetFileName(path);
                path = Path.Combine(DirectoryPathModifier(path), name);

                _refreshList = true;
            }
        }
    }
}
