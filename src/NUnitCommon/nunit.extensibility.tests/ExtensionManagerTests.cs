// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Net;

namespace NUnit.Extensibility
{
    [TestFixture("/NUnit/Engine/TypeExtensions/")]
    public class ExtensionManagerTests
    {
        private static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionManagerTests).Assembly;
        private static readonly string THIS_ASSEMBLY_DIRECTORY = Path.GetDirectoryName(THIS_ASSEMBLY.Location)!;
        private const string FAKE_EXTENSIONS_FILENAME = "FakeExtensions.dll";
        private static readonly string FAKE_EXTENSIONS_PARENT_DIRECTORY =
#if NETFRAMEWORK
            Path.Combine(new DirectoryInfo(THIS_ASSEMBLY_DIRECTORY).Parent!.FullName, "fakesv2/net462");
#else
            Path.Combine(new DirectoryInfo(THIS_ASSEMBLY_DIRECTORY).Parent!.FullName, "fakesv2/netstandard2.0");
#endif

        private const string FAKE_AGENT_LAUNCHER_EXTENSION = "NUnit.Engine.Fakes.FakeAgentLauncherExtension";
        private const string FAKE_FRAMEWORK_DRIVER_EXTENSION = "NUnit.Engine.Fakes.FakeFrameworkDriverExtension";
        private const string FAKE_PROJECT_LOADER_EXTENSION = "NUnit.Engine.Fakes.FakeProjectLoaderExtension";
        private const string FAKE_RESULT_WRITER_EXTENSION = "NUnit.Engine.Fakes.FakeResultWriterExtension";
        private const string FAKE_EVENT_LISTENER_EXTENSION = "NUnit.Engine.Fakes.FakeEventListenerExtension";
        private const string FAKE_SERVICE_EXTENSION = "NUnit.Engine.Fakes.FakeServiceExtension";
        private const string FAKE_DISABLED_EXTENSION = "NUnit.Engine.Fakes.FakeDisabledExtension";
        private const string FAKE_NUNIT_V2_DRIVER_EXTENSION = "NUnit.Engine.Fakes.V2DriverExtension";
        private const string FAKE_EXTENSION_WITH_NO_EXTENSION_POINT = "NUnit.Engine.Fakes.FakeExtension_NoExtensionPointFound";

        private readonly string[] KnownExtensions =
        {
            FAKE_AGENT_LAUNCHER_EXTENSION,
            FAKE_FRAMEWORK_DRIVER_EXTENSION,
            FAKE_PROJECT_LOADER_EXTENSION,
            FAKE_RESULT_WRITER_EXTENSION,
            FAKE_EVENT_LISTENER_EXTENSION,
            FAKE_SERVICE_EXTENSION,
            FAKE_DISABLED_EXTENSION,
            //FAKE_NUNIT_V2_DRIVER_EXTENSION,
            FAKE_EXTENSION_WITH_NO_EXTENSION_POINT
        };

        private ExtensionManager _extensionManager;
        private string _defaultTestExtensionPath;

        private string[] _expectedExtensionPointPaths;
        private Type[] _expectedExtensionPointTypes;
        private int[] _expectedExtensionCounts;

        public ExtensionManagerTests(string defaultTestExtensionPath)
        {
            Guard.ArgumentValid(
                defaultTestExtensionPath.StartsWith('/') && defaultTestExtensionPath.EndsWith('/'),
                "Path must start and end with '/'", nameof(defaultTestExtensionPath));

            _defaultTestExtensionPath = defaultTestExtensionPath;
            var prefix = defaultTestExtensionPath ?? "/NUnit/Extensibility/TypeExtensions/";

            _expectedExtensionPointPaths =
            [
                prefix + "IAgentLauncher",
                prefix + "IDriverFactory",
                prefix + "IProjectLoader",
                prefix + "IResultWriter",
                prefix + "ITestEventListener",
                prefix + "IService",
                "/NUnit/Engine/NUnitV2Driver"
            ];

            _expectedExtensionPointTypes =
            [
                typeof(IAgentLauncher),
                typeof(IDriverFactory),
                typeof(IProjectLoader),
                typeof(IResultWriter),
                typeof(ITestEventListener),
                typeof(IService),
                typeof(IFrameworkDriver)
            ];

            _expectedExtensionCounts = [1, 1, 1, 1, 2, 1, 0];
        }

