// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Internal.FileSystemAccess
{
    /// <summary>
    /// A file contained in the file-system.
    /// </summary>
    // TODO: Temporarily public
    public interface IFile
    {
        /// <summary>
        /// Gets the directory that contains the file.
        /// </summary>
        /// <value>The parent directory.</value>
        IDirectory Parent { get; }

        /// <summary>
        /// Gets the full name of the file.
        /// </summary>
        /// <value>The full name consists of the parent directory and the file-name.</value>
        string FullName { get; }
    }
}