using System;
using System.IO;
using System.Linq;
using ActionGame;
using BepInEx.Configuration;
using HarmonyLib;
using Illusion.Extensions;
using Manager;
using UnityEngine;

namespace BrowserFolders.MainGame
{
    public class ClassroomFolders : BaseFolderBrowser
    {
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        internal static ConfigEntry<bool> EnableClassroom { get; private set; }
        private static ConfigEntry<bool> _randomCharaSubfolders;
        private static FolderTreeView _folderTreeView;

        private static string _targetScene;
        private static PreviewCharaList _customCharaFile;

        public ClassroomFolders() : base("Character folder", BrowserFoldersPlugin.UserDataPath, Overlord.GetDefaultPath(0)) { }

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

        protected override bool OnInitialize(bool isStudio, ConfigFile config, Harmony harmony)
        {
            if (isStudio) return false;

            _folderTreeView = TreeView;

            EnableClassroom = config.Bind("Main game", "Enable folder browser in classroom/new game browser", true, "Changes take effect on game restart");

            _randomCharaSubfolders = config.Bind("Main game", "Search subfolders for random characters", true, "When filling the class with random characters (or in other cases where a random character is picked) choose random characters from the main directory AND all of its subdirectories. If false, only search in the main directory (UserData/chara/female).");

            Overlord.Init();

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
            return EnableClassroom.Value && _customCharaFile != null && _customCharaFile.isVisible && _targetScene == Scene.AddSceneName && !Scene.IsOverlap && !Scene.IsNowLoadingFade ? 1 : 0;
        }

        public override void OnListRefresh()
        {
            _customCharaFile.SafeProc(ccf => ccf.CharFile.SafeProc(cf => cf.Initialize()));
        }

        public override Rect GetDefaultRect()
        {
            return new Rect((int)(Screen.width * 0.015), (int)(Screen.height * 0.35f), (int)(Screen.width * 0.16), (int)(Screen.height * 0.4));
        }

        private static class Hooks
        {
            /// <summary>
            /// Make it possible to fill in class with random characters from all subfolders
            /// ChaFileControl[] GetRandomUserDataFemaleCard(int num)
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Localize.Translate.Manager), nameof(Localize.Translate.Manager.GetRandomUserDataFemaleCard), typeof(int))]
            internal static bool RandomCharaPickOverride(int num, ref ChaFileControl[] __result)
            {
                if (!_randomCharaSubfolders.Value) return true;

                try
                {
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
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                    return true;
                }
            }
        }
    }
}
