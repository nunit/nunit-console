// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using SIO = System.IO;

namespace NUnit.FileSystemAccess
{
    /// <summary>
    /// Default implementation of <see cref="IDirectory"/> that relies on <see cref="System.IO"/>.
    /// </summary>
    public sealed class Directory : IDirectory
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
            Guard.ArgumentNotNull(path, nameof(path));

            if (path.IndexOfAny(SIO.Path.GetInvalidPathChars()) > -1)
            {
                throw new ArgumentException("Path contains invalid characters.", nameof(path));
            }

            this.directory = new SIO.DirectoryInfo(path);

            this.Parent = this.directory.Parent is null ? null : new Directory(directory.Parent.FullName);
        }

        /// <inheritdoc/>
        public IDirectory? Parent { get; private set; }

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
