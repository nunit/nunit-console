// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.FileSystemAccess;
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
    /// <remarks>All tests in this fixture modify the file-system.</remarks>
    [TestFixture, Category("WritesToDisk"), NonParallelizable]
    public sealed class DirectoryTests2
    {
        private string _testDirectory;
        private IEnumerable<string> _subDirectories;

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
            this._testDirectory = Combine(SIO.Path.GetTempPath(), "nunit.engine.tests.temp", Guid.NewGuid().ToString());
            var subDirectories = new List<string>();
            subDirectories.Add(Combine(this._testDirectory, "abc"));
            subDirectories.Add(Combine(this._testDirectory, "abc", "123"));
            subDirectories.Add(Combine(this._testDirectory, "abc", "456"));
            subDirectories.Add(Combine(this._testDirectory, "abc", "789"));
            subDirectories.Add(Combine(this._testDirectory, "abc", "789", "xyz"));
            subDirectories.Add(Combine(this._testDirectory, "def"));
            subDirectories.Add(Combine(this._testDirectory, "def", "kek"));
            subDirectories.Add(Combine(this._testDirectory, "def", "kek", "lel"));
            subDirectories.Add(Combine(this._testDirectory, "def", "kek", "lel", "mem"));
            subDirectories.Add(Combine(this._testDirectory, "ghi"));

            this._subDirectories = subDirectories;
            SIO.Directory.CreateDirectory(this._testDirectory);
            foreach (var directory in this._subDirectories)
            {
                SIO.Directory.CreateDirectory(directory);
            }
        }

        [OneTimeTearDown]
        public void DeleteDirectoryStructure()
        {
            SIO.Directory.Delete(_testDirectory!, true);
        }

        [Test]
        public void GetDirectories()
        {
            var expected = new string[] { Combine(this._testDirectory, "abc"), Combine(this._testDirectory, "def"), Combine(this._testDirectory, "ghi") };
            var directory = new Directory(this._testDirectory);

            var actualDirectories = directory.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly);

            var actual = actualDirectories.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void GetDirectories_AllSubDirectories()
        {
            var directory = new Directory(this._testDirectory);

            var actualDirectories = directory.GetDirectories("*", SIO.SearchOption.AllDirectories);

            var actual = actualDirectories.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(this._subDirectories));
        }

        [Test]
        public void GetDirectories_WithPattern()
        {
            var expected = new string[] { Combine(this._testDirectory, "abc") };
            var directory = new Directory(this._testDirectory);

            var actualDirectories = directory.GetDirectories("a??", SIO.SearchOption.TopDirectoryOnly);

            var actual = actualDirectories.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void GetDirectories_WithPattern_NoMatch()
        {
            var directory = new Directory(this._testDirectory);

            var actual = directory.GetDirectories("z*", SIO.SearchOption.AllDirectories);

            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void GetDirectories_WithPattern_AllSubDirectories()
        {
            var expected = new string[] { Combine(this._testDirectory, "def"), Combine(this._testDirectory, "def", "kek"), Combine(this._testDirectory, "def", "kek", "lel"), Combine(this._testDirectory, "def", "kek", "lel", "mem") };
            var directory = new Directory(this._testDirectory);

            var actualDirectories = directory.GetDirectories("?e?", SIO.SearchOption.AllDirectories);

            var actual = actualDirectories.Select(x => x.FullName);
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
