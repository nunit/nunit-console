// ***********************************************************************
// Copyright (c) 2021 NUnit Contributors
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
namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    using NUnit.Engine.Internal.FileSystemAccess.Default;
    using NUnit.Framework;
    using System.Linq;
    using SIO = System.IO;

    [TestFixture]
    public sealed class DirectoryTests
    {
        [Test]
        public void Init()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var parent = new SIO.DirectoryInfo(path).Parent.FullName;

            var directory = new Directory(path);

            Assert.AreEqual(path, directory.FullName);
            Assert.AreEqual(parent, directory.Parent.FullName);
        }

        [Test]
        public void Init_PathIsNull()
        {
            Assert.That(() => new Directory(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Init_InvalidPath()
        {
            Assert.That(() => new Directory("c:\\this\\is\\an\\invalid" + SIO.Path.GetInvalidPathChars()[2] + "path"), Throws.ArgumentException);
        }

        [Test]
        public void Init_EmptyPath()
        {
            Assert.That(() => new Directory(string.Empty), Throws.ArgumentException);
        }

        [Test]
        public void Init_TrailingDirectorySeparator()
        {
            var path = SIO.Directory.GetCurrentDirectory() + new string(SIO.Path.DirectorySeparatorChar, 1);
            var parent = new SIO.DirectoryInfo(SIO.Directory.GetCurrentDirectory()).Parent.FullName;

            var directory = new Directory(path);

            Assert.AreEqual(path, directory.FullName);
            Assert.AreEqual(parent, directory.Parent.FullName);
        }

        [Test]
        public void Init_NoParent_SMB()
        {
            var path = "\\\\server\\share";

            var directory = new Directory(path);

            Assert.AreEqual(path, directory.FullName);
            Assert.IsNull(directory.Parent);
        }

        [Test]
        public void Init_NoParent_Drive()
        {
            var path = "x:\\";

            var directory = new Directory(path);

            Assert.AreEqual(path, directory.FullName);
            Assert.IsNull(directory.Parent);
        }

        [Test]
        public void Init_NoParent_Root()
        {
            var path = "/";
            var expected = new SIO.DirectoryInfo(path).FullName;

            var directory = new Directory(path);

            Assert.AreEqual(expected, directory.FullName);
            Assert.IsNull(directory.Parent);
        }

        [Test]
        public void GetFiles()
        {
            var path = SIO.Directory.GetCurrentDirectory();
            var expected = new SIO.DirectoryInfo(path).GetFiles().Select(x => x.FullName);
            var directory = new Directory(path);

            var actualFiles = directory.GetFiles("*");

            var actual = actualFiles.Select(x => x.FullName);
            CollectionAssert.AreEquivalent(expected, actual);
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
            CollectionAssert.AreEquivalent(expected, actual);
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
