// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using SIO = System.IO;

namespace NUnit.FileSystemAccess
{
    /// <summary>
    /// Default implementation of <see cref="IFile"/> that relies on <see cref="System.IO"/>.
    /// </summary>
    public sealed class File : IFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="File"/> class.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>An object representing the file-system entry located at <paramref name="path"/>.</returns>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="path"/> contains no file-name or contains invalid characters (see <see cref="SIO.Path.GetInvalidFileNameChars"/> for details).</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path exceeds the system-defined maximum length.</exception>
        public File(string path)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("String is empty.", nameof(path));
            }

            var filename = SIO.Path.GetFileName(path);
            if (filename == string.Empty)
            {
                throw new ArgumentException("No filename found.", nameof(path));
            }

            if (filename.IndexOfAny(SIO.Path.GetInvalidFileNameChars()) > -1)
            {
                throw new ArgumentException("Filename contains invalid characters.", nameof(path));
            }

            var directory = SIO.Path.GetDirectoryName(path)!;
            if (directory.IndexOfAny(SIO.Path.GetInvalidPathChars()) > -1)
            {
                throw new ArgumentException("Directory contains invalid characters.", nameof(path));
            }

            this.FullName = new SIO.FileInfo(path).FullName;
            this.Parent = new Directory(directory);
        }

        /// <inheritdoc/>
        public IDirectory Parent { get; private set; }

        /// <inheritdoc/>
        public string FullName { get; private set; }
    }
}
