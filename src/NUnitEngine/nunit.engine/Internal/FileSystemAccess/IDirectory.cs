//-----------------------------------------------------------------------
// <copyright file="IDirectory.cs" company="TillW">
//   Copyright 2020 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace NUnit.Engine.Internal.FileSystemAccess
{
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
    }
}