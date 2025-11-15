using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders
{
    public interface IFolderBrowser
    {
        bool Initialize(bool isStudio, ConfigFile config, Harmony harmony);
        void Update();
        void OnGui();
        void OnListRefresh();
        Rect GetDefaultRect();
        FolderTreeView TreeView { get; }
        string Title { get; }
        Rect WindowRect { get; set; }
    }
}