//-----------------------------------------------------------------------
// <copyright file="FileTests.cs" company="TillW">
//   Copyright 2020 TillW. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    using NUnit.Engine.Internal.FileSystemAccess.Default;
    using NUnit.Framework;
    using System.Linq;
    using System.Reflection;
    using SIO = System.IO;

    [TestFixture]
    public sealed class FileTests
    {
        private string GetTestFileLocation()
        {
#if NETCOREAPP1_1
            return Assembly.GetEntryAssembly().Location;
#else
            return Assembly.GetAssembly(typeof(FileTests)).Location;
#endif
        }

        [Test]
        public void Init()
        {
            var path = this.GetTestFileLocation();
            var parent = SIO.Path.GetDirectoryName(path);

            var file = new File(path);

            Assert.AreEqual(path, file.FullName);
            Assert.AreEqual(parent, file.Parent.FullName);
        }

        [Test]
        public void Init_PathIsNull()
        {
            Assert.That(() => new File(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Init_InvalidPath_InvalidDirectory()
        {
            var path = SIO.Path.GetInvalidPathChars()[1] + this.GetTestFileLocation();

            Assert.That(() => new File(path), Throws.ArgumentException);
        }

        [Test]
        public void Init_InvalidPath_InvalidFileName()
        {
            char invalidCharThatIsNotInInvalidPathChars = SIO.Path.GetInvalidFileNameChars().Except(SIO.Path.GetInvalidPathChars()).FirstOrDefault();
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

            Assert.AreEqual(path, file.FullName);
            Assert.AreEqual(parent, file.Parent.FullName);
        }

        [Test]
        public void Init_PathIsDirectory()
        {
            var path = SIO.Directory.GetCurrentDirectory() + SIO.Path.DirectorySeparatorChar;

            Assert.That(() => new File(path), Throws.ArgumentException);
        }

    }
}
