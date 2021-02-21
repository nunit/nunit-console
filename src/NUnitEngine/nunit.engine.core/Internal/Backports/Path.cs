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

            if (RunningOnWindows())
            {
                if (path.Length > 2)
                {
                    return (IsValidDriveSpecifier(path[0]) && path[1] == ':' && IsDirectorySeparator(path[2]))
                        || (IsDirectorySeparator(path[0]) && IsDirectorySeparator(path[1]));
                }
            }
            else
            {
                if (path.Length > 0)
                {
                    return IsDirectorySeparator(path[0]);
                }
            }

            return false;
        }

        private static bool RunningOnWindows()
        {
            return System.IO.Path.DirectorySeparatorChar == '\\';
        }

        private static bool IsDirectorySeparator(char c)
        {
            return c == System.IO.Path.DirectorySeparatorChar || c == System.IO.Path.AltDirectorySeparatorChar;
        }

        private static bool IsValidDriveSpecifier(char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z'); 
        }
    }
}
