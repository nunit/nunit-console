// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Engine.Extensibility;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Engine.Internal;
using NSubstitute;
using NUnit.Engine.Internal.FileSystemAccess;

namespace NUnit.Engine.Services.Tests
{
    public class ExtensionManagerTests
    {
        private ExtensionManager _extensionManager;

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

        private readonly string[] KnownExtensions = {
            "NUnit.Engine.Tests.DummyFrameworkDriverExtension",
            "NUnit.Engine.Tests.DummyProjectLoaderExtension",
            "NUnit.Engine.Tests.DummyResultWriterExtension",
            "NUnit.Engine.Tests.DummyEventListenerExtension",
            "NUnit.Engine.Tests.DummyServiceExtension",
            "NUnit.Engine.Tests.DummyDisabledExtension",
            "NUnit.Engine.Tests.V2DriverExtension"
        };
#pragma warning restore 414

        [SetUp]
        public void CreateExtensionManager()
        {
            _extensionManager = new ExtensionManager();

            // Find actual extension points.
            _extensionManager.FindExtensionPoints(typeof(CoreEngine).Assembly);
            _extensionManager.FindExtensionPoints(typeof(ITestEngine).Assembly);

            // Find dummy extensions in this test assembly.
            _extensionManager.FindExtensionsInAssembly(new ExtensionAssembly(GetType().Assembly.Location, false));
        }

        [Test]
        public void AllKnownExtensionPointsAreFound()
        {
            Assert.That(_extensionManager.ExtensionPoints.Select(ep => ep.Path), 
                Is.EquivalentTo(KnownExtensionPointPaths));
        }

        [Test]
        public void AllKnownExtensionsAreFound()
        {
            Assert.That(_extensionManager.Extensions.Select(ext => ext.TypeName),
                Is.EquivalentTo(KnownExtensions));
        }

        [Test]
        public void AllKnownExtensionsAreEnabledAsRequired()
        {
            foreach (var node in _extensionManager.Extensions)
            {
                var shouldBeEnabled = node.TypeName != "NUnit.Engine.Tests.DummyDisabledExtension";
                Assert.That(node.Enabled, Is.EqualTo(shouldBeEnabled));
            }
        }

        [Test, Sequential]
        public void CanGetExtensionPointByPath(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _extensionManager.GetExtensionPoint(path);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [Test, Sequential]
        public void CanGetExtensionPointByType(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _extensionManager.GetExtensionPoint(type);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [Test, Sequential]
        public void ExtensionsAreAddedToExtensionPoint(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointCounts))] int extensionCount)
        {
            var ep = _extensionManager.GetExtensionPoint(path);
            Assume.That(ep, Is.Not.Null);

            Assert.That(ep.Extensions.Count, Is.EqualTo(extensionCount));
        }

        [Test]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            _extensionManager.EnableExtension("NUnit.Engine.Tests.DummyDisabledExtension", true);

            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
            //May be null on mono
            Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP
            // Attempt to load the .NET 3.5 version of the extensions from the .NET Core 2.0 tests
            var assemblyName = Path.Combine(GetSiblingDirectory("net462"), "nunit.engine.core.tests.exe");
#else
            // Attempt to load the .NET Core 3.1 version of the extensions from the .NET 3.5 tests
            var assemblyName = Path.Combine(GetSiblingDirectory("netcoreapp3.1"), "nunit.engine.core.tests.dll");
#endif
            Assert.That(assemblyName, Does.Exist);
            Console.WriteLine($"{assemblyName} does exist");

            var service = new ExtensionManager();
            service.FindExtensionPoints(typeof(CoreEngine).Assembly);
            service.FindExtensionPoints(typeof(ITestEngine).Assembly);
            var extensionAssembly = new ExtensionAssembly(assemblyName, false);

            Assert.That(() => service.FindExtensionsInAssembly(extensionAssembly), Throws.Nothing);
        }

        [TestCaseSource(nameof(ValidCombos))]
        public void ValidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.True);
        }

        [TestCaseSource(nameof(InvalidTargetFrameworkCombos))]
        public void InvalidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.False);
        }

        [TestCaseSource(nameof(InvalidRunnerCombos))]
        public void InvalidRunnerTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => ExtensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
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
#if NETCOREAPP
            Assembly netstandard =
                Assembly.LoadFile(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"));
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
#if NETCOREAPP
            Assembly netstandard =
                Assembly.LoadFile(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"));
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("net462"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetFramework)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFramework = typeof(ExtensionService).Assembly;

            var extNetStandard = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"), false);
            var extNetCore = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netcoreapp3.1"), "nunit.engine.tests.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetCore)).SetName("InvalidCombo(.NET Framework, .NET Core)");
#endif

        }

        public static IEnumerable<TestCaseData> InvalidRunnerCombos()
        {
#if NETCOREAPP
            Assembly netstandard = Assembly.LoadFile(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"));
            Assembly netcore = Assembly.GetExecutingAssembly();

            var extNetStandard = new ExtensionAssembly(netstandard.Location, false);
            var extNetCore = new ExtensionAssembly(netcore.Location, false);
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("net462"), "nunit.engine.dll"), false);

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
        /// if these tests are in bin\release\net462 and dir is netstandard2.0, this will return
        /// bin\release\netstandard2.0.
        /// </summary>
        /// <param name="dir">The sibling directory</param>
        /// <returns></returns>
        private static string GetSiblingDirectory(string dir)
        {
            var file = new FileInfo(typeof(ExtensionManagerTests).Assembly.Location);
            return Path.Combine(file.Directory.Parent.FullName, dir);
        }
    }
}
