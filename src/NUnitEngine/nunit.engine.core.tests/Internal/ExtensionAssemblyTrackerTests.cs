// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            _tracker.AddOrUpdate(TEST_EXTENSION_ASSEMBLY);

            Assert.That(_tracker.Count, Is.EqualTo(1));
            var assembly = _tracker.Single();

            Assert.That(assembly.FilePath, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(assembly.AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(assembly.AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }

        [Test]
        public void AddUpdatesPathIndex()
        {
            _tracker.AddOrUpdate(TEST_EXTENSION_ASSEMBLY);

            Assert.That(_tracker.ContainsPath(THIS_ASSEMBLY_PATH));
        }

        private static IEnumerable<TestCaseData> TestCasesAddNewerAssemblyUpdatesExistingInformation()
        {
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major + 1, THIS_ASSEMBLY_VERSION.Minor, THIS_ASSEMBLY_VERSION.Build));
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major, THIS_ASSEMBLY_VERSION.Minor + 1, THIS_ASSEMBLY_VERSION.Build));
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major, THIS_ASSEMBLY_VERSION.Minor, THIS_ASSEMBLY_VERSION.Build + 1));
        }

        [TestCaseSource(nameof(TestCasesAddNewerAssemblyUpdatesExistingInformation))]
        public void AddNewerAssemblyUpdatesExistingInformation(Version newVersion)
        {
            _tracker.AddOrUpdate(TEST_EXTENSION_ASSEMBLY);

            string newAssemblyPath = "/path/to/new/assembly";
            var newerAssembly = new ExtensionAssembly(newAssemblyPath, false, THIS_ASSEMBLY_NAME, newVersion);

            _tracker.AddOrUpdate(newerAssembly);

            Assert.That(_tracker.Count, Is.EqualTo(1));
            Assert.That(_tracker.ContainsPath(newAssemblyPath));

            var assembly = _tracker.Single();
            Assert.That(assembly.FilePath, Is.EqualTo(newAssemblyPath));
            Assert.That(assembly.AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(assembly.AssemblyVersion, Is.EqualTo(newVersion));
        }

        private static IEnumerable<TestCaseData> AddNewerAssemblyUpdatesExistingInformationTestCases()
        {
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major - 1, THIS_ASSEMBLY_VERSION.Minor, THIS_ASSEMBLY_VERSION.Build));
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major, THIS_ASSEMBLY_VERSION.Minor - 1, THIS_ASSEMBLY_VERSION.Build));
            yield return new TestCaseData(new Version(THIS_ASSEMBLY_VERSION.Major, THIS_ASSEMBLY_VERSION.Minor, THIS_ASSEMBLY_VERSION.Build));
        }

        [TestCaseSource(nameof(AddNewerAssemblyUpdatesExistingInformationTestCases))]
        public void AddOlderOrSameAssemblyDoesNotUpdateExistingInformation(Version newVersion)
        {
            _tracker.AddOrUpdate(TEST_EXTENSION_ASSEMBLY);

            string newAssemblyPath = "/path/to/new/assembly";
            var newerAssembly = new ExtensionAssembly(newAssemblyPath, false, THIS_ASSEMBLY_NAME, newVersion);

            _tracker.AddOrUpdate(newerAssembly);

            Assert.That(_tracker.Count, Is.EqualTo(1));
            Assert.That(_tracker.ContainsPath(newAssemblyPath));

            var assembly = _tracker.Single();
            Assert.That(assembly.FilePath, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(assembly.AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
            Assert.That(assembly.AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }
    }
}
