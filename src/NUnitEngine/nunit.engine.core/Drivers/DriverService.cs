// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using TestCentric.Metadata;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// The DriverService provides drivers able to load and run tests
    /// using various frameworks.
    /// </summary>
    public class DriverService : IDriverService
    {
        static readonly ILogger log = InternalTrace.GetLogger("DriverService");

        readonly IList<IDriverFactory> _factories = new List<IDriverFactory>();

        public DriverService()
        {
#if NETFRAMEWORK // TODO: Restore extensibility to .NET 8.0 build
            //var thisAssembly = Assembly.GetExecutingAssembly();
            //var extensionManager = new ExtensionManager();

            //extensionManager.FindExtensionPoints(thisAssembly);
            //extensionManager.FindExtensionAssemblies(thisAssembly);

            //foreach (IDriverFactory factory in extensionManager.GetExtensions<IDriverFactory>())
            //    _factories.Add(factory);

            //// HACK
            //var node = extensionManager.GetExtensionNode("/NUnit/Engine/NUnitV2Driver") as ExtensionNode;
            //if (node != null)
            //    _factories.Add(new NUnit2DriverFactory(node));
#endif

            _factories.Add(new NUnit3DriverFactory());
        }

        /// <summary>
        /// Get a driver suitable for use with a particular test assembly.
        /// </summary>
        /// <param name="domain">The application domain to use for the tests</param>
        /// <param name="assemblyPath">The full path to the test assembly</param>
        /// <param name="targetFramework">The value of any TargetFrameworkAttribute on the assembly, or null</param>
        /// <param name="skipNonTestAssemblies">True if non-test assemblies should simply be skipped rather than reporting an error</param>
        /// <returns></returns>
        public IFrameworkDriver GetDriver(AppDomain domain, TestPackage package, string assemblyPath, string? targetFramework, bool skipNonTestAssemblies)
        {
            if (!File.Exists(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID, "File not found: " + assemblyPath);

            if (!PathUtils.IsAssemblyFileType(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID, "File type is not supported");

            if (targetFramework != null)
            {
                // This takes care of an issue with Roslyn. It may get fixed, but we still
                // have to deal with assemblies having this setting. I'm assuming that
                // any true Portable assembly would have a Profile as part of its name.
                var platform = targetFramework == ".NETPortable,Version=v5.0"
                    ? ".NETStandard"
                    : targetFramework.Split(new char[] { ',' })[0];

                if (platform == "Silverlight" || platform == ".NETPortable" || platform == ".NETStandard" || platform == ".NETCompactFramework")
                    if (skipNonTestAssemblies)
                        return new SkippedAssemblyFrameworkDriver(assemblyPath, "X");
                    else
                        return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID, platform + 
                            " test assemblies are not supported by this version of the engine");
            }

            try
            {
                using (var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath))
                {
                    if (skipNonTestAssemblies)
                    {
                        foreach (var attr in assemblyDef.CustomAttributes)
                            if (attr.AttributeType.FullName == "NUnit.Framework.NonTestAssemblyAttribute")
                                return new SkippedAssemblyFrameworkDriver(assemblyPath, package.ID);
                    }

                    foreach (var factory in _factories)
                    {
                        log.Debug($"Trying {factory.GetType().Name}");

                        foreach (var cecilRef in assemblyDef.MainModule.AssemblyReferences)
                        {
                            var assemblyName = new AssemblyName(cecilRef.FullName);
                            if (factory.IsSupportedTestFramework(assemblyName))
                            {
#if NETFRAMEWORK
                                return factory.GetDriver(domain, package.ID, assemblyName);
#else
                                return factory.GetDriver(package.ID, assemblyName);
#endif
                            }
                        }
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID, ex.Message);
            }

            if (skipNonTestAssemblies)
                return new SkippedAssemblyFrameworkDriver(assemblyPath, package.ID);
            else
                return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID, 
                    $"No suitable tests found in '{assemblyPath}'.\r\nEither assembly contains no tests or proper test driver has not been found.");
        }
    }
}
