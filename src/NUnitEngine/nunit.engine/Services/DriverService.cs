// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Common;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The DriverService provides drivers able to load and run tests
    /// using various frameworks.
    /// </summary>
    public class DriverService : Service, IDriverService
    {
        readonly IList<IDriverFactory> _factories = new List<IDriverFactory>();

        #region IDriverService Members

        /// <summary>
        /// Get a driver suitable for use with a particular test assembly.
        /// </summary>
        /// <param name="domain">The application domain to use for the tests</param>
        /// <param name="assemblyPath">The full path to the test assembly</param>
        /// <param name="targetFramework">The value of any TargetFrameworkAttribute on the assembly, or null</param>
        /// <param name="skipNonTestAssemblies">True if non-test assemblies should simply be skipped rather than reporting an error</param>
        /// <returns></returns>
#if NETSTANDARD1_6
        public IFrameworkDriver GetDriver(string assemblyPath, bool skipNonTestAssemblies)
#else
        public IFrameworkDriver GetDriver(AppDomain domain, string assemblyPath, string targetFramework, bool skipNonTestAssemblies)
#endif
        {
            if (!File.Exists(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, "File not found: " + assemblyPath);

            if (!PathUtils.IsAssemblyFileType(assemblyPath))
                return new InvalidAssemblyFrameworkDriver(assemblyPath, "File type is not supported");

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
            if (targetFramework != null)
            {
                // This takes care of an issue with Roslyn. It may get fixed, but we still
                // have to deal with assemblies having this setting. I'm assuming that
                // any true Portable assembly would have a Profile as part of its name.
                var platform = targetFramework == ".NETPortable,Version=v5.0"
                    ? ".NETStandard"
                    : targetFramework.Split(new char[] { ',' })[0];
                if (platform == "Silverlight" || platform == ".NETPortable" || platform == ".NETStandard" || platform == ".NETCompactFramework")
                    return new InvalidAssemblyFrameworkDriver(assemblyPath, platform + " test assemblies are not supported by this version of the engine");
            }
#endif

            try
            {
                var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath);

                if (skipNonTestAssemblies)
                {
                    foreach (var attr in assemblyDef.CustomAttributes)
                        if (attr.AttributeType.FullName == "NUnit.Framework.NonTestAssemblyAttribute")
                            return new SkippedAssemblyFrameworkDriver(assemblyPath);
                }

                var references = new List<AssemblyName>();
                foreach (var cecilRef in assemblyDef.MainModule.AssemblyReferences)
                    references.Add(new AssemblyName(cecilRef.FullName));

                foreach (var factory in _factories)
                {
                    foreach (var reference in references)
                    {
                        if (factory.IsSupportedTestFramework(reference))
#if NETSTANDARD1_6 || NETSTANDARD2_0
                            return factory.GetDriver(reference);
#else
                            return factory.GetDriver(domain, reference);
#endif
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                return new InvalidAssemblyFrameworkDriver(assemblyPath, ex.Message);
            }

            if (skipNonTestAssemblies)
                return new SkippedAssemblyFrameworkDriver(assemblyPath);
            else
                return new InvalidAssemblyFrameworkDriver(assemblyPath, string.Format("No suitable tests found in '{0}'.\n" +
                                                                              "Either assembly contains no tests or proper test driver has not been found.", assemblyPath));
        }

        #endregion

        #region Service Overrides

        public override void StartService()
        {
            Guard.OperationValid(ServiceContext != null, "Can't start DriverService outside of a ServiceContext");

            try
            {
#if NET20 || NETSTANDARD2_0
                var extensionService = ServiceContext.GetService<ExtensionService>();
                if (extensionService != null)
                {
                    foreach (IDriverFactory factory in extensionService.GetExtensions<IDriverFactory>())
                        _factories.Add(factory);

#if NET20
                    var node = extensionService.GetExtensionNode("/NUnit/Engine/NUnitV2Driver");
                    if (node != null)
                        _factories.Add(new NUnit2DriverFactory(node));
#endif
                }
#endif

                _factories.Add(new NUnit3DriverFactory());

                Status = ServiceStatus.Started;
            }
            catch(Exception)
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        #endregion
    }
}
