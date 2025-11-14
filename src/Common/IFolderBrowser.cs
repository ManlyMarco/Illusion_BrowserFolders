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
        Rect WindowRect { get; set; }
        Rect GetDefaultRect();
    }
}