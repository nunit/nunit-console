// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Remote.Deployment
{
    /// <summary>
    /// Adapts the prototype's ordinary-path, containment and ownership checks.
    /// Link resolution is deliberately rejected for this local deployment candidate.
    /// </summary>
    internal static class DeploymentPaths
    {
        private static readonly SearchValues<char> InvalidCharacters = SearchValues.Create(":*?\"<>|");
        private static StringComparison Comparison => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        /// <summary>Requires an explicit root and rejects reparse points in every existing ancestor.</summary>
        internal static string Root(string path)
        {
            if (!Path.IsPathFullyQualified(path))
                throw new ArgumentException("An absolute deployment input path is required.", nameof(path));
            string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            EnsureOrdinary(full);
            return full;
        }

        /// <summary>Includes the root itself when checking overlap; callers use Child for strict containment.</summary>
        internal static bool Contains(string root, string path) =>
            path.Equals(root, Comparison) || path.StartsWith(root + Path.DirectorySeparatorChar, Comparison);

        /// <summary>Accepts portable relative manifest names only, rejecting aliases and traversal.</summary>
        internal static string Child(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || relative.Contains('\\') || Path.IsPathRooted(relative))
                throw new IOException("A portable relative payload path is required.");
            foreach (string part in relative.Split('/'))
            {
                if (part.Length == 0 || part == "." || part == ".." || part.EndsWith(' ') || part.EndsWith('.')
                    || part.AsSpan().IndexOfAny(InvalidCharacters) >= 0)
                    throw new IOException("The payload path contains an ambiguous or escaping segment.");
                foreach (char character in part)
                    if (char.IsControl(character))
                        throw new IOException("Control characters are not valid payload paths.");
            }
            string child = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!Contains(root, child) || child.Equals(root, Comparison))
                throw new IOException("The payload path escapes its root.");
            EnsureOrdinary(child);
            return child;
        }

        /// <summary>Checks every existing ancestor without following filesystem links.</summary>
        internal static void EnsureOrdinary(string path)
        {
            string? current = Path.GetFullPath(path);
            while (current is not null)
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Reparse paths are outside the session deployment contract: " + current);
                }
                catch (FileNotFoundException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }
                current = Path.GetDirectoryName(current);
            }
        }

        /// <summary>Enumerates files only after checking each entry, so recursion never follows a link.</summary>
        internal static IEnumerable<string> Files(string directory)
        {
            EnsureOrdinary(directory);
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                EnsureOrdinary(entry);
                if (Directory.Exists(entry))
                {
                    foreach (string file in Files(entry))
                        yield return file;
                }
                else
                    yield return entry;
            }
        }

        /// <summary>Verifies both generated directory identity and its independent ownership marker.</summary>
        internal static void EnsureOwned(string root, string directory, string owner)
        {
            if (Path.GetDirectoryName(directory) is not string parent || !parent.Equals(root, Comparison)
                || Path.GetFileName(directory) != "nunit-" + owner)
                throw new IOException("Cleanup is outside the generated session directory.");
            EnsureOrdinary(directory);
            string marker = Child(directory, "owner.txt");
            if (File.ReadAllText(marker) != owner)
                throw new IOException("The deployment ownership marker changed; retain the files.");
        }
    }
}
