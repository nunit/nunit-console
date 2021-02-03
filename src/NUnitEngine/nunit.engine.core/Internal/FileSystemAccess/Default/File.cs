// ***********************************************************************
// Copyright (c) 2021 Charlie Poole, Rob Prouse
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
using System;
using SIO = System.IO;

namespace NUnit.Engine.Internal.FileSystemAccess.Default
{
    /// <summary>
    /// Default implementation of <see cref="IFile"/> that relies on <see cref="System.IO"/>.
    /// </summary>
    internal sealed class File : IFile
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
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

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

            var directory = SIO.Path.GetDirectoryName(path);
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
