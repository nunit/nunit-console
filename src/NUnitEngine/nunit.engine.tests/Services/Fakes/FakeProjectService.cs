// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

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
