﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Engine.Extensibility;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Engine.Drivers;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using NSubstitute;

namespace NUnit.Engine.Services
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
#pragma warning restore 414

        [SetUp]
        public void CreateService()
        {
            _extensionManager = new ExtensionManager();

            // Rather than actually starting the service, which would result
            // in finding the extensions actually in use on the current system,
            // we simulate the start using this assemblies dummy extensions.
            _extensionManager.FindExtensionPoints(typeof(ExtensionManager).Assembly);
            _extensionManager.FindExtensionPoints(typeof(ITestEngine).Assembly);

            _extensionManager.FindExtensionsInAssembly(new ExtensionAssembly(GetType().Assembly.Location, false));
        }

        [Test]
        public void StartService_UseFileSystemAbstraction()
        {
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var fileSystem = Substitute.For<IFileSystem>();
            var service = new ExtensionManager(addinsReader, fileSystem);
            var workingDir = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);

            Initialize(service);

            fileSystem.Received().GetDirectory(workingDir);
        }

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(_extensionManager.ExtensionPoints.Select(ep => ep.Path), Is.EquivalentTo(KnownExtensionPointPaths));
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

        private static readonly string[] KnownExtensions = {
            typeof(DummyFrameworkDriverExtension).FullName,
            typeof(DummyProjectLoaderExtension).FullName,
            typeof(DummyResultWriterExtension).FullName,
            typeof(DummyEventListenerExtension).FullName,
            typeof(DummyServiceExtension).FullName,
            typeof(V2DriverExtension).FullName
        };

        [TestCaseSource(nameof(KnownExtensions))]
        public void CanListExtensions(string typeName)
        {
            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(typeName)
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
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
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(typeof(DummyDisabledExtension).FullName)
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            string fullName = typeof(DummyDisabledExtension).FullName;
            _extensionManager.EnableExtension(fullName, true);

            Assert.That(_extensionManager.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(fullName)
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void SkipsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
            //May be null on mono
            Assume.That(Assembly.GetEntryAssembly(), Is.Not.Null, "Entry assembly is null, framework loading validation will be skipped.");

#if NETCOREAPP
            // Attempt to load the .NET 3.5 version of the extensions from the .NET Core 2.0 tests
            var assemblyName = Path.GetFullPath("../net35/nunit.engine.core.tests.exe");
#else
            // Attempt to load the .NET Core 3.1 version of the extensions from the .NET 3.5 tests
            var assemblyName = Path.GetFullPath("../netcoreapp3.1/nunit.engine.core.tests.dll");
#endif
            Assert.That(assemblyName, Does.Exist);
            Console.WriteLine($"{assemblyName} does exist");

            var service = new ExtensionManager();
            service.FindExtensionPoints(typeof(DriverService).Assembly);
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

#if DEBUG
        static readonly string CONFIG = "Debug";
#else
        static readonly string CONFIG = "Release";
#endif

        public static IEnumerable<TestCaseData> ValidCombos()
        {
#if NETCOREAPP
            Assembly netcoreAssembly = Assembly.GetExecutingAssembly();

            var netStandardExtension = new ExtensionAssembly(Path.GetFullPath($"../../../../nunit.engine/bin/{CONFIG}/netstandard2.0/nunit.engine.dll"), false);
            yield return new TestCaseData(new FrameworkCombo(netcoreAssembly, netStandardExtension)).SetName("ValidCombo(.NET Core, .NET Standard)");

            var netCoreExtension = new ExtensionAssembly(netcoreAssembly.Location, false);
            yield return new TestCaseData(new FrameworkCombo(netcoreAssembly, netCoreExtension)).SetName("ValidCombo(.NET Core, .Net Core)");
#else
            Assembly netFrameworkAssembly = typeof(ExtensionManager).Assembly;

            var netFrameworkExtension = new ExtensionAssembly(netFrameworkAssembly.Location, false);
            yield return new TestCaseData(new FrameworkCombo(netFrameworkAssembly, netFrameworkExtension)).SetName("ValidCombo(.NET Framework, .NET Framework)");

            var netStandardExtension = new ExtensionAssembly(Path.GetFullPath($"../../../../nunit.engine/bin/{CONFIG}/netstandard2.0/nunit.engine.dll"), false);
            yield return new TestCaseData(new FrameworkCombo(netFrameworkAssembly, netStandardExtension)).SetName("ValidCombo(.NET Framework, .NET Standard)");
#endif
        }

        public static IEnumerable<TestCaseData> InvalidTargetFrameworkCombos()
        {
#if NETCOREAPP
            Assembly netcoreAssembly = Assembly.GetExecutingAssembly();

            var netFrameworkExtension = new ExtensionAssembly(Path.GetFullPath($"../../../../nunit.engine/bin/{CONFIG}/net462/nunit.engine.dll"), false);
            yield return new TestCaseData(new FrameworkCombo(netcoreAssembly, netFrameworkExtension)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFrameworkAssembly = typeof(ExtensionManager).Assembly;

            var netCoreExtension = new ExtensionAssembly(Path.GetFullPath($"../../../../nunit.engine.tests/bin/{CONFIG}/netcoreapp3.1/nunit.engine.tests.dll"), false);
            yield return new TestCaseData(new FrameworkCombo(netFrameworkAssembly, netCoreExtension)).SetName("InvalidCombo(.NET Framework, .NET Core)");
#endif

        }

        public static IEnumerable<TestCaseData> InvalidRunnerCombos()
        {
#if NETCOREAPP
            Assembly netStandardAssembly = Assembly.LoadFile(Path.GetFullPath($"../../../../nunit.engine/bin/{CONFIG}/netstandard2.0/nunit.engine.dll"));
            Assembly netCoreAssembly = Assembly.GetExecutingAssembly();

            var netStandardExtension = new ExtensionAssembly(netStandardAssembly.Location, false);
            yield return new TestCaseData(new FrameworkCombo(netStandardAssembly, netStandardExtension)).SetName("InvalidCombo(.NET Standard, .NET Standard)");

            var netCoreExtension = new ExtensionAssembly(netCoreAssembly.Location, false);
            yield return new TestCaseData(new FrameworkCombo(netStandardAssembly, netCoreExtension)).SetName("InvalidCombo(.NET Standard, .NET Core)");

            var netFrameworkExtension = new ExtensionAssembly(Path.GetFullPath($"../../../../nunit.engine/bin/{CONFIG}/net462/nunit.engine.dll"), false);
            yield return new TestCaseData(new FrameworkCombo(netStandardAssembly, netFrameworkExtension)).SetName("InvalidCombo(.NET Standard, .NET Framework)");
#else
            return new List<TestCaseData>();
#endif
        }

        [Test]
        public void StartService_ReadsAddinsFile()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var sut = new ExtensionManager(addinsReader, fileSystem);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
        }

        private void Initialize(ExtensionManager sut)
        {
            var coreAssembly = typeof(ExtensionManager).Assembly;
            var apiAssembly = typeof(ITestEngine).Assembly;

            sut.FindExtensionPoints(coreAssembly, apiAssembly);

            sut.FindExtensions(AssemblyHelper.GetDirectoryName(coreAssembly));
        }

        [Test]
        public void StartService_ReadsAddinsFilesFromMultipleDirectories()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var subdirectoryPath = Path.Combine(startDirectoryPath, "subdirectory");
            var subdirectory = Substitute.For<IDirectory>();
            subdirectory.FullName.Returns(subdirectoryPath);
            subdirectory.GetDirectories(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(new IDirectory[] { });
            subdirectory.GetFiles(Arg.Any<string>()).Returns(new IFile[] { });
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            var addinsFile2 = Substitute.For<IFile>();
            addinsFile2.Parent.Returns(subdirectory);
            addinsFile2.FullName.Returns(Path.Combine(subdirectoryPath, "second.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            startDirectory.GetDirectories("subdirectory", SearchOption.TopDirectoryOnly).Returns(new[] { subdirectory });
            subdirectory.GetFiles(Arg.Any<string>()).Returns(ci => (string)ci[0] == "*.addins" ? new[] { addinsFile2 } : new IFile[] { });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            fileSystem.GetDirectory(subdirectoryPath).Returns(subdirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "subdirectory/" });
            var sut = new ExtensionManager(addinsReader, fileSystem);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
            subdirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile2);
        }

        [Test]
        public void ProcessAddinsFile_RelativePaths()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(
                new[]
                { 
                    "path/to/directory/",
                    "directory2/",
                    "**/wildcard-directory/",
                    "path/to/file/file1.dll",
                    "file2.dll",
                    "**/wildcard-file.dll"
                });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(startDirectory, "path/to/directory/");
            directoryFinder.Received().GetDirectories(startDirectory, "directory2/");
            directoryFinder.Received().GetDirectories(startDirectory, "**/wildcard-directory/");
            directoryFinder.Received().GetFiles(startDirectory, "path/to/file/file1.dll");
            directoryFinder.Received().GetFiles(startDirectory, "file2.dll");
            directoryFinder.Received().GetFiles(startDirectory, "**/wildcard-file.dll");
        }

        [Test]
        [Platform("win")]
        public void ProcessAddinsFile_AbsolutePath_Windows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var metamorphosatorDirectoryPath = "c:/tools/metamorphosator";
            var metamorphosatorDirectory = Substitute.For<IDirectory>();
            metamorphosatorDirectory.FullName.Returns(metamorphosatorDirectoryPath);
            var toolsDirectoryPath = "d:/tools";
            var toolsDirectory = Substitute.For<IDirectory>();
            toolsDirectory.FullName.Returns(toolsDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            fileSystem.GetDirectory(metamorphosatorDirectoryPath + "/").Returns(metamorphosatorDirectory);
            fileSystem.GetDirectory("d:\\tools").Returns(toolsDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "c:/tools/metamorphosator/", "d:/tools/frobuscator.dll" });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(metamorphosatorDirectory, string.Empty);
            directoryFinder.Received().GetFiles(toolsDirectory, "frobuscator.dll");
            directoryFinder.DidNotReceive().GetDirectories(toolsDirectory, Arg.Is<string>(s => s != "frobuscator.dll"));
        }

        [Test]
        [Platform("linux,macosx,unix")]
        public void ProcessAddinsFile_AbsolutePath_NonWindows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var metamorphosatorDirectoryPath = "/tmp/tools/metamorphosator";
            var metamorphosatorDirectory = Substitute.For<IDirectory>();
            metamorphosatorDirectory.FullName.Returns(metamorphosatorDirectoryPath);
            var usrDirectoryPath = "/usr";
            var toolsDirectory = Substitute.For<IDirectory>();
            toolsDirectory.FullName.Returns(usrDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            fileSystem.GetDirectory(metamorphosatorDirectoryPath + "/").Returns(metamorphosatorDirectory);
            fileSystem.GetDirectory(usrDirectoryPath).Returns(toolsDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "/tmp/tools/metamorphosator/", "/usr/frobuscator.dll" });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(metamorphosatorDirectory, string.Empty);
            directoryFinder.Received().GetFiles(toolsDirectory, "frobuscator.dll");
            directoryFinder.DidNotReceive().GetDirectories(toolsDirectory, Arg.Is<string>(s => s != "frobuscator.dll"));
        }

        [Test]
        [Platform("win")]
        public void ProcessAddinsFile_InvalidAbsolutePathToFile_Windows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "/absolute/unix/path" });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetFiles(startDirectory, "/absolute/unix/path");
        }

        [Test]
        [Platform("win")]
        public void ProcessAddinsFile_InvalidAbsolutePathToDirectory_Windows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "/absolute/unix/path/" });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(startDirectory, "/absolute/unix/path/");
        }

        [TestCase("c:/absolute/windows/path")]
        [TestCase("c:\\absolute\\windows\\path")]
        [TestCase("c:\\absolute\\windows\\path\\")]
        [Platform("linux,macosx,unix")]
        public void ProcessAddinsFile_InvalidAbsolutePathToFile_NonWindows(string windowsPath)
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { windowsPath });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetFiles(startDirectory, windowsPath);
        }

        [TestCase("c:/absolute/windows/path/")]
        [Platform("linux,macosx,unix")]
        public void ProcessAddinsFile_InvalidAbsolutePathToDirectory_NonWindows(string windowsPath)
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { windowsPath });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(startDirectory, windowsPath);
        }

        [Test]
        public void StartService_ReadsMultipleAddinsFilesFromSingleDirectory()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile1 = Substitute.For<IFile>();
            addinsFile1.Parent.Returns(startDirectory);
            addinsFile1.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile1 });
            var addinsFile2 = Substitute.For<IFile>();
            addinsFile1.Parent.Returns(startDirectory);
            addinsFile1.FullName.Returns(Path.Combine(startDirectoryPath, "test2.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile1, addinsFile2 });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var sut = new ExtensionManager(addinsReader, fileSystem);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile1);
            addinsReader.Received().Read(addinsFile2);
        }

        [Test]
        public void ProcessAddinsFile_ReadsAddinsFileFromReferencedDirectory()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var referencedDirectoryPath = Path.Combine(startDirectoryPath, "metamorphosator");
            var referencedDirectory = Substitute.For<IDirectory>();
            referencedDirectory.FullName.Returns(referencedDirectoryPath);
            referencedDirectory.Parent.Returns(startDirectory);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            var referencedAddinsFile = Substitute.For<IFile>();
            referencedAddinsFile.Parent.Returns(referencedDirectory);
            referencedAddinsFile.FullName.Returns(Path.Combine(referencedDirectoryPath, "test2.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            referencedDirectory.GetFiles("*.addins").Returns(new[] { referencedAddinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(referencedDirectoryPath).Returns(referencedDirectory);
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            addinsReader.Read(addinsFile).Returns(new[] { "./metamorphosator/" });
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            directoryFinder.GetDirectories(startDirectory, "./metamorphosator/").Returns(new[] { referencedDirectory });
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
            referencedDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(referencedAddinsFile);
        }

        [Test]
        [Platform("win")]
        public void ProcessAddinsFile_Issue915_AddinsFilePointsToContainingDirectory_Windows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            
            // Faking the presence of the assembly is required to reproduce the error described in GitHub issue 915...
            var testAssembly = Substitute.For<IFile>();
            testAssembly.Parent.Returns(startDirectory);
            testAssembly.FullName.Returns(typeof(ExtensionManager).Assembly.Location);
            startDirectory.GetFiles("*.dll").Returns(new[] { testAssembly });

            var parentPath = new DirectoryInfo(startDirectoryPath).Parent.FullName;
            var parentDirectory = Substitute.For<IDirectory>();
            parentDirectory.FullName.Returns(parentPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "my.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var addinsContent = new[] {
                "./",
                startDirectoryPath + Path.DirectorySeparatorChar,
                $"..{Path.DirectorySeparatorChar}{Path.GetFileName(startDirectoryPath)}{Path.DirectorySeparatorChar}",
                @"*\..\",
                @"**\..\",
                @"**\.\"
            };
            addinsReader.Read(addinsFile).Returns(addinsContent);
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            directoryFinder.GetDirectories(startDirectory, "./").Returns(new[] { startDirectory });
            directoryFinder.GetFiles(startDirectory, string.Empty).Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, $"..{Path.DirectorySeparatorChar}{Path.GetFileName(startDirectoryPath)}{Path.DirectorySeparatorChar}").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, @"*\..\").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, @"**\..\").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, @"**\.\").Returns(new[] { testAssembly });
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            startDirectory.DidNotReceive().GetFiles("*.dll");
            parentDirectory.DidNotReceive().GetFiles("*.dll");
            directoryFinder.Received().GetDirectories(startDirectory, "./");
            directoryFinder.Received().GetFiles(startDirectory, string.Empty);
            directoryFinder.Received().GetFiles(startDirectory, $"..{Path.DirectorySeparatorChar}{Path.GetFileName(startDirectoryPath)}{Path.DirectorySeparatorChar}");
            directoryFinder.Received().GetFiles(startDirectory, @"*\..\");
            directoryFinder.Received().GetFiles(startDirectory, @"**\..\");
            directoryFinder.Received().GetFiles(startDirectory, @"**\.\");
            addinsReader.Received().Read(addinsFile);
        }

        [Test]
        [Platform("linux,macosx,unix")]
        public void ProcessAddinsFile_Issue915_AddinsFilePointsToContainingDirectory_NonWindows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionManager).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);

            // Faking the presence of the assembly is required to reproduce the error described in GitHub issue 915...
            var testAssembly = Substitute.For<IFile>();
            testAssembly.Parent.Returns(startDirectory);
            testAssembly.FullName.Returns(typeof(ExtensionManager).Assembly.Location);
            startDirectory.GetFiles("*.dll").Returns(new[] { testAssembly });

            var parentPath = new DirectoryInfo(startDirectoryPath).Parent.FullName;
            var parentDirectory = Substitute.For<IDirectory>();
            parentDirectory.FullName.Returns(parentPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "my.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            fileSystem.GetDirectory(startDirectoryPath + "/").Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var addinsContent = new[] {
                "./",
                startDirectoryPath + "/",
                $"../{Path.GetFileName(startDirectoryPath)}/",
                "*/../",
                "**/../",
                "**/./"
            };
            addinsReader.Read(addinsFile).Returns(addinsContent);
            var directoryFinder = Substitute.For<IDirectoryFinder>();
            directoryFinder.GetDirectories(startDirectory, "./").Returns(new[] { startDirectory });
            directoryFinder.GetFiles(startDirectory, string.Empty).Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, $"../{Path.GetFileName(startDirectoryPath)}/").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, "*/../").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, "**/../").Returns(new[] { testAssembly });
            directoryFinder.GetFiles(startDirectory, "**/./").Returns(new[] { testAssembly });
            var sut = new ExtensionManager(addinsReader, fileSystem, directoryFinder);

            // Act
            Initialize(sut);

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
            startDirectory.DidNotReceive().GetFiles("*.dll");
            parentDirectory.DidNotReceive().GetFiles("*.dll");
            directoryFinder.Received().GetDirectories(startDirectory, "./");
            directoryFinder.Received().GetDirectories(startDirectory, string.Empty);
            directoryFinder.Received().GetDirectories(startDirectory, $"../{Path.GetFileName(startDirectoryPath)}/");
            directoryFinder.Received().GetDirectories(startDirectory, "*/../");
            directoryFinder.Received().GetDirectories(startDirectory, "**/../");
            directoryFinder.Received().GetDirectories(startDirectory, "**/./");
        }
    }
}
