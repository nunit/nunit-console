//-----------------------------------------------------------------------
// <copyright file="IDirectory.cs" company="TillW">
//   Copyright 2020 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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