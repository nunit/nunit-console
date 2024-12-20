// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess.Default;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using SIO = System.IO;

namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    [TestFixture]
    public sealed class FileTests
    {
        [Test]
        public void Init()
        {
            var path = this.GetTestFileLocation();
            var parent = SIO.Path.GetDirectoryName(path);

            var file = new File(path);

            Assert.That(path, Is.EqualTo(file.FullName));
            Assert.That(parent, Is.EqualTo(file.Parent.FullName));
        }

        [Test]
        public void Init_PathIsNull()
        {
            Assert.That(() => new File(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Init_InvalidPath_InvalidDirectory()
        {
            if (SIO.Path.GetInvalidPathChars().Length == 0)
            {
                Assert.Ignore("This test does not make sense on systems where System.IO.Path.GetInvalidPathChars() returns an empty array.");
            }

            var path = SIO.Path.GetInvalidPathChars()[SIO.Path.GetInvalidPathChars().Length-1] + this.GetTestFileLocation();

            Assert.That(() => new File(path), Throws.ArgumentException);
        }

        [Test]
        public void Init_InvalidPath_InvalidFileName()
        {
            if (!SIO.Path.GetInvalidFileNameChars().Except(SIO.Path.GetInvalidPathChars()).Any())
            {
                Assert.Ignore("This test does not make sense on systems where all characters returned by System.IO.Path.GetInvalidFileNameChars() are contained in System.IO.System.Path.GetInvalidPathChars().");
            }

            char invalidCharThatIsNotInInvalidPathChars = SIO.Path.GetInvalidFileNameChars().Except(SIO.Path.GetInvalidPathChars()).First();
            var path = this.GetTestFileLocation() + invalidCharThatIsNotInInvalidPathChars;

            Assert.That(() => new File(path), Throws.ArgumentException);
        }

        [Test]
        public void Init_EmptyPath()
        {
            Assert.That(() => new File(string.Empty), Throws.ArgumentException);
        }

        [Test]
        public void Init_NonExistingFile()
        {
            var path = this.GetTestFileLocation();
            while (SIO.File.Exists(path))
            {
                path += "a";
            }

            var parent = SIO.Path.GetDirectoryName(path);

            var file = new File(path);

            Assert.That(path, Is.EqualTo(file.FullName));
            Assert.That(parent, Is.EqualTo(file.Parent.FullName));
        }

        [Test]
        public void Init_PathIsDirectory()
        {
            var path = SIO.Directory.GetCurrentDirectory() + SIO.Path.DirectorySeparatorChar;

            Assert.That(() => new File(path), Throws.ArgumentException);
        }

        private string GetTestFileLocation()
        {
            return Assembly.GetAssembly(typeof(FileTests)).Location;
        }
    }
}
