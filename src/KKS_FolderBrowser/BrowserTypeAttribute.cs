using System;

namespace BrowserFolders
{
    public sealed class BrowserTypeAttribute : Attribute
    {
        public BrowserType BrowserType { get; }
        public BrowserTypeAttribute(BrowserType browserType)
        {
            BrowserType = browserType;
        }
    }
}