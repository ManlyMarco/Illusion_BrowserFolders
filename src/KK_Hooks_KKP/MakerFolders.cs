using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKP
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "Start")]
        public static void InitHook(CustomCharaFile __instance)
        {
            _folderTreeView.DefaultPath = Overlord.GetDefaultPath(CustomBase.Instance.modeSex);
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;

            var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
            _loadCharaToggle = gt.transform.Find("tglLoadChara").GetComponent<Toggle>();
            _saveCharaToggle = gt.transform.Find("tglSaveChara").GetComponent<Toggle>();

            var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
            _catToggle = mt.GetComponent<Toggle>();

            _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

            _targetScene = Scene.Instance.AddSceneName;
        }

        public void OnGui()
        {
            var guiShown = false;
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                    {
                        if (_refreshList)
                        {
                            _folderTreeView.ResetTreeCache();
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        if (_windowRect.IsEmpty())
                            _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                        InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select character folder", OnFolderChanged, drawAdditionalButtons: () =>
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
                _customCharaFile.Initialize(loadCharaToggleIsOn == true, false);
            }
        }
    }
}
