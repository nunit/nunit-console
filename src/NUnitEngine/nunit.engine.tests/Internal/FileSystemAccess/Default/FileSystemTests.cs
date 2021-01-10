// ***********************************************************************
// Copyright (c) 2021 Charlie Poole, Rob Prouse
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
            var path = TestContext.CurrentContext.TestDirectory;
            var parent = SIO.Path.GetDirectoryName(path);
            var fileSystem = new FileSystem();

            var file = fileSystem.GetFile(path);

            Assert.AreEqual(path, file.FullName);
            Assert.AreEqual(parent, file.Parent.FullName);
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
            var path = TestContext.CurrentContext.TestDirectory;
            var file = new File(path);
            var fileSystem = new FileSystem();

            var exists = fileSystem.Exists(file);

            Assert.That(exists, Is.True);
        }

        [Test]
        public void Exists_FileDoesNotExist()
        {
            var path = TestContext.CurrentContext.TestDirectory;
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

            Assert.That(() => fileSystem.Exists((IFile)null), Throws.ArgumentNullException);
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

            Assert.That(() => fileSystem.Exists((IDirectory)null), Throws.ArgumentNullException);
        }
    }
}
