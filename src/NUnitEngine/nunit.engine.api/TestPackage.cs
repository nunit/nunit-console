﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Engine
{
    /// <summary>
    /// TestPackage holds information about a set of test files to
    /// be loaded by a TestRunner. Each TestPackage represents
    /// tests for one or more test files. TestPackages may be named
    /// or anonymous, depending on the constructor used.
    ///
    /// Upon construction, a package is given an ID (string), which
    /// remains unchanged for the lifetime of the TestPackage instance.
    /// The package ID is passed to the test framework for use in generating
    /// test IDs.
    ///
    /// A runner that reloads test assemblies and wants the ids to remain stable
    /// should avoid creating a new package but should instead use the original
    /// package, changing settings as needed. This gives the best chance for the
    /// tests in the reloaded assembly to match those originally loaded.
    /// </summary>
    [Serializable]
    public class TestPackage
    {
        /// <summary>
        /// Construct a top-level TestPackage that wraps one or more
        /// test files, contained as subpackages.
        /// </summary>
        /// <param name="testFiles">Names of all the test files</param>
        /// <remarks>
        /// Semantically equivalent to the IList constructor.
        /// </remarks>
        public TestPackage(params string[] testFiles)
        {
            InitializeSubPackages(testFiles);
        }

        /// <summary>
        /// Construct a top-level TestPackage that wraps one or more
        /// test files, contained as subpackages.
        /// </summary>
        /// <param name="testFiles">Names of all the test files.</param>
        /// <remarks>
        /// Semantically equivalent to the array constructor.
        /// </remarks>
        public TestPackage(IList<string> testFiles)
        {
            InitializeSubPackages(testFiles);
        }

        private void InitializeSubPackages(IList<string> testFiles)
        {
            foreach (string testFile in testFiles)
                AddSubPackage(testFile);
        }

        /// <summary>
        ///  Construct an empty TestPackage
        /// </summary>
        /// <remarks>Used in deserializing a package from xml.</remarks>
        public TestPackage()
        {
        }

        private static int _nextID = 0;

        private static string GetNextID()
        {
            return (_nextID++).ToString();
        }

        /// <summary>
        /// Every test package gets a unique ID used to prefix test IDs within that package.
        /// </summary>
        /// <remarks>
        /// The generated ID is only unique for packages created within the same application domain.
        /// For that reason, NUnit pre-creates all test packages that will be needed.
        /// </remarks>
        // TODO: Setter is temporarily public for use in deserialization.
        // We should find a better solution for setting the ID.
        public string ID { get; set; } = GetNextID();

        /// <summary>
        /// Gets the name of the package
        /// </summary>
        public string? Name
        {
            get { return FullName is null ? null : Path.GetFileName(FullName); }
        }

        /// <summary>
        /// Gets the path to the file containing tests. It may be
        /// an assembly or a recognized project type.
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Gets the list of SubPackages contained in this package
        /// </summary>
        public IList<TestPackage> SubPackages { get; } = new List<TestPackage>();

        /// <summary>
        /// Gets the settings dictionary for this package.
        /// </summary>
        public PackageSettings Settings { get; } = new PackageSettings();

        /// <summary>
        /// Return the value of a setting or a default.
        /// </summary>
        /// <param name="definition">The name and type of the setting</param>
        /// <param name="defaultSetting">The default value</param>
        public T GetSetting<T>(SettingDefinition<T> definition, T defaultSetting)
            where T : notnull
        {
            return Settings.GetSetting(definition, defaultSetting);
        }

        /// <summary>
        /// Add a subpackage to the package.
        /// </summary>
        /// <param name="subPackage">The subpackage to be added</param>
        public void AddSubPackage(TestPackage subPackage)
        {
            SubPackages.Add(subPackage);

            foreach (var setting in Settings)
                subPackage.AddSetting(setting);
        }

        /// <summary>
        /// Add a subpackage to the package, specifying its name. This is
        /// the only way to add a named subpackage to the top-level package.
        /// </summary>
        /// <param name="packageName">The name of the subpackage to be added</param>
        public TestPackage AddSubPackage(string packageName)
        {
            var subPackage = new TestPackage() { FullName = Path.GetFullPath(packageName) };
            SubPackages.Add(subPackage);

            return subPackage;
        }

        /// <summary>
        /// Add a setting to a package and all of its subpackages.
        /// </summary>
        /// <param name="setting">The setting to be added</param>
        /// <remarks>
        /// Once a package is created, subpackages may have been created
        /// as well. If you add a setting directly to the Settings dictionary
        /// of the package, the subpackages are not updated. This method is
        /// used when the settings are intended to be reflected to all the
        /// subpackages under the package.
        /// </remarks>
        public void AddSetting(PackageSetting setting)
        {
            Settings.Add(setting);
            foreach (var subPackage in SubPackages)
                subPackage.AddSetting(setting);
        }

        /// <summary>
        /// Add a setting to a package and all of its subpackages.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        /// <remarks>
        /// Once a package is created, subpackages may have been created
        /// as well. If you add a setting directly to the Settings dictionary
        /// of the package, the subpackages are not updated. This method is
        /// used when the settings are intended to be reflected to all the
        /// subpackages under the package.
        /// </remarks>
        public void AddSetting<T>(string name, T value)
            where T : notnull
        {
            AddSetting(new PackageSetting<T>(name, value));
        }
    }
}
