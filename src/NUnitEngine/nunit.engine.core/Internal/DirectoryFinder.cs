// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using NUnit.Common;
using NUnit.Engine.Internal.FileSystemAccess;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// DirectoryFinder is a utility class used for extended wildcard
    /// selection of directories and files. It's less than a full-fledged
    /// Linux-style globbing utility and more than standard wildcard use.
    /// </summary>
    internal sealed class DirectoryFinder
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

        /// <summary>
        /// Gets all sub-directories recursively that match a pattern.
        /// </summary>
        /// <param name="startDirectory">Start point of the search.</param>
        /// <param name="pattern">Search pattern, where each path component may have wildcard characters. The wildcard "**" may be used to represent "all directories". Components need to be separated with slashes ('/').</param>
        /// <returns>All found sub-directories.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
        public IEnumerable<IDirectory> GetDirectories(IDirectory startDirectory, string pattern)
        {
            Guard.ArgumentNotNull(startDirectory, nameof(startDirectory));
            Guard.ArgumentNotNullOrEmpty(pattern, nameof(pattern));

            if (Path.DirectorySeparatorChar == '\\')
                pattern = pattern.Replace(Path.DirectorySeparatorChar, '/');

            var dirList = new List<IDirectory>();
            dirList.Add(startDirectory);

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
                    pattern = "";
                }

                if (range == "." || range == "")
                    continue;

                dirList = ExpandOneStep(dirList, range);
            }

            return dirList;
        }

        /// <summary>
        /// Gets all files that match a pattern.
        /// </summary>
        /// <param name="startDirectory">Start point of the search.</param>
        /// <param name="pattern">Search pattern, where each path component may have wildcard characters. The wildcard "**" may be used to represent "all directories". Components need to be separated with slashes ('/').</param>
        /// <returns>All found files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
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

        private List<IDirectory> ExpandOneStep(IList<IDirectory> dirList, string pattern)
        {
            var newList = new List<IDirectory>();

            foreach (var dir in dirList)
            {
                if (pattern == "." || pattern == "")
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
                    if (subDirs.Any()) newList.AddRange(subDirs);
                }
                else
                {
                    var subDirs = dir.GetDirectories(pattern, SearchOption.TopDirectoryOnly);
                    if (subDirs.Any()) newList.AddRange(subDirs);
                }
            }

            return newList;
        }
    }
}
