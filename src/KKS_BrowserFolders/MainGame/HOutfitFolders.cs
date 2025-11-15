using System.IO;
using BepInEx.Configuration;
using ChaCustom;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
{
    // Handles both h scene outfits and dance mode
    public class HOutfitFolders : BaseFolderBrowser
    {
        private static clothesFileControl _customCoordinateFile;
        private static FolderTreeView _folderTreeView;
        private static GameObject _uiObject;
        private static string _sceneName;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        public HOutfitFolders() : base("Outfit folder", Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path)) { }

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            var enable = config.Bind("Main game", "Enable folder browser in H preset browser", true, "Changes take effect on game restart.\n Kplug doesn't support this and will restore previous outfit when not main or out of H.");

            if (isStudio || !enable.Value) return false;

            _folderTreeView = TreeView;

            harmony.PatchAll(typeof(Hooks));

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
            return _uiObject && _uiObject.activeSelf && _sceneName == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade ? 1 : 0;
        }

        protected override void OnListRefresh()
        {
            if (_customCoordinateFile != null)
                _customCoordinateFile.Initialize();
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(clothesFileControl), nameof(clothesFileControl.Start))]
            public static void InitHook(clothesFileControl __instance)
            {
                _folderTreeView.DefaultPath = Path.Combine(UserData.Path, "coordinate/");
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCoordinateFile = __instance;
                _uiObject = __instance.gameObject;
                _sceneName = Manager.Scene.AddSceneName;
            }
        }
    }
}
