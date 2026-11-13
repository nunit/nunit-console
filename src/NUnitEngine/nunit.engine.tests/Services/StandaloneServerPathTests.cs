// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NET8_0_OR_GREATER
using NUnit.Framework;
using NUnit.Remote.Hosting;

namespace NUnit.Engine.Services
{
    /// <summary>Exercises the server's real path boundary without accessing network shares or devices.</summary>
    public sealed class StandaloneServerPathTests
    {
        [TestCase(@"C:\agents", @"C:\agents\net10\launcher.dll")]
        [TestCase(@"C:\agents", @"\\?\C:\agents\net10\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"C:\agents\net10\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"\\?\C:\agents\net10\launcher.dll")]
        [TestCase(@"c:\AGENTS\", @"\\?\C:\agents\net10\launcher.dll")]
        [TestCase(@"C:/agents", @"C:/agents/net10/launcher.dll")]
        [TestCase(@"C:\agents", @"C:\agents\net10\..\launcher.dll")]
        [TestCase(@"C:\", @"C:\agents\launcher.dll")]
        [TestCase(@"\\server\share\agents", @"\\server\share\agents\launcher.dll")]
        [TestCase(@"\\server\share\agents", @"\\?\UNC\server\share\agents\launcher.dll")]
        [TestCase(@"\\?\UNC\server\share\agents", @"\\server\share\agents\launcher.dll")]
        [TestCase(@"\\?\UNC\server\share\agents", @"\\?\UNC\server\share\agents\launcher.dll")]
        [TestCase(@"\\SERVER\Share\agents\", @"\\?\unc\server\share\AGENTS\launcher.dll")]
        [Platform("Win")]
        public void EquivalentWindowsPathsRemainWithinTheStagedRoot(string directory, string assemblyPath)
        {
            Assert.That(ExtensionPath.IsWithinDirectory(directory, assemblyPath), Is.True);
        }

        [TestCase(@"C:\agents", @"C:\agents-other\launcher.dll")]
        [TestCase(@"C:\agents", @"\\?\C:\agents-other\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"C:\agents-other\launcher.dll")]
        [TestCase(@"C:\agents", @"C:\agents\..\outside\launcher.dll")]
        [TestCase(@"C:\agents", @"\\?\C:\agents\..\outside\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"\\?\C:\agents\..\outside\launcher.dll")]
        [TestCase(@"C:\agents", @"C:\agents")]
        [TestCase(@"C:\agents", @"C:\agents\")]
        [TestCase(@"C:\", @"C:\")]
        [TestCase(@"C:\agents", @"\\?\C:\agents")]
        [TestCase(@"C:\agents", @"D:\agents\launcher.dll")]
        [TestCase(@"\\server\share\agents", @"\\?\UNC\other\share\agents\launcher.dll")]
        [TestCase(@"\\server\share\agents", @"\\?\UNC\server\other\agents\launcher.dll")]
        [TestCase(@"\\?\UNC\server\share\agents", @"\\?\UNC\server\share\agents-other\launcher.dll")]
        [TestCase(@"C:\agents", @"\\.\C:\agents\launcher.dll")]
        [TestCase(@"\\.\C:\agents", @"\\.\C:\agents\launcher.dll")]
        [TestCase(@"\\?\GLOBALROOT\Device\Volume\agents", @"\\?\GLOBALROOT\Device\Volume\agents\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"\\?\C:\agents\sub\..\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"\\?\C:\agents\sub.\launcher.dll")]
        [TestCase(@"\\?\C:\agents", @"\\?\C:\agents\sub \launcher.dll")]
        [Platform("Win")]
        public void WindowsPathsOutsideOrAmbiguousToTheBoundaryAreRejected(string directory, string assemblyPath)
        {
            Assert.That(ExtensionPath.IsWithinDirectory(directory, assemblyPath), Is.False);
        }

        [TestCase("agents", "agents/launcher.dll")]
        [TestCase(".", "launcher.dll")]
        public void RelativePathsDoNotEstablishAnAllowedRoot(string directory, string assemblyPath)
        {
            Assert.That(ExtensionPath.IsWithinDirectory(directory, assemblyPath), Is.False);
        }

        [TestCase("/tmp/agents", "/tmp/agents/net10/launcher.dll", true)]
        [TestCase("/tmp/agents/", "/tmp/agents/launcher.dll", true)]
        [TestCase("/", "/tmp/agents/launcher.dll", true)]
        [TestCase("/tmp/agents", "/tmp/agents-other/launcher.dll", false)]
        [TestCase("/tmp/agents", "/tmp/agents/../outside/launcher.dll", false)]
        [TestCase("/tmp/agents", "/tmp/AGENTS/launcher.dll", false)]
        [TestCase("/tmp/agents", "/tmp/agents", false)]
        [TestCase("/", "/", false)]
        [Platform(Exclude = "Win")]
        public void UnixPathsRetainTheirCaseSensitiveDirectoryBoundary(string directory, string assemblyPath, bool expected)
        {
            Assert.That(ExtensionPath.IsWithinDirectory(directory, assemblyPath), Is.EqualTo(expected));
        }
    }
}
#endif
