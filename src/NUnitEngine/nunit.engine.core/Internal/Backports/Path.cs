//-----------------------------------------------------------------------
// <copyright file="Path.cs" company="TillW">
//   Copyright 2021 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace NUnit.Engine.Internal.Backports
{
    /// <summary>
    /// Backports of <see cref="System.IO.Path"/> functionality that is only available in newer .NET versions.
    /// </summary>
    public static class Path
    {
        /// <summary>
        /// Returns a value that indicates whether the specified file path is absolute or not.
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns><see langword="true"/> if <paramref name="path"/> is an absolute or UNC path; otherwhise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/></exception>
        /// <remarks>See https://docs.microsoft.com/en-us/dotnet/api/system.io.path.ispathfullyqualified for original implementation.</remarks>
        public static bool IsPathFullyQualified(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return RunningOnWindows() ? PathUtils.IsFullyQualifiedWindowsPath(path) : PathUtils.IsFullyQualifiedUnixPath(path);
        }

        private static bool RunningOnWindows()
        {
            return System.IO.Path.DirectorySeparatorChar == '\\';
        }
    }
}
