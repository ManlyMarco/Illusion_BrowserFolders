using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using KKAPI.Utilities;

namespace BrowserFolders
{
    public class DirectoryTree
    {
        private List<DirectoryTree> _subDirs;

        public DirectoryTree(DirectoryInfo info)
        {
            Info = info;
            Name = Info.Name;
            FullName = FolderTreeView.NormalizePath(Info.FullName);
            Reset();
        }

        public DirectoryInfo Info { get; }

        public string Name { get; }
        public string FullName { get; }

        public List<DirectoryTree> SubDirs
        {
            get
            {
                if (_subDirs == null)
                {
                    try
                    {
                        _subDirs = Info.GetDirectories()
                            .OrderBy(x => x.Name, new WindowsStringComparer())
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