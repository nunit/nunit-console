// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.IO;
using NUnit.Engine.TestHelpers;

namespace NUnit.Engine.Integration
{
    internal sealed class DirectoryWithNeededAssemblies : IDisposable
    {
        public string Directory { get; }

        /// <summary>
        /// Returns the transitive closure of assemblies needed to copy.
        /// Deals with assembly names rather than paths to work with runners that shadow copy.
        /// </summary>
        public DirectoryWithNeededAssemblies(params string[] assemblyNames)
        {
            Directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            System.IO.Directory.CreateDirectory(Directory);

            foreach (var neededAssembly in ShadowCopyUtils.GetAllNeededAssemblyPaths(assemblyNames))
            {
                File.Copy(neededAssembly, Path.Combine(Directory, Path.GetFileName(neededAssembly)));
            }
        }

        public void Dispose()
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }
}
#endif
