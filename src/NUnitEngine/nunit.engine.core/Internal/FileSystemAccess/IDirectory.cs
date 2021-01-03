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
    using System.Collections.Generic;

    /// <summary>
    /// A directory contained in a file-system.
    /// </summary>
    internal interface IDirectory
    {
        /// <summary>
        /// Gets the parent directory.
        /// </summary>
        /// <value>The parent directory or <see langword="null"/> if the instance denotes a root (such as '\', 'c:\' , '\\server\share').</value>
        IDirectory Parent { get; }

        /// <summary>
        /// Gets the full path of the directory.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets all directories in the directory.
        /// </summary>
        /// <param name="searchPattern">The search string to match against the names of directories. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.</param>
        /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or all subdirectories.</param>
        /// <returns>All directories that match the search-pattern.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="searchPattern"/> contains one or more invalid characters defined by the <see cref="System.IO.Path.GetInvalidPathChars"/> method.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="System.IO.SearchOption"/>-value.</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">The directory is not on the file-system.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        IEnumerable<IDirectory> GetDirectories(string searchPattern, System.IO.SearchOption searchOption);

        /// <summary>
        /// Gets all files in the directory.
        /// </summary>
        /// <param name="searchPattern">The search string to match against the names of files. This parameter can contain a combination of valid literal path and wildcard (* and ?) characters, but it doesn't support regular expressions.</param>
        /// <returns>All found files.</returns>
        /// <exception cref="System.IO.DirectoryNotFoundException">The directory is not on the file-system.</exception>
        /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="searchPattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="searchPattern"/> contains one or more invalid characters defined by the <see cref="System.IO.Path.GetInvalidPathChars"/> method.</exception>
        IEnumerable<IFile> GetFiles(string searchPattern);
    }
}