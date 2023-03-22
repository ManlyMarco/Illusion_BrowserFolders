using System;
using System.IO;
using System.Linq;
using ActionGame;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKS
{
    [BrowserType(BrowserType.Classroom)]
    public class ClassroomFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static string _targetScene;
        private static PreviewCharaList _customCharaFile;
        private Rect _windowRect;

        public ClassroomFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Overlord.Init();

            Harmony.CreateAndPatchAll(typeof(ClassroomFolders));
        }

        /// <summary>
        /// Make it possible to fill in class with random characters from all subfolders
        /// ChaFileControl[] GetRandomUserDataFemaleCard(int num)
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Localize.Translate.Manager), nameof(Localize.Translate.Manager.GetRandomUserDataFemaleCard), typeof(int))]
        internal static bool RandomCharaPickOverride(int num, ref ChaFileControl[] __result)
        {
            if (KKS_BrowserFolders.RandomCharaSubfolders?.Value != true) return true;

            var path = Path.Combine(UserData.Path, "chara/female");
            if (!Directory.Exists(path))
            {
                __result = Array.Empty<ChaFileControl>();
                return false;
            }

            // Grab from all subdirs
            var results = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
            // Try to load cards until enough load successfully
            __result = results.Shuffle().Attempt(f =>
            {
                var chaFileControl = new ChaFileControl();
                if (chaFileControl.LoadCharaFile(f, 1, true, true))
                {
                    if (chaFileControl.parameter.sex != 0)
                        return chaFileControl;
                }
                return null;
            }).Where(x => x != null).Take(num).ToArray();
            return false;
        }

        public static void Init(PreviewCharaList list, int sex)
        {
            if (_customCharaFile != list)
            {
                // Stop events from firing
                _customCharaFile = null;

                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _customCharaFile = list;
                _targetScene = Scene.AddSceneName;
            }
        }

        public void OnGui()
        {
            if (_customCharaFile != null && _customCharaFile.isVisible && _targetScene == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade)
            {
                if (_windowRect.IsEmpty())
                    _windowRect = GetFullscreenBrowserRect();

                InterfaceUtils.DisplayFolderWindow(_folderTreeView, () => _windowRect, r => _windowRect = r, "Select character folder", OnFolderChanged, drawAdditionalButtons: () =>
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

        private static Rect GetFullscreenBrowserRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }

        private static void OnFolderChanged()
        {
            _customCharaFile.SafeProc(ccf => ccf.CharFile.SafeProc(cf => cf.Initialize()));
        }
    }
}
