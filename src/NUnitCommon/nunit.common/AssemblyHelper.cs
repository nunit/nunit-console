// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
namespace NUnit
{
    /// <summary>
    /// AssemblyHelper provides static methods for working
    /// with assemblies.
    /// </summary>
    public static class AssemblyHelper
    {
        /// <summary>
        /// Gets the path to the directory from which an assembly was loaded.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The path.</returns>
        public static string GetDirectoryName(Assembly assembly)
        {
            return Path.GetDirectoryName(GetAssemblyPath(assembly))!;
        }

        /// <summary>
        /// Gets the path from which an assembly was loaded.
        /// For builds where this is not possible, returns
        /// the name of the assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The path.</returns>
        public static string GetAssemblyPath(Assembly assembly)
        {
#if NETFRAMEWORK
            // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.location
            // .NET Framework only:
            // If the loaded file was shadow-copied, the location is that of the file after being shadow-copied.
            // To get the location before the file has been shadow-copied, use the CodeBase property.
            string? codeBase = assembly.CodeBase;

            if (codeBase is not null && IsFileUri(codeBase))
                return GetAssemblyPathFromCodeBase(codeBase);
#endif

            return assembly.Location;
        }

#if NETFRAMEWORK
        private static bool IsFileUri(string uri)
        {
            return uri.StartsWith(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the assembly path from code base.
        /// </summary>
        /// <remarks>Public for testing purposes</remarks>
        /// <param name="codeBase">The code base.</param>
        /// <returns></returns>
        public static string GetAssemblyPathFromCodeBase(string codeBase)
        {
            // Skip over the file:// part
            int start = Uri.UriSchemeFile.Length + Uri.SchemeDelimiter.Length;

            if (codeBase[start] == '/') // third slash means a local path
            {
                // Handle Windows Drive specifications
                if (codeBase[start + 2] == ':')
                    ++start;
                // else leave the last slash so path is absolute
            }
            else // It's either a Windows Drive spec or a share
            {
                if (codeBase[start + 1] != ':')
                    start -= 2; // Back up to include two slashes
            }

            return codeBase.Substring(start);
        }
#endif
    }
}
