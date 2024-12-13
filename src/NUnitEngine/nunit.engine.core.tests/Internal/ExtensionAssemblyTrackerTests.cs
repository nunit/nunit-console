// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.ComponentModel;
using System.Reflection;
using NUnit.Engine.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Internal.Tests
{
    public class ExtensionAssemblyTrackerTests
    {
        private static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionAssemblyTrackerTests).Assembly;
        private static readonly string THIS_ASSEMBLY_PATH = THIS_ASSEMBLY.Location;
        private static readonly string THIS_ASSEMBLY_NAME = THIS_ASSEMBLY.GetName().Name;
        private static readonly Version THIS_ASSEMBLY_VERSION = THIS_ASSEMBLY.GetName().Version;
        private static readonly ExtensionAssembly TEST_EXTENSION_ASSEMBLY = 
            new ExtensionAssembly(THIS_ASSEMBLY_PATH, false, THIS_ASSEMBLY_NAME, THIS_ASSEMBLY_VERSION);

        private ExtensionAssemblyTracker _tracker;

        [SetUp]
        public void CreateTracker()
        {
            _tracker = new ExtensionAssemblyTracker();
        }

        [Test]
        public void AddToList()
        {
            _tracker.Add(TEST_EXTENSION_ASSEMBLY);

            Assert.That(_tracker.Count, Is.EqualTo(1));
            Assert.That(_tracker[0].FilePath, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(_tracker[0].AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(_tracker[0].AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }

        [Test]
        public void AddUpdatesNameIndex()
        {
            _tracker.Add(TEST_EXTENSION_ASSEMBLY);

            Assert.That(_tracker.ByName.ContainsKey(THIS_ASSEMBLY_NAME));
            Assert.That(_tracker.ByName[THIS_ASSEMBLY_NAME].AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(_tracker.ByName[THIS_ASSEMBLY_NAME].FilePath, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(_tracker.ByName[THIS_ASSEMBLY_NAME].AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }
        [Test]
        public void AddUpdatesPathIndex()
        {
            _tracker.Add(TEST_EXTENSION_ASSEMBLY);

            Assert.That(_tracker.ByPath.ContainsKey(THIS_ASSEMBLY_PATH));
            Assert.That(_tracker.ByPath[THIS_ASSEMBLY_PATH].AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(_tracker.ByPath[THIS_ASSEMBLY_PATH].FilePath, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(_tracker.ByPath[THIS_ASSEMBLY_PATH].AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }

        [Test]
        public void AddDuplicatePathThrowsArgumentException()
        {
            _tracker.Add(TEST_EXTENSION_ASSEMBLY);

            Assert.That(() => 
                _tracker.Add(TEST_EXTENSION_ASSEMBLY), 
                Throws.TypeOf<System.ArgumentException>());
        }

        [Test]
        public void AddDuplicateAssemblyNameThrowsArgumentException()
        {
            _tracker.Add(TEST_EXTENSION_ASSEMBLY);

            Assert.That(() => _tracker.Add(new ExtensionAssembly("Some/Other/Path", false, THIS_ASSEMBLY_NAME, THIS_ASSEMBLY_VERSION)),
                Throws.TypeOf<System.ArgumentException>());
        }
    }
}
