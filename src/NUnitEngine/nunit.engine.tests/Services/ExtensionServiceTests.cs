// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1
using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Engine.Extensibility;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services.Tests
{
    public class ExtensionServiceTests
    {
        private ExtensionService _serviceClass;
        private IExtensionService _serviceInterface;

#pragma warning disable 414
        private static readonly string[] KnownExtensionPointPaths = {
            "/NUnit/Engine/TypeExtensions/IDriverFactory",
            "/NUnit/Engine/TypeExtensions/IProjectLoader",
            "/NUnit/Engine/TypeExtensions/IResultWriter",
            "/NUnit/Engine/TypeExtensions/ITestEventListener",
            "/NUnit/Engine/TypeExtensions/IService",
            "/NUnit/Engine/NUnitV2Driver"
        };

        private static readonly Type[] KnownExtensionPointTypes = {
            typeof(IDriverFactory),
            typeof(IProjectLoader),
            typeof(IResultWriter),
            typeof(ITestEventListener),
            typeof(IService),
            typeof(IFrameworkDriver)
        };

        private static readonly int[] KnownExtensionPointCounts = { 1, 1, 1, 2, 1, 1 };
#pragma warning restore 414

        [SetUp]
        public void CreateService()
        {
            _serviceInterface = _serviceClass = new ExtensionService();

            // Rather than actually starting the service, which would result
            // in finding the extensions actually in use on the current system,
            // we simulate the start using this assemblies dummy extensions.
            _serviceClass.FindExtensionPoints(typeof(TestEngine).Assembly);
            _serviceClass.FindExtensionPoints(typeof(ITestEngine).Assembly);

            _serviceClass.FindExtensionsInAssembly(new ExtensionAssembly(GetType().Assembly.Location, false));
        }

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(_serviceInterface.ExtensionPoints.Select(ep => ep.Path), Is.EquivalentTo(KnownExtensionPointPaths));
        }

        [Test, Sequential]
        public void CanGetExtensionPointByPath(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _serviceInterface.GetExtensionPoint(path);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [Test, Sequential]
        public void CanGetExtensionPointByType(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _serviceClass.GetExtensionPoint(type);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

#pragma warning disable 414
        private static readonly string[] KnownExtensions = {
            "NUnit.Engine.Tests.DummyFrameworkDriverExtension",
            "NUnit.Engine.Tests.DummyProjectLoaderExtension",
            "NUnit.Engine.Tests.DummyResultWriterExtension",
            "NUnit.Engine.Tests.DummyEventListenerExtension",
            "NUnit.Engine.Tests.DummyServiceExtension",
            "NUnit.Engine.Tests.V2DriverExtension"
        };
#pragma warning restore 414

        [TestCaseSource(nameof(KnownExtensions))]
        public void CanListExtensions(string typeName)
        {
            Assert.That(_serviceClass.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(typeName)
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test, Sequential]
        public void ExtensionsAreAddedToExtensionPoint(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointCounts))] int extensionCount)
        {
            var ep = _serviceClass.GetExtensionPoint(path);
            Assume.That(ep, Is.Not.Null);

            Assert.That(ep.Extensions.Count, Is.EqualTo(extensionCount));
        }

        [Test]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(_serviceInterface.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            _serviceInterface.EnableExtension("NUnit.Engine.Tests.DummyDisabledExtension", true);

            Assert.That(_serviceInterface.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
            //May be null on mono
            Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP2_0
            string other = "net35"; // Attempt to load the .NET 3.5 version of the extensions from the .NET Core 2.0 tests
#elif NET35
            string other = "netcoreapp2.0"; // Attempt to load the .NET Core 2.0 version of the extensions from the .NET 3.5 tests
#endif
            var assemblyName = Path.Combine(GetSiblingDirectory(other), "nunit.engine.tests.dll");
            Assert.That(assemblyName, Does.Exist);

            var service = new ExtensionService();
            service.FindExtensionPoints(typeof(TestEngine).Assembly);
            service.FindExtensionPoints(typeof(ITestEngine).Assembly);
            var extensionAssembly = new ExtensionAssembly(assemblyName, false);

            Assert.That(() => service.FindExtensionsInAssembly(extensionAssembly), Throws.Nothing);
        }

        [TestCaseSource(nameof(ValidCombos))]
        public void ValidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionService.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.True);
        }

        [TestCaseSource(nameof(InvalidTargetFrameworkCombos))]
        public void InvalidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionService.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.False);
        }

        [TestCaseSource(nameof(InvalidRunnerCombos))]
        public void InvalidRunnerTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionService.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Throws.Exception.TypeOf<NUnitEngineException>().And.Message.Contains("not .NET Standard"));
        }

        // ExtensionAssembly is internal, so cannot be part of the public test parameters
        public struct FrameworkCombo
        {
            internal Assembly RunnerAssembly { get; }
            internal ExtensionAssembly ExtensionAssembly { get; }

            internal FrameworkCombo(Assembly runnerAsm, ExtensionAssembly extensionAsm)
            {
                RunnerAssembly = runnerAsm;
                ExtensionAssembly = extensionAsm;
            }

            public override string ToString() =>
                $"{RunnerAssembly.GetName()}:{ExtensionAssembly.AssemblyName}";
        }

        public static IEnumerable<TestCaseData> ValidCombos()
        {
#if NETCOREAPP2_0
            Assembly netstandard = typeof(ExtensionService).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetStandard)).SetName("ValidCombo(.NET Core, .NET Standard)");
            yield return new TestCaseData(new FrameworkCombo(netcore, extNetCore)).SetName("ValidCombo(.NET Core, .Net Core)");
#else
            Assembly netFramework = typeof(ExtensionService).Assembly;

            var extNetFramework = new ExtensionAssembly(netFramework.Location, false);
            var extNetStandard = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetFramework)).SetName("ValidCombo(.NET Framework, .NET Framework)");
            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetStandard)).SetName("ValidCombo(.NET Framework, .NET Standard)");
#endif
        }

        public static IEnumerable<TestCaseData> InvalidTargetFrameworkCombos()
        {
#if NETCOREAPP2_0
            Assembly netstandard = typeof(ExtensionService).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("net35"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetFramework)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFramework = typeof(ExtensionService).Assembly;

            var extNetStandard = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"), false);
            var extNetCore = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netcoreapp2.0"), "nunit.engine.tests.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetCore)).SetName("InvalidCombo(.NET Framework, .NET Core)");
#endif

        }

        public static IEnumerable<TestCaseData> InvalidRunnerCombos()
        {
#if NETCOREAPP2_0
            Assembly netstandard = typeof(ExtensionService).Assembly;
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("net35"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetStandard)).SetName("InvalidCombo(.NET Standard, .NET Standard)");
            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetCore)).SetName("InvalidCombo(.NET Standard, .NET Core)");
            yield return new TestCaseData(new FrameworkCombo(netstandard, extNetFramework)).SetName("InvalidCombo(.NET Standard, .NET Framework)");
#else
            return new List<TestCaseData>();
#endif
        }

        /// <summary>
        /// Returns a directory in the parent directory that the current test assembly is in. This
        /// is used to load assemblies that target different frameworks than the current tests. So
        /// if these tests are in bin\release\net35 and dir is netstandard2.0, this will return
        /// bin\release\netstandard2.0.
        /// </summary>
        /// <param name="dir">The sibling directory</param>
        /// <returns></returns>
        private static string GetSiblingDirectory(string dir)
        {
            var file = new FileInfo(typeof(ExtensionServiceTests).Assembly.Location);
            return Path.Combine(file.Directory.Parent.FullName, dir);
        }
    }
}
#endif
