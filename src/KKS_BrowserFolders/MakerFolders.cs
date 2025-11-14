using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKS
{
    [BrowserType(BrowserType.Maker)]
    public class MakerFolders : IFolderBrowser
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

        private static bool _refreshList;
        private static string _targetScene;
        private Rect _windowRect;

        public MakerFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(MakerFolders));

            MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);

            Overlord.Init();
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

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
        [HarmonyPatch(typeof(CustomCharaFile), "Start")]
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

        public void OnGui()
        {
            var guiShown = false;
            // Check UI visibility and the opened category
            if (_customControl && !_customControl.hideFrontUI && _catToggle != null && _catToggle.isOn && _targetScene == Scene.AddSceneName)
            {
                // Check opened tab
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if ((_saveFront == null || !_saveFront.activeSelf) && !Scene.IsOverlap && !Scene.IsNowLoadingFade && (_ccwGo == null || !_ccwGo.activeSelf))
                    {
                        if (_refreshList)
                        {
                            _folderTreeView.ResetTreeCache();
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        if (_windowRect.IsEmpty())
                            _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                        InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Character folder", OnFolderChanged, drawAdditionalButtons: () =>
                        {
                            if (Overlord.DrawDefaultCardsToggle())
                                OnFolderChanged();
                        });

                        guiShown = true;
                    }
                }
            }

            if (!guiShown)
            {
                _folderTreeView?.StopMonitoringFiles();
            }
        }

        private static void OnFolderChanged()
        {
            if (_customCharaFile == null) return;

            var loadCharaToggleIsOn = _loadCharaToggle != null && _loadCharaToggle.isOn;
            if (loadCharaToggleIsOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
            {
                _customCharaFile.Initialize(loadCharaToggleIsOn, false);
            }
        }
    }
}
