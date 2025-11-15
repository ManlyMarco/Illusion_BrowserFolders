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
        private static GameObject _ccwGo;
        private static CustomControl _customControl;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

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

            Overlord.Init();

            return true;
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
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if ((_saveFront == null || !_saveFront.activeSelf) && !Scene.IsOverlap && !Scene.IsNowLoadingFade && (_ccwGo == null || !_ccwGo.activeSelf))
                        return 1;
                }
            }

            return 0;
        }

        public override void OnListRefresh()
        {
            if (_customCharaFile == null) return;

            var loadCharaToggleIsOn = _loadCharaToggle != null && _loadCharaToggle.isOn;
            if (loadCharaToggleIsOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
            {
                _customCharaFile.Initialize(loadCharaToggleIsOn, false);
            }
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Initialize))]
            public static void InitializePatch(CustomCharaFile __instance)
            {
                if (_customCharaFile == null)
                {
                    _customCharaFile = __instance;

                    _folderTreeView.DefaultPath = Overlord.GetDefaultPath(__instance.chaCtrl.sex);
                    _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                    _targetScene = Scene.AddSceneName;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(CustomCharaFile), nameof(CustomCharaFile.Start))]
            public static void InitHook(CustomCharaFile __instance)
            {
                var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
                _loadCharaToggle = gt.transform.Find("tglLoadChara").GetComponent<Toggle>();
                _saveCharaToggle = gt.transform.Find("tglSaveChara").GetComponent<Toggle>();

                var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
                _catToggle = mt.GetComponent<Toggle>();

                _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

                _targetScene = Scene.AddSceneName;

                // Exit maker / save character dialog boxes
                _ccwGo = GameObject.FindObjectOfType<CustomCheckWindow>()?.gameObject;

                _customControl = GameObject.FindObjectOfType<CustomControl>();
            }
        }
    }
}
