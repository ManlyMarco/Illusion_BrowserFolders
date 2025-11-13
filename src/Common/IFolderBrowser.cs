using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace BrowserFolders
{
    // TODO:
    // make all windows resizable
    // save positions of windows to config
    // if width is above height, make it horizontal layout
    // refactor stuff to make more window code shared

    public interface IFolderBrowser
    {
        bool Initialize(bool isStudio, ConfigFile config, Harmony harmony);
        void Update();
        void OnGui();
        // todo config entry to turn on/off remembering window pos/size
        Rect WindowRect { get; set; }
        Rect GetDefaultRect();
    }
}