// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;

// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Engine.Extensibility;

namespace NUnit.Extensibility.Wrappers
{
    /// <summary>
    /// Wrapper class for project loaders which use the NUnit3 Engine API
    /// </summary>
    public class ProjectLoaderWrapper : ExtensionWrapper, IProjectLoader
    {
        public ProjectLoaderWrapper(object projectLoader) : base(projectLoader)
        {
            log.Debug("Creating ProjectLoaderWrapper");
        }

        public bool CanLoadFrom(string path)
        {
            log.Debug("CanLoadFrom");
            return Invoke<bool>(nameof(CanLoadFrom), path);
        }

        public IProject LoadFrom(string path)
        {
            log.Debug("LoadFrom");
            return new ProjectWrapper(Invoke<object>(nameof(LoadFrom), path));
        }

        public class ProjectWrapper : ExtensionWrapper, IProject
        {
            public ProjectWrapper(object wrappedInstance) : base(wrappedInstance)
            {
            }

            public string ProjectPath => GetProperty<string>(nameof(ProjectPath)).ShouldNotBeNull();

            public string ActiveConfigName => GetProperty<string>(nameof(ActiveConfigName)).ShouldNotBeNull();

            public IList<string> ConfigNames => GetProperty<IList<string>>(nameof(ConfigNames)).ShouldNotBeNull();

            public TestPackage GetTestPackage()
            {
                return new TestPackage();
                //return CreateTestPackage(Invoke<object>(nameof(GetTestPackage)));
            }

            public TestPackage GetTestPackage(string configName)
            {
                return new TestPackage();
                //return CreateTestPackage(Invoke<object>(nameof(GetTestPackage), configName));
            }
        }

        // TODO: This adhoc interface exposes only the TestPackage methods
        // which are used by the ProjectService. We should consider adding
        // a more complete interface to the API assembly and possibly also
        // moving the implementation code to the common assembly.
        public interface ITestPackage
        {
            IDictionary<string, object> Settings { get; }
            IList<TestPackage> SubPackages { get; }
            T GetSetting<T>(string name, T defaultValue);
        }

        public class TestPackageWrapper : ExtensionWrapper, ITestPackage
        {
            public TestPackageWrapper(object wrappedInstance) : base(wrappedInstance)
            {
            }

            public IDictionary<string, object> Settings => throw new NotImplementedException();

            public IList<TestPackage> SubPackages => throw new NotImplementedException();

            public T GetSetting<T>(string name, T defaultValue)
            {
                if (defaultValue == null)
                    throw new ArgumentNullException(nameof(defaultValue));
                return Invoke<T>(nameof(GetSetting), name, defaultValue);
            }

            public TestPackage GetTestPackage(string configName)
            {
                return Invoke<TestPackage>(nameof(GetTestPackage), configName);
            }
        }
    }
}