        [SetUp]
        public void CreateExtensionManager()
        {
            _extensionManager = new ExtensionManager() { TypeExtensionPath = _defaultTestExtensionPath };

            // Find actual extension points.
            _extensionManager.FindExtensionPoints(typeof(ExtensionManager).Assembly);
            _extensionManager.FindExtensionPoints(typeof(ITestEngine).Assembly);

            // Find Fake Extensions using alternate start directory
            _extensionManager.FindExtensionAssemblies(FAKE_EXTENSIONS_PARENT_DIRECTORY);
            _extensionManager.InstallExtensions();
        }

        [Test]
        public void AllKnownExtensionPointsAreFound()
        {
            Assert.That(_extensionManager.ExtensionPoints.Select(ep => ep.Path),
                Is.EquivalentTo(_expectedExtensionPointPaths));
        }

        [Test]
        public void AllKnownExtensionsAreFound()
        {
            Assert.That(_extensionManager.Extensions.Select(ext => ext.TypeName),
                Is.EquivalentTo(KnownExtensions));
        }

        [Test]
        public void AllExtensionsUseTheLatestVersion()
        {
            // We have two builds of FakeExtensions. Version 2
            // should be used rather than 1 for all extensions.
            foreach (var node in _extensionManager.Extensions)
                Assert.That(node.AssemblyVersion.ToString(), Is.EqualTo("2.0.0.0"));
        }

        [Test]
        public void AllExtensionsHaveCorrectStatus()
        {
            foreach (var node in _extensionManager.Extensions)
            {
                var expectedStatus = node.TypeName == FAKE_EXTENSION_WITH_NO_EXTENSION_POINT
                    ? ExtensionStatus.Unknown
                    : ExtensionStatus.Unloaded;
                Assert.That(node.Status, Is.EqualTo(expectedStatus));
            }
        }

        [Test]
        public void AllKnownExtensionsAreEnabledAsRequired()
        {
            foreach (var node in _extensionManager.Extensions)
            {
                var shouldBeEnabled = node.TypeName != FAKE_DISABLED_EXTENSION;
                Assert.That(node.Enabled, Is.EqualTo(shouldBeEnabled));
            }
        }

        [Test]
        public void CanGetExtensionPointsByPath()
        {
            for (int i = 0; i < _expectedExtensionPointPaths.Length; i++)
            {
                var path = _expectedExtensionPointPaths[i];
                var type = _expectedExtensionPointTypes[i];
                var ep = _extensionManager.GetExtensionPoint(path);
                Assert.That(ep, Is.Not.Null);
                Assert.That(ep.Path, Is.EqualTo(path));
                Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
            }
        }

        [Test]
        public void CanGetExtensionPointByType()
        {
            for (int i = 0; i < _expectedExtensionPointPaths.Length; i++)
            {
                var path = _expectedExtensionPointPaths[i];
                var type = _expectedExtensionPointTypes[i];
                var ep = _extensionManager.GetExtensionPoint(type);
                Assert.That(ep, Is.Not.Null);
                Assert.That(ep.Path, Is.EqualTo(path));
                Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
            }
        }

        [Test]
        public void ExtensionsAreAddedToExtensionPoint()
        {
            for (int i = 0; i < _expectedExtensionPointPaths.Length; i++)
            {
                var path = _expectedExtensionPointPaths[i];
                var extensionCount = _expectedExtensionCounts[i];
                var ep = _extensionManager.GetExtensionPoint(path);
                Assume.That(ep, Is.Not.Null);
                Assert.That(ep.Extensions.Count, Is.EqualTo(extensionCount));
            }
        }

