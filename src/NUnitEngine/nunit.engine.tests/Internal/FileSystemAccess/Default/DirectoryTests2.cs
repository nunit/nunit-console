// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess.Default;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using SIO = System.IO;

namespace NUnit.Engine.Tests.Internal.FileSystemAccess.Default
{
    /// <summary>
    /// Tests the implementation of <see cref="Directory"/>.
    /// </summary>
    /// <remarks>All tests in this fixture modify the file-system. Therefore they need to be marked explicitly to run.</remarks>
    [TestFixture, Category("WritesToDisk"), NonParallelizable]
    public sealed class DirectoryTests2
    {
        private string testDirectory;
        private IEnumerable<string> subDirectories;

        private static string Combine(params string[] parts)
        {
            string result = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                result = SIO.Path.Combine(result, parts[i]);
            }
            return result;
        }

        [OneTimeSetUp]
        public void CreateDirectoryStructure()
        {
            this.testDirectory = Combine(SIO.Path.GetTempPath(), "nunit.engine.tests.temp", Guid.NewGuid().ToString());
            var subDirectories = new List<string>();
            subDirectories.Add(Combine(this.testDirectory, "abc"));
            subDirectories.Add(Combine(this.testDirectory, "abc", "123"));
            subDirectories.Add(Combine(this.testDirectory, "abc", "456"));
            subDirectories.Add(Combine(this.testDirectory, "abc", "789"));
            subDirectories.Add(Combine(this.testDirectory, "abc", "789", "xyz"));
            subDirectories.Add(Combine(this.testDirectory, "def"));
            subDirectories.Add(Combine(this.testDirectory, "def", "kek"));
            subDirectories.Add(Combine(this.testDirectory, "def", "kek", "lel"));
            subDirectories.Add(Combine(this.testDirectory, "def", "kek", "lel", "mem"));
            subDirectories.Add(Combine(this.testDirectory, "ghi"));

            this.subDirectories = subDirectories;
            SIO.Directory.CreateDirectory(this.testDirectory);
            foreach(var directory in this.subDirectories)
            {
                SIO.Directory.CreateDirectory(directory);
            }
        }

        [OneTimeTearDown]
        public void DeleteDirectoryStructure()
        {
            SIO.Directory.Delete(this.testDirectory, true);
        }

        [Test]
        public void GetDirectories()
        {
            var expected = new string[] { Combine(this.testDirectory, "abc"), Combine(this.testDirectory, "def"), Combine(this.testDirectory, "ghi") };
            var directory = new Directory(this.testDirectory);

            var actualDirectories = directory.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly);

            var actual = actualDirectories.Select(x => x.FullName);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void GetDirectories_AllSubDirectories()
        {
            var directory = new Directory(this.testDirectory);

            var actualDirectories = directory.GetDirectories("*", SIO.SearchOption.AllDirectories);

            var actual = actualDirectories.Select(x => x.FullName);
            CollectionAssert.AreEquivalent(this.subDirectories, actual);
        }

        [Test]
        public void GetDirectories_WithPattern()
        {
            var expected = new string[] { Combine(this.testDirectory, "abc") };
            var directory = new Directory(this.testDirectory);

            var actualDirectories = directory.GetDirectories("a??", SIO.SearchOption.TopDirectoryOnly);

            var actual = actualDirectories.Select(x => x.FullName);
            CollectionAssert.AreEquivalent(expected, actual);
        }

        [Test]
        public void GetDirectories_WithPattern_NoMatch()
        {
            var directory = new Directory(this.testDirectory);

            var actual = directory.GetDirectories("z*", SIO.SearchOption.AllDirectories);

            CollectionAssert.IsEmpty(actual);
        }

        [Test]
        public void GetDirectories_WithPattern_AllSubDirectories()
        {
            var expected = new string[] { Combine(this.testDirectory, "def"), Combine(this.testDirectory, "def", "kek"), Combine(this.testDirectory, "def", "kek", "lel"), Combine(this.testDirectory, "def", "kek", "lel", "mem") };
            var directory = new Directory(this.testDirectory);

            var actualDirectories = directory.GetDirectories("?e?", SIO.SearchOption.AllDirectories);

            var actual = actualDirectories.Select(x => x.FullName);
            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
