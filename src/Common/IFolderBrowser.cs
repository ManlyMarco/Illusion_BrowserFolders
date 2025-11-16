using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders
{
    /// <summary>
    /// Full interface for a folder browser of a single in-game list.
    /// Must have a parameterless constructor.
    /// </summary>
    public interface IFolderBrowser
    {
        /// <summary>
        /// Init this instance.
        /// Return true on success, false if this instance is disabled for whatever reason, throw on error.
        /// </summary>
        bool Initialize(bool isStudio, ConfigFile config, Harmony harmony);
        /// <summary>
        /// Unity event call.
        /// </summary>
        void Update();
        /// <summary>
        /// Unity event call.
        /// </summary>
        void OnGui();
        /// <summary>
        /// Called whenever the in-game folder list needs to be refreshed.
        /// </summary>
        void OnListRefresh();
        /// <summary>
        /// Called to get the default window position and size if none was stored.
        /// </summary>
        Rect GetDefaultRect();
        /// <summary>
        /// Tree view instance used by this browser.
        /// </summary>
        FolderTreeView TreeView { get; }
        /// <summary>
        /// Title of this browser window.
        /// </summary>
        string Title { get; }
        /// <summary>
        /// Gets or sets the dimensions and position of the window.
        /// </summary>
        Rect WindowRect { get; set; }
    }
}