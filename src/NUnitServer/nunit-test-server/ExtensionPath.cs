// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Linq;

namespace NUnit.Remote.Hosting
{
    /// <summary>Compares discovered extension paths with their explicitly staged root.</summary>
    internal static class ExtensionPath
    {
        /// <summary>
        /// Requires the assembly to be strictly below the allowed directory. On Windows,
        /// ordinary and canonical extended drive/UNC names identify the same boundary.
        /// This is a lexical check, not resolution of filesystem links or file identity.
        /// </summary>
        /// <param name="directory">An absolute allowed launcher directory.</param>
        /// <param name="assemblyPath">An absolute discovered extension filename.</param>
        /// <returns>True only for a supported path strictly within the directory.</returns>
        internal static bool IsWithinDirectory(string directory, string assemblyPath)
        {
            string? allowed = ComparisonPath(directory);
            string? extension = ComparisonPath(assemblyPath);
            if (allowed is null || extension is null)
                return false;
            if (!Path.EndsInDirectorySeparator(allowed))
                allowed += Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return extension.Length > allowed.Length && extension.StartsWith(allowed, comparison);
        }

        /// <summary>
        /// Obtains a fully qualified comparison spelling without changing the path used
        /// to load the assembly. Only canonical extended drive and UNC forms are folded
        /// into ordinary names. Device namespaces and extended paths whose dot segments,
        /// trailing dots/spaces or slash spelling would change meaning are rejected.
        /// </summary>
        private static string? ComparisonPath(string path)
        {
            if (!Path.IsPathFullyQualified(path))
                return null;

            if (OperatingSystem.IsWindows())
            {
                if (path.StartsWith(@"\\.\", StringComparison.Ordinal))
                    return null;
                if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
                {
                    if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                        path = string.Concat(@"\\", path.AsSpan(8));
                    else if (path.Length >= 7 && char.IsAsciiLetter(path[4]) && path[5] == ':' && path[6] == '\\')
                        path = path.Substring(4);
                    else
                        return null;

                    if (path.Contains('/') || path.Split('\\').Any(part => part.EndsWith('.') || part.EndsWith(' ')))
                        return null;
                }
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
