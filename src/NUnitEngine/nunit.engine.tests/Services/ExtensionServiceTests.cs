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
            _serviceClass.FindExtensionPoints(typeof(CoreEngine).Assembly);
            _serviceClass.FindExtensionPoints(typeof(ITestEngine).Assembly);

            _serviceClass.FindExtensionsInAssembly(new ExtensionAssembly(GetType().Assembly.Location, false));
        }

        [Test]
        public void StartService_UseFileSystemAbstraction()
        {
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var fileSystem = Substitute.For<IFileSystem>();
            var service = new ExtensionService(false, addinsReader, fileSystem);
            var workingDir = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);

            service.StartService();

            fileSystem.Received().GetDirectory(workingDir);
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

#if NETCOREAPP
            string other = "net35"; // Attempt to load the .NET 3.5 version of the extensions from the .NET Core 2.0 tests
#elif NET35
            string other = "netcoreapp2.1"; // Attempt to load the .NET Core 2.1 version of the extensions from the .NET 3.5 tests
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
            var extNetFramework = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("net35"), "nunit.engine.dll"), false);

            yield return new TestCaseData(new FrameworkCombo(netcore, extNetFramework)).SetName("InvalidCombo(.NET Core, .NET Framework)");
#else
            Assembly netFramework = typeof(ExtensionService).Assembly;

            var extNetStandard = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netstandard2.0"), "nunit.engine.dll"), false);
            var extNetCore = new ExtensionAssembly(Path.Combine(GetSiblingDirectory("netcoreapp2.1"), "nunit.engine.tests.dll"), false);

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

        [Test]
        public void StartService_ReadsAddinsFile()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
            var startDirectory = Substitute.For<IDirectory>();
            startDirectory.FullName.Returns(startDirectoryPath);
            var addinsFile = Substitute.For<IFile>();
            addinsFile.Parent.Returns(startDirectory);
            addinsFile.FullName.Returns(Path.Combine(startDirectoryPath, "test.addins"));
            startDirectory.GetFiles("*.addins").Returns(new[] { addinsFile });
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetDirectory(startDirectoryPath).Returns(startDirectory);
            var addinsReader = Substitute.For<IAddinsFileReader>();
            var sut = new ExtensionService(false, addinsReader, fileSystem);

            // Act
            sut.StartService();

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
        }

        [Test]
        public void StartService_ReadsAddinsFilesFromMultipleDirectories()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem);

            // Act
            sut.StartService();

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
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

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
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

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
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

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
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetFiles(startDirectory, "/absolute/unix/path");
        }

        [Test]
        [Platform("win")]
        public void ProcessAddinsFile_InvalidAbsolutePathToDirectory_Windows()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

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
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetFiles(startDirectory, windowsPath);
        }

        [TestCase("c:/absolute/windows/path/")]
        [Platform("linux,macosx,unix")]
        public void ProcessAddinsFile_InvalidAbsolutePathToDirectory_NonWindows(string windowsPath)
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

            // Assert
            addinsReader.Received().Read(addinsFile);
            directoryFinder.Received().GetDirectories(startDirectory, windowsPath);
        }

        [Test]
        public void StartService_ReadsMultipleAddinsFilesFromSingleDirectory()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem);

            // Act
            sut.StartService();

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile1);
            addinsReader.Received().Read(addinsFile2);
        }

        [Test]
        public void ProcessAddinsFile_ReadsAddinsFileFromReferencedDirectory()
        {
            // Arrange
            var startDirectoryPath = AssemblyHelper.GetDirectoryName(typeof(ExtensionService).Assembly);
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
            var sut = new ExtensionService(false, addinsReader, fileSystem, directoryFinder);

            // Act
            sut.StartService();

            // Assert
            startDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(addinsFile);
            referencedDirectory.Received().GetFiles("*.addins");
            addinsReader.Received().Read(referencedAddinsFile);
        }
    }
}
