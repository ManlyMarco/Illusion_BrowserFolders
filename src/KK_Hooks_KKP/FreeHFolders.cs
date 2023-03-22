using FreeH;
using Localize.Translate;
using Manager;
using UnityEngine;

namespace BrowserFolders.Hooks.KKP
{
    [BrowserType(BrowserType.FreeH)]
    public class FreeHFolders : IFolderBrowser
    {
        private static FolderTreeView _folderTreeView;
        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        //private static bool _isLive;
        private static string _targetScene;
        private static FreeHPreviewCharaList _freeHFile;
        private static CustomFileListSelecter _customFileListSelecter;
        private Rect _windowRect;

        public FreeHFolders()
        {
            _folderTreeView = new FolderTreeView(Overlord.GetUserDataRootPath(), Overlord.GetDefaultPath(0))
            {
                CurrentFolderChanged = OnFolderChanged
            };

            Overlord.Init();
        }

        public static void Init(FreeHPreviewCharaList list, int sex)
        {
            if (_freeHFile != list)
            {
                // Stop events from firing
                _freeHFile = null;

                _folderTreeView.DefaultPath = Overlord.GetDefaultPath(sex);
                _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

                _freeHFile = list;
                _customFileListSelecter = list.GetComponentInChildren<CustomFileListSelecter>();
                _targetScene = Scene.Instance.AddSceneName;
            }
        }

        private static void OnFolderChanged()
        {
            if (_freeHFile != null)
                _customFileListSelecter?.Initialize();
        }

        public void OnGui()
        {
            if (_freeHFile != null
                //&& !_isLive 
                && _targetScene == Scene.Instance.AddSceneName)
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
    }
}