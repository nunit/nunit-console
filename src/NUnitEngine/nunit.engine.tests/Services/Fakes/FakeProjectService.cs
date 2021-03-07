// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services.Tests.Fakes
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
            if (_projects.ContainsKey(package.Name))
            {
                foreach (string assembly in _projects[package.Name])
                    package.AddSubPackage(new TestPackage(assembly));
            }
        }

        bool IProjectService.CanLoadFrom(string path)
        {
            return Path.GetExtension(path) == _supportedExtension;
        }
    }
}
