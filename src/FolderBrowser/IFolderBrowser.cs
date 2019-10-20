namespace BrowserFolders.Common
{
    public interface IFolderBrowser
    {
        void OnGui();
        BrowserType Type { get; }
    }
}