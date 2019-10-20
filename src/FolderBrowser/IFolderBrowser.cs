namespace BrowserFolders
{
    public interface IFolderBrowser
    {
        void OnGui();
        BrowserType Type { get; }
    }
}