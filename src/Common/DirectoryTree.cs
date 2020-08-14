using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace BrowserFolders
{
    public class DirectoryTree
    {
        private List<DirectoryTree> _subDirs;

        public DirectoryTree(DirectoryInfo info)
        {
            Info = info;
            Reset();
        }

        public DirectoryInfo Info { get; }

        public string Name => Info.Name;
        public string FullName => Info.FullName;

        public List<DirectoryTree> SubDirs
        {
            get
            {
                if (_subDirs == null)
                {
                    try
                    {
                        _subDirs = Info.GetDirectories()
                            .OrderBy(x => x.Name, new Utils.WindowsStringComparer())
                            .Select(x => new DirectoryTree(x))
                            .ToList();
                    }
                    catch (DirectoryNotFoundException) { }
                    catch (SecurityException) { }
                    catch (UnauthorizedAccessException) { }

                    if (_subDirs == null) _subDirs = new List<DirectoryTree>();
                }

                return _subDirs;
            }
        }

        public void Reset()
        {
            _subDirs = null;
        }
    }
}