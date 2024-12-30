// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Engine.Internal.FileSystemAccess.Default;
using NUnit.Framework;
using System.Reflection;
using SIO = System.IO;

namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    [TestFixture]
    public sealed class FileSystemTests
    {
        [Test]
        public void GetDirectory()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var fileSystem = new FileSystem();

            var actual = fileSystem.GetDirectory(path);

            Assert.That(actual.FullName, Is.EqualTo(path));
        }

        [Test]
        public void GetDirectory_DirectoryDoesNotExist()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            while (SIO.Directory.Exists(path))
            {
                path += "x";
            }

            var fileSystem = new FileSystem();

            Assert.That(() => fileSystem.GetDirectory(path), Throws.InstanceOf<System.IO.DirectoryNotFoundException>());
        }

        [Test]
        public void GetFile()
        {
            var path = this.GetTestFileLocation();
            var parent = SIO.Path.GetDirectoryName(path);
            var fileSystem = new FileSystem();

            var file = fileSystem.GetFile(path);

            Assert.That(path, Is.EqualTo(file.FullName));
            Assert.That(parent, Is.EqualTo(file.Parent.FullName));
        }

        [Test]
        public void GetFile_DirectoryDoesNotExist()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            while (SIO.Directory.Exists(path))
            {
                path += "x";
            }
            path = SIO.Path.Combine(path, "foobar.file");

            var fileSystem = new FileSystem();

            Assert.That(() => fileSystem.GetFile(path), Throws.InstanceOf<System.IO.DirectoryNotFoundException>());
        }

        [Test]
        public void Exists_FileExists()
        {
            var path = this.GetTestFileLocation();
            var file = new File(path);
            var fileSystem = new FileSystem();

            var exists = fileSystem.Exists(file);

            Assert.That(exists, Is.True);
        }

        [Test]
        public void Exists_FileDoesNotExist()
        {
            var path = this.GetTestFileLocation();
            while (SIO.File.Exists(path))
            {
                path += "x";
            }

            var file = new File(path);
            var fileSystem = new FileSystem();

            var exists = fileSystem.Exists(file);

            Assert.That(exists, Is.False);
        }

        [Test]
        public void ExistsFileIsNull()
        {
            var fileSystem = new FileSystem();

            Assert.That(() => fileSystem.Exists((IFile)null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Exists_DirectoryExists()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var directory = new Directory(path);
            var fileSystem = new FileSystem();

            var exists = fileSystem.Exists(directory);

            Assert.That(exists, Is.True);
        }

        [Test]
        public void Exists_DirectoryDoesNotExist()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            while (SIO.Directory.Exists(path))
            {
                path += "x";
            }

            var directory = new Directory(path);
            var fileSystem = new FileSystem();

            var exists = fileSystem.Exists(directory);

            Assert.That(exists, Is.False);
        }

        [Test]
        public void Exists_DirectoryIsNull()
        {
            var fileSystem = new FileSystem();

            Assert.That(() => fileSystem.Exists((IDirectory)null!), Throws.ArgumentNullException);
        }

        private string GetTestFileLocation()
        {
            return Assembly.GetAssembly(typeof(FileTests))!.Location;
        }
    }
}