        [Test]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(FAKE_DISABLED_EXTENSION)
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            _extensionManager.EnableExtension(FAKE_DISABLED_EXTENSION, true);

            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(FAKE_DISABLED_EXTENSION)
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
            //May be null on mono
            Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP
            // Attempt to load the .NET 4.6.2 version of the extensions from the .NET 8.0 tests
            var assemblyName = Path.Combine(GetSiblingDirectory("net462"), "nunit.extensibility.tests.exe");
#else
            // Attempt to load the .NET 8.0 version of the extensions from the .NET 4.6.2 tests
            var assemblyName = Path.Combine(GetSiblingDirectory("net8.0"), "nunit.extensibility.tests.dll");
#endif
            Assert.That(assemblyName, Does.Exist);
            Console.WriteLine($"{assemblyName} does exist");

            var service = new ExtensionManager();
            service.FindExtensionPoints(typeof(ExtensionManager).Assembly);
            service.FindExtensionPoints(typeof(ITestEngine).Assembly);
            var extensionAssembly = new ExtensionAssembly(assemblyName, false);

            Assert.That(() => service.FindExtensionsInAssembly(extensionAssembly), Throws.Nothing);
        }

#if NETCOREAPP
        [TestCase("netstandard2.0", ExpectedResult = true)]
        //[TestCase("net462", ExpectedResult = false)]
        //[TestCase("net20", ExpectedResult = false)]
#elif NET40_OR_GREATER
        //[TestCase("netstandard2.0", ExpectedResult = false)]
        [TestCase("net462", ExpectedResult = true)]
        //[TestCase("net20", ExpectedResult = true)]
#else
        //[TestCase("netstandard2.0", ExpectedResult = false)]
        //[TestCase("net462", ExpectedResult = false)]
        //[TestCase("net20", ExpectedResult = true)]
#endif
        public bool LoadTargetFramework(string tfm)
        {
            return _extensionManager.CanLoadTargetFramework(THIS_ASSEMBLY, FakeExtensions());
        }

        //[TestCaseSource(nameof(ValidCombos))]
        public void ValidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => _extensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.True);
        }

        //[TestCaseSource(nameof(InvalidTargetFrameworkCombos))]
        public void InvalidTargetFrameworkCombinations(FrameworkCombo combo)
        {
            Assert.That(() => _extensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly),
                Is.False);
        }

        //[TestCaseSource(nameof(InvalidRunnerCombos))]
        public void InvalidRunnerTargetFrameworkCombinations(FrameworkCombo combo)
        {
            var ex = Assert.Catch(() => _extensionManager.CanLoadTargetFramework(combo.RunnerAssembly, combo.ExtensionAssembly));
            Assert.That(ex.Message.Contains("not .NET Standard"));
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
            Assembly netFramework = typeof(ExtensionManager).Assembly;

            var extNetFramework = new ExtensionAssembly(netFramework.Location, false);
            var extNetStandard = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netFramework, extNetFramework)).SetName("ValidCombo(.NET Framework, .NET Framework)");
            //yield return new TestCaseData(new FrameworkCombo(netFramework, extNetStandard)).SetName("ValidCombo(.NET Framework, .NET Standard)");
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
            Assembly netFramework = typeof(ExtensionManager).Assembly;

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
        private static string GetSiblingDirectory(string dir)
        {
            var file = new FileInfo(typeof(ExtensionManagerTests).Assembly.Location);
            return Path.Combine(file.Directory!.Parent!.FullName, dir);
        }

        /// <summary>
        /// Returns an ExtensionAssembly referring to a particular build of the fake test extensions
        /// assembly based on the argument provided.
        /// </summary>
        /// <param name="tfm">A test framework moniker. Must be one for which the fake extensions are built.</param>
        private static ExtensionAssembly FakeExtensions()
        {
            return new ExtensionAssembly(
                Path.Combine(FAKE_EXTENSIONS_PARENT_DIRECTORY, FAKE_EXTENSIONS_FILENAME), false);
        }
    }
}
