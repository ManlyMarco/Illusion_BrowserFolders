using System.IO;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
{
    // Handles both h scene outfits and dance mode
    [BrowserType(BrowserType.HOutfit)]
    public class HOutfitFolders : IFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static GameObject _uiObject;
        private static string _sceneName;
        private Rect _windowRect;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        public HOutfitFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Harmony.CreateAndPatchAll(typeof(HOutfitFolders));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(clothesFileControl), "Start")]
        public static void InitHook(clothesFileControl __instance)
        {
            _folderTreeView.DefaultPath = Path.Combine((UserData.Path), "coordinate/");
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCoordinateFile = __instance;
            _uiObject = __instance.gameObject;
            _sceneName = Manager.Scene.AddSceneName;
        }

        public void OnGui()
        {
            if (_uiObject && _uiObject.activeSelf && _sceneName == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade)
            {
                if (_windowRect.IsEmpty())
                    _windowRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select outfit folder", OnFolderChanged, drawAdditionalButtons: () =>
                {
                    if (Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();
                });
            }
            else
            {
                _folderTreeView?.StopMonitoringFiles();
            }
        }

        private static void OnFolderChanged()
        {
            if (_customCoordinateFile == null) return;
            _customCoordinateFile.Initialize();
        }
    }
}
