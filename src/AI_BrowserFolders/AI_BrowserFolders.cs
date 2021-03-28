using System.Diagnostics;
using BepInEx;

namespace BrowserFolders
{
    [BepInProcess("AI-Syoujyo")]
    [BepInProcess("StudioNEOV2")]
    public partial class AI_BrowserFolders : BaseUnityPlugin
    {
        [Conditional("itsempty")] // remove if there's any actual code added
        private void GameSpecificAwake()
        {
        }

        [Conditional("itsempty")]
        private void GameSpecificOnGui()
        {
        }
    }
}
