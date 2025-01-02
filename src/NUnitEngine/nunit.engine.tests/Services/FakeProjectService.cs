// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    public class FakeProjectService : FakeService, IProjectService
    {
        private string _supportedExtension;
        private Dictionary<string, string[]> _projects = new Dictionary<string, string[]>();

        public FakeProjectService(string supportedExtension = ".nunit")
        {
            _supportedExtension = supportedExtension;
        }

        public void Add(string projectName, params string[] assemblies)
        {
            _projects.Add(projectName, assemblies);
        }

        void IProjectService.ExpandProjectPackage(TestPackage package)
        {
            if (package.Name == null)
            {
                throw new ArgumentException("Package must have a name", nameof(package));
            }

            if (_projects.TryGetValue(package.Name, out string[]? projects))
            {
                foreach (string assembly in projects)
                    package.AddSubPackage(new TestPackage(assembly));
            }
        }

        bool IProjectService.CanLoadFrom(string path)
        {
            return Path.GetExtension(path) == _supportedExtension;
        }
    }
}
