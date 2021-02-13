//-----------------------------------------------------------------------
// <copyright file="IDirectory.cs" company="TillW">
//   Copyright 2021 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using NUnit.Engine.Internal.FileSystemAccess;
using System.Collections.Generic;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Implementations of this interface can be used to search for directories and files using wildcards.
    /// </summary>
    internal interface IDirectoryFinder
    {
        /// <summary>
        /// Gets all sub-directories recursively that match a pattern.
        /// </summary>
        /// <param name="startDirectory">Start point of the search.</param>
        /// <param name="pattern">Search pattern, where each path component may have wildcard characters. The wildcard "**" may be used to represent "all directories". Components need to be separated with slashes ('/').</param>
        /// <returns>All found sub-directories.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
        IEnumerable<IDirectory> GetDirectories(IDirectory startDirectory, string pattern);

        /// <summary>
        /// Gets all files that match a pattern.
        /// </summary>
        /// <param name="startDirectory">Start point of the search.</param>
        /// <param name="pattern">Search pattern, where each path component may have wildcard characters. The wildcard "**" may be used to represent "all directories". Components need to be separated with slashes ('/').</param>
        /// <returns>All found files.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startDirectory"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pattern"/> is empty.</exception>
        IEnumerable<IFile> GetFiles(IDirectory startDirectory, string pattern);
    }
}
