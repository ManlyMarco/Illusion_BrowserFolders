using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using BrowserFolders.Common;
using UnityEngine;

namespace BrowserFolders
{
    [BepInPlugin(Guid, "Maker/Studio Browser Folders", Version)]
    public class KK_BrowserFolders : BaseUnityPlugin
    {
        public const string Guid = "marco.FolderBrowser";
        public const string Version = "1.3";

        private IFolderBrowser _sceneFolders;
        private IFolderBrowser _makerFolders;
        private IFolderBrowser _classroomFolders;
        private IFolderBrowser _freeHFolders;
        private IFolderBrowser _newGameFolders;

        [DisplayName("Enable folder browser in maker")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableMaker { get; private set; }

        [DisplayName("Enable folder browser in classroom/new game browser")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableClassroom { get; private set; }

        [DisplayName("Enable folder browser in Free H browser")]
        [Category("Main game")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableFreeH { get; private set; }

        [DisplayName("Enable folder browser in scene browser")]
        [Category("Chara Studio")]
        [Description("Changes take effect on game restart")]
        public static ConfigWrapper<bool> EnableStudio { get; private set; }

        [DisplayName("Save scenes to current folder")]
        [Category("Chara Studio")]
        [Description("When you select a custom folder to load a scene from, " +
                     "newly saved scenes will be saved to this folder.\n" +
                     "If disabled, scenes are always saved to default folder (studio/scene).")]
        public static ConfigWrapper<bool> StudioSaveOverride { get; private set; }

        private void OnGUI()
        {
            if (_sceneFolders != null) _sceneFolders.OnGui();
            else
            {
                _makerFolders?.OnGui();
                _classroomFolders?.OnGui();
                _freeHFolders?.OnGui();
                _newGameFolders?.OnGui();
            }
        }

        private void Start()
        {
            Assembly.Load(ResourceUtils.GetEmbeddedResource("Hooks_KK.dll"));
            if (Application.productName != "CharaStudio")
                Assembly.Load(ResourceUtils.GetEmbeddedResource("Hooks_KKP.dll"));

            var browserType = typeof(IFolderBrowser);
            var browsers = Assembly.GetExecutingAssembly()
                .GetTypesSafe()
                .Where(x => x.IsClass && !x.IsAbstract && browserType.IsAssignableFrom(x))
                .Attempt(Activator.CreateInstance)
                .Cast<IFolderBrowser>()
                .ToList();

            var maker = browsers.FirstOrDefault(x => x.Type == BrowserType.Maker);
            var classroom = browsers.FirstOrDefault(x => x.Type == BrowserType.Classroom);
            var newGame = browsers.FirstOrDefault(x => x.Type == BrowserType.NewGame);
            var freeH = browsers.FirstOrDefault(x => x.Type == BrowserType.FreeH);
            var scene = browsers.FirstOrDefault(x => x.Type == BrowserType.Scene);

            if (maker != null)
                EnableMaker = new ConfigWrapper<bool>(nameof(EnableMaker), this, true);

            if (classroom != null || newGame != null)
                EnableClassroom = new ConfigWrapper<bool>(nameof(EnableClassroom), this, true);

            if (freeH != null)
                EnableFreeH = new ConfigWrapper<bool>(nameof(EnableFreeH), this, true);

            if (scene != null)
            {
                EnableStudio = new ConfigWrapper<bool>(nameof(EnableStudio), this, true);
                StudioSaveOverride = new ConfigWrapper<bool>(nameof(StudioSaveOverride), this, false);
                Settings.StudioSaveOverride = () => StudioSaveOverride.Value;
            }

            if (Application.productName == "CharaStudio")
            {
                if (EnableStudio != null && EnableStudio.Value)
                    _sceneFolders = scene;
            }
            else
            {
                if (EnableMaker != null && EnableMaker.Value)
                    _makerFolders = maker;

                if (EnableClassroom != null && EnableClassroom.Value)
                {
                    _classroomFolders = classroom;
                    _newGameFolders = newGame;
                }

                if (EnableFreeH != null && EnableFreeH.Value)
                    _freeHFolders = freeH;
            }
        }
    }
}
