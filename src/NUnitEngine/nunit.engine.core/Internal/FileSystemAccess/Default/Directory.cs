// ***********************************************************************
// Copyright (c) 2021 NUnit Contributors
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
namespace NUnit.Engine.Internal.FileSystemAccess.Default
{
    using System;
    using System.Collections.Generic;
    using SIO = System.IO;

    /// <summary>
    /// Default implementation of <see cref="IDirectory"/> that relies on <see cref="System.IO"/>.
    /// </summary>
    internal sealed class Directory : IDirectory
    {
        private readonly SIO.DirectoryInfo directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Directory"/> class.
        /// </summary>
        /// <param name="path">Path of the directory.</param>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission to access <paramref name="path"/>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="path"/> contains invalid characters (see <see cref="SIO.Path.GetInvalidPathChars"/> for details).</exception>
        /// <exception cref="SIO.PathTooLongException"><paramref name="path"/> exceeds the system-defined maximum length.</exception>
        public Directory(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.IndexOfAny(SIO.Path.GetInvalidPathChars()) > -1)
            {
                throw new ArgumentException("Path contains invalid characters.", nameof(path));
            }

            this.directory = new SIO.DirectoryInfo(path);

            this.Parent = this.directory.Parent == null ? null : new Directory(directory.Parent.FullName);
        }

        /// <inheritdoc/>
        public IDirectory Parent { get; private set; }

        /// <inheritdoc/>
        public string FullName => this.directory.FullName;

        /// <inheritdoc/>
        public IEnumerable<IDirectory> GetDirectories(string searchPattern, SIO.SearchOption searchOption)
        {
            List<IDirectory> directories = new List<IDirectory>();
            foreach (var currentDirectory in this.directory.GetDirectories(searchPattern, searchOption))
            {
                directories.Add(new Directory(currentDirectory.FullName));
            }

            return directories;
        }

        /// <inheritdoc/>
        public IEnumerable<IFile> GetFiles(string searchPattern)
        {
            List<IFile> files = new List<IFile>();
            foreach (var currentFile in this.directory.GetFiles(searchPattern))
            {
                files.Add(new File(currentFile.FullName));
            }

            return files;
        }
    }
}
