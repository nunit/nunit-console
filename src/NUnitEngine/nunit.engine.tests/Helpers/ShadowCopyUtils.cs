// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NUnit.Engine.TestHelpers
{
    public static class ShadowCopyUtils
    {
        /// <summary>
        /// Returns the transitive closure of assemblies needed to copy.
        /// Deals with assembly names rather than paths to work with runners that shadow copy.
        /// </summary>
        public static ICollection<string> GetAllNeededAssemblyPaths(params string[] assemblyNames)
        {
            var r = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dependencies = StackEnumerator.Create(
                from assemblyName in assemblyNames
                select new AssemblyName(assemblyName));

            foreach (var dependencyName in dependencies)
            {
#if NETFRAMEWORK
                var dependency = Assembly.ReflectionOnlyLoad(dependencyName.FullName);
                if (dependency.GlobalAssemblyCache)
                    continue;
#else
                // ReflectionOnlyLoad isn't available on .NET Core / .NET 5+. Load the assembly into the
                // default context for inspection instead, and skip assemblies without a physical location.
                var dependency = Assembly.Load(dependencyName);
#endif
                var location = dependency.Location;
                if (string.IsNullOrEmpty(location) && r.Add(Path.GetFullPath(location)))
                    dependencies.Recurse(dependency.GetReferencedAssemblies());
            }

            return r;
        }
    }
}
