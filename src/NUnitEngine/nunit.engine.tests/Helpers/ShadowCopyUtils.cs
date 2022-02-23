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
                var dependency = Assembly.ReflectionOnlyLoad(dependencyName.FullName);

#if NET5_0_OR_GREATER
                if (r.Add(Path.GetFullPath(dependency.Location)))
#else
                if (!dependency.GlobalAssemblyCache && r.Add(Path.GetFullPath(dependency.Location)))
#endif
                {
                    dependencies.Recurse(dependency.GetReferencedAssemblies());
                }
            }

            return r;
        }
    }
}
