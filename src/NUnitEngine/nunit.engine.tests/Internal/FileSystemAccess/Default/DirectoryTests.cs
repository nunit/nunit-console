//-----------------------------------------------------------------------
// <copyright file="Directory.cs" company="TillW">
//   Copyright 2020 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    using NUnit.Engine.Internal.FileSystemAccess.Default;
    using NUnit.Framework;
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
    }
}
