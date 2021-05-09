// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt


using System.Collections.Generic;

namespace NUnit.Engine
{
    /// <summary>
    /// ITestPackage holds information about a set of test files to
    /// be loaded by a TestRunner. Each ITestPackage represents
    /// tests for one or more test files.
    /// 
    /// Upon construction, a package is given an ID (string), which
    /// remains unchanged for the lifetime of the ITestPackage instance.
    /// The package ID is passed to the test framework for use in generating
    /// test IDs.
    /// 
    /// A runner that reloads test assemblies and wants the ids to remain stable
    /// should avoid creating a new package but should instead use the original
    /// package, changing settings as needed. This gives the best chance for the
    /// tests in the reloaded assembly to match those originally loaded.
    /// </summary>
    public interface ITestPackage
    {
        /// <summary>
        /// Every test package gets a unique ID used to prefix test IDs within that package.
        /// </summary>
        /// <remarks>
        /// The generated ID is only unique for packages created within the same application domain.
        /// For that reason, NUnit pre-creates all test packages that will be needed.
        /// </remarks>
        string ID { get; }

        /// <summary>
        /// Gets the name of the package
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the path to the file containing tests. It may be
        /// an assembly or a recognized project type.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the list of SubPackages contained in this package
        /// </summary>
        IList<ITestPackage> SubPackages { get; }

        /// <summary>
        /// Gets the settings dictionary for this package.
        /// </summary>
        IDictionary<string, object> Settings { get; }

        /// <summary>
        /// Add a subpackage to the package.
        /// </summary>
        /// <param name="subPackage">The subpackage to be added</param>
        void AddSubPackage(ITestPackage subPackage);

        /// <summary>
        /// Add a subpackage to the package, specifying its name. This is
        /// the only way to add a named subpackage to the top-level package.
        /// </summary>
        /// <param name="packageName">The name of the subpackage to be added</param>
        ITestPackage AddSubPackage(string packageName);

        /// <summary>
        /// Add a setting to a package and all of its subpackages.
        /// </summary>
        /// <param name="name">The name of the setting</param>
        /// <param name="value">The value of the setting</param>
        /// <remarks>
        /// Once a package is created, subpackages may have been created
        /// as well. If you add a setting directly to the Settings dictionary
        /// of the package, the subpackages are not updated. This method is
        /// used when the settings are intended to be reflected to all the
        /// subpackages under the package.
        /// </remarks>
        void AddSetting(string name, object value);

        /// <summary>
        /// Return the value of a setting or a default, which
        /// is specified by the caller.
        /// </summary>
        /// <param name="name">The name of the setting</param>
        /// <param name="defaultSetting">The default value</param>
        /// <returns></returns>
        T GetSetting<T>(string name, T defaultSetting);
    }
}