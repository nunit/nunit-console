//-----------------------------------------------------------------------
// <copyright file="IAddinsFileReader.cs" company="TillW">
//   Copyright 2021 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using NUnit.Engine.Internal.FileSystemAccess;
using System.Collections.Generic;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// A reader for NUnit addins-files.
    /// </summary>
    /// <remarks>
    /// The format of an addins-file can be found at https://docs.nunit.org/articles/nunit-engine/extensions/Installing-Extensions.html.
    /// </remarks>
    internal interface IAddinsFileReader
    {
        /// <summary>
        /// Reads all entries from an addins-file.
        /// </summary>
        /// <param name="path">Location of the file.</param>
        /// <returns>All entries contained in the file.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="file"/> is <see langword="null"/></exception>
        /// <exception cref="System.IO.IOException"><paramref name="file"/> cannot be found or read</exception>
        IEnumerable<string> Read(IFile file);
    }
}
