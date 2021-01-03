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
namespace NUnit.Engine.Internal.FileSystemAccess
{
    /// <summary>
    /// Abstraction of a file-system.
    /// </summary>
    internal interface IFileSystem
    {
        /// <summary>
        /// Checks whether a directory exists or not.
        /// </summary>
        /// <param name="directory">Directory to check.</param>
        /// <returns><see langword="true"/> if <paramref name="directory"/> points to an existing directory; <see langword="false"/> otherwhise.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="directory"/> is <see langword="null"/>.</exception>
        bool Exists(IDirectory directory);

        /// <summary>
        /// Checks whether a file exists or not.
        /// </summary>
        /// <param name="file">File to check.</param>
        /// <returns><see langword="true"/> if <paramref name="file"/> points to an existing file; <see langword="false"/> otherwhise.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="file"/> is <see langword="null"/>.</exception>
        bool Exists(IFile file);

        /// <summary>
        /// Creates a <see cref="IDirectory"/> that points to an existing directory.
        /// </summary>
        /// <param name="path">Path of the directory.</param>
        /// <returns>An object representing the file-system entry located at <paramref name="path"/></returns>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="path"/> is empty or contains invalid characters (see <see cref="SIO.Path.InvalidPathChars"/> for details).</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="path"/> points to a non-existing directory.</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path exceeds the system-defined maximum length.</exception>
        IDirectory GetDirectory(string path);

        /// <summary>
        /// Creates a <see cref="IFile"/> that points to an existing file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>An object representing the file-system entry located at <paramref name="path"/></returns>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="path"/> is empty, contains no file-name or contains invalid characters (see <see cref="SIO.Path.GetInvalidFileNameChars"/> for details).</exception>
        /// <exception cref="System.IO.PathTooLongException">The specified path exceeds the system-defined maximum length.</exception>
        /// <exception cref="System.UnauthorizedAccessException">Access to <paramref name="path"/> is denied.</exception>
        /// <exception cref="System.NotSupportedException"><paramref name="path"/> contains a colon (:) in the middle of the string.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"><paramref name="path"/> points to a non-existing directory.</exception>
        IFile GetFile(string path);
    }
}
