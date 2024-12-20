// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess.Default;
using NUnit.Framework;
using System.Linq;
using SIO = System.IO;

namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    [TestFixture]
    public sealed class DirectoryTests
    {
        [Test]
        public void Init()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var parent = new SIO.DirectoryInfo(path).Parent.FullName;

            var directory = new Directory(path);

            Assert.That(path, Is.EqualTo(directory.FullName));
            Assert.That(parent, Is.EqualTo(directory.Parent.FullName));
        }

        [Test]
        public void Init_PathIsNull()
        {
            Assert.That(() => new Directory(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Init_InvalidPath()
        {
            if (SIO.Path.GetInvalidPathChars().Length == 0)
            {
                Assert.Ignore("This test does not make sense on systems where System.IO.Path.GetInvalidPathChars() returns an empty array.");
            }

            Assert.That(() => new Directory("c:\\this\\is\\an\\invalid" + SIO.Path.GetInvalidPathChars()[SIO.Path.GetInvalidPathChars().Length - 1] + "path"), Throws.ArgumentException);
        }

        [Test]
        public void Init_EmptyPath()
        {
            Assert.That(() => new Directory(string.Empty), Throws.ArgumentException);
        }

        [Test]
        public void Init_TrailingDirectorySeparator()
        {
            var path = SIO.Directory.GetCurrentDirectory() + SIO.Path.DirectorySeparatorChar;
            var parent = new SIO.DirectoryInfo(SIO.Directory.GetCurrentDirectory()).Parent.FullName;

            var directory = new Directory(path);

            Assert.That(path, Is.EqualTo(directory.FullName));
            Assert.That(parent, Is.EqualTo(directory.Parent.FullName));
        }

        // Skip this test on non-Windows systems since System.IO.DirectoryInfo appends '\\server\share' to the current working-directory, making this test useless.
        [Test, Platform("Win")]
        public void Init_NoParent_SMB()
        {
            var path = "\\\\server\\share";
            var directory = new Directory(path);

            Assert.That(path, Is.EqualTo(directory.FullName));
            Assert.That(directory.Parent, Is.Null);
        }

        // Skip this test on non-Windows systems since System.IO.DirectoryInfo appends 'x:\' to the current working-directory, making this test useless.
        [Test, Platform("Win")]
        public void Init_NoParent_Drive()
        {
            var path = "x:\\";
            var directory = new Directory(path);

            Assert.That(path, Is.EqualTo(directory.FullName));
            Assert.That(directory.Parent, Is.Null);
        }

        [Test]
        public void Init_NoParent_Root()
        {
            var path = "/";
            var expected = new SIO.DirectoryInfo(path).FullName;

            var directory = new Directory(path);

            Assert.That(expected, Is.EqualTo(directory.FullName));
            Assert.That(directory.Parent, Is.Null);
        }

        [Test]
        public void GetFiles()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var expected = new SIO.DirectoryInfo(path).GetFiles().Select(x => x.FullName);
            var directory = new Directory(path);

            var actualFiles = directory.GetFiles("*");

            var actual = actualFiles.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void GetFiles_NonExistingDirectory()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            while (SIO.Directory.Exists(path))
            {
                path += "a";
            }
            var directory = new Directory(path);

            Assert.That(() => directory.GetFiles("*"), Throws.InstanceOf<SIO.DirectoryNotFoundException>());
        }

        [Test]
        public void GetFiles_WithPattern()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var expected = new SIO.DirectoryInfo(path).GetFiles("*.dll").Select(x => x.FullName);
            var directory = new Directory(path);

            var actualFiles = directory.GetFiles("*.dll");

            var actual = actualFiles.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void GetFiles_SearchPatternIsNull()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var directory = new Directory(path);

            Assert.That(() => directory.GetFiles(null), Throws.ArgumentNullException);
        }

        [Test]
        public void GetDirectories_NonExistingDirectory()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            while (SIO.Directory.Exists(path))
            {
                path += "a";
            }
            var directory = new Directory(path);

            Assert.That(() => directory.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly), Throws.InstanceOf<SIO.DirectoryNotFoundException>());
        }

        [Test]
        public void GetDirectories_SearchPatternIsNull()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var directory = new Directory(path);

            Assert.That(() => directory.GetDirectories(null, SIO.SearchOption.TopDirectoryOnly), Throws.ArgumentNullException);
        }

        [Test]
        public void GetDirectories_SearchOptionIsInvalid()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var directory = new Directory(path);

            Assert.That(() => directory.GetDirectories("*", (SIO.SearchOption)5), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
