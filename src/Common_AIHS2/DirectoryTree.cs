using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BrowserFolders
{
    public class DirectoryTree
    {
        private List<DirectoryTree> _subDirs;
        public DirectoryInfo Info { get; }

        public string Name => Info.Name;
        public string FullName => Info.FullName;
        public DirectoryTree(DirectoryInfo info)
        {
            Info = info;
            Reset();
        }
        public List<DirectoryTree> SubDirs
        {
            get
            {
                if (_subDirs != null) return _subDirs;
                _subDirs = Info.GetDirectories().OrderBy(x => x.Name, new Utils.WindowsStringComparer()).Select(
                    x => new DirectoryTree(x)).ToList();
                return _subDirs;
            }
        }

        public void Reset()
        {
            _subDirs = null;
        }
    }
}
