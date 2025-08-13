// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using TestCentric.Metadata;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// The DriverService provides drivers able to load and run tests
    /// using various frameworks.
    /// </summary>
    public class DriverService : IDriverService
    {
#if NETFRAMEWORK
        private const string TYPE_EXTENSION_PATH = "/NUnit/Engine/TypeExtensions/";

        private static readonly Assembly THIS_ASSEMBLY = typeof(DriverService).Assembly;
        private static readonly bool RUNNING_UNDER_CHOCOLATEY =
            System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(THIS_ASSEMBLY.Location)!, "VERIFICATION.txt"));
        private static readonly string PACKAGE_PREFIX = RUNNING_UNDER_CHOCOLATEY ? "nunit-extension-" : "NUnit.Extension.";
#endif

        private static readonly Logger log = InternalTrace.GetLogger("DriverService");

        private static readonly char[] CommaSeparator = [','];

        private readonly List<IDriverFactory> _factories = new List<IDriverFactory>();

        public DriverService()
        {
            _factories.Add(new NUnit3DriverFactory());

#if NETFRAMEWORK // TODO: Restore extensibility to .NET 8.0 build
            var extensionManager = new ExtensionManager()
            {
                TypeExtensionPath = TYPE_EXTENSION_PATH,
                PackagePrefixes = [PACKAGE_PREFIX]
            };

            extensionManager.FindExtensionPoints(THIS_ASSEMBLY);
            extensionManager.FindExtensionAssemblies(THIS_ASSEMBLY);

            foreach (IDriverFactory factory in extensionManager.GetExtensions<IDriverFactory>())
                _factories.Add(factory);

            var node = extensionManager.GetExtensionNode("/NUnit/Engine/NUnitV2Driver");
            if (node is not null)
                _factories.Add(new NUnit2DriverFactory(node));
#endif
        }

        /// <summary>
        /// Get a driver suitable for use with a particular test assembly.
        /// </summary>
        /// <param name="domain">The application domain to use for the tests</param>
        /// <param name="assemblyPath">The full path to the test assembly</param>
        /// <param name="targetFramework">The value of any TargetFrameworkAttribute on the assembly, or null</param>
        /// <param name="skipNonTestAssemblies">True if non-test assemblies should simply be skipped rather than reporting an error</param>
        public IFrameworkDriver GetDriver(AppDomain domain, TestPackage package, string assemblyPath, string? targetFramework, bool skipNonTestAssemblies)
        {
            string InternalErrorMessage(string message) =>
                $"Internal Error: {message} {assemblyPath}";

            Guard.ArgumentValid(File.Exists(assemblyPath), InternalErrorMessage("File not found"), nameof(assemblyPath));
            Guard.ArgumentValid(PathUtils.IsAssemblyFileType(assemblyPath), InternalErrorMessage("Not an assembly type"), nameof(assemblyPath));

            log.Debug("Looking for a driver");
            using (var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                if (skipNonTestAssemblies)
                {
                    // TODO: We should not get here since the engine should have eliminated any
                    // such assemblies. Double-check for now but remove before the next release.
                    foreach (var attr in assemblyDef.CustomAttributes)
                        if (attr.AttributeType.FullName == "NUnit.Framework.NonTestAssemblyAttribute")
                            throw new InvalidOperationException(InternalErrorMessage("Assembly should have been skipped"));
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

            return new InvalidAssemblyFrameworkDriver(assemblyPath, package.ID,
                $"No suitable test driver has not been found for {assemblyPath}");
        }
    }
}
