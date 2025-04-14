// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NUnit.FileSystemAccess
{
    /// <summary>
    /// DirectoryFinder is a utility class used for extended wildcard
    /// selection of directories and files. It's less than a full-fledged
    /// Linux-style globbing utility and more than standard wildcard use.
    /// </summary>
    public sealed class DirectoryFinder : IDirectoryFinder
    {
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryFinder"/> class.
        /// </summary>
        /// <param name="fileSystem">File-system to use.</param>
        public DirectoryFinder(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        /// <inheritdoc/>
        public IEnumerable<IDirectory> GetDirectories(IDirectory startDirectory, string pattern)
        {
            Guard.ArgumentNotNull(startDirectory, nameof(startDirectory));
            Guard.ArgumentNotNull(pattern, nameof(pattern));

            if (Path.DirectorySeparatorChar == '\\')
                pattern = pattern.Replace(Path.DirectorySeparatorChar, '/');

            var dirList = new List<IDirectory> { startDirectory };

            while (pattern.Length > 0)
            {
                string range;
                int sep = pattern.IndexOf('/');

                if (sep >= 0)
                {
                    range = pattern.Substring(0, sep);
                    pattern = pattern.Substring(sep + 1);
                }
                else
                {
                    range = pattern;
                    pattern = string.Empty;
                }

                if (range == "." || range == string.Empty)
                    continue;

                dirList = ExpandOneStep(dirList, range);
            }

            return dirList;
        }

        /// <inheritdoc/>
        public IEnumerable<IFile> GetFiles(IDirectory startDirectory, string pattern)
        {
            Guard.ArgumentNotNull(startDirectory, nameof(startDirectory));
            Guard.ArgumentNotNullOrEmpty(pattern, nameof(pattern));

            // If there is no directory path in pattern, delegate to DirectoryInfo
            int lastSep = pattern.LastIndexOf('/');
            if (lastSep < 0) // Simple file name entry, no path
                return startDirectory.GetFiles(pattern);

            // Otherwise split pattern into two parts around last separator
            var pattern1 = pattern.Substring(0, lastSep);
            var pattern2 = pattern.Substring(lastSep + 1);

            var fileList = new List<IFile>();

            foreach (var dir in this.GetDirectories(startDirectory, pattern1))
                fileList.AddRange(dir.GetFiles(pattern2));

            return fileList;
        }

        private static List<IDirectory> ExpandOneStep(IList<IDirectory> dirList, string pattern)
        {
            var newList = new List<IDirectory>();

            foreach (var dir in dirList)
            {
                if (pattern == "." || pattern == string.Empty)
                    newList.Add(dir);
                else if (pattern == "..")
                {
                    if (dir.Parent != null)
                        newList.Add(dir.Parent);
                }
                else if (pattern == "**")
                {
                    // ** means zero or more intervening directories, so we
                    // add the directory itself to start out.
                    newList.Add(dir);
                    var subDirs = dir.GetDirectories("*", SearchOption.AllDirectories);
                    if (subDirs.Any())
                        newList.AddRange(subDirs);
                }
                else
                {
                    var subDirs = dir.GetDirectories(pattern, SearchOption.TopDirectoryOnly);
                    if (subDirs.Any())
                        newList.AddRange(subDirs);
                }
            }

            return newList;
        }
    }
}
