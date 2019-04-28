// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
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

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    internal static class TestPackageFactory
    {
        internal const string FakeProject = "a.nunit";
        private const string FakeAssembly = "a.dll";
        private const string UnknownExtension = "a.junk";

        public static TestPackage OneAssemblyStringCtor()
        {
            return new TestPackage(FakeAssembly);
        }

        public static TestPackage OneAssemblyListCtor()
        {
            return new TestPackage(new [] { FakeAssembly });
        }

        public static TestPackage TwoAssemblies()
        {
            return new TestPackage(new[] { FakeAssembly, FakeAssembly });
        }

        public static TestPackage OneProjectStringCtor()
        {
            return new TestPackage(FakeProject);
        }

        public static TestPackage OneProjectListCtor()
        {
            return new TestPackage(new[] { FakeProject });
        }

        public static TestPackage TwoProjects()
        {
            return new TestPackage(new[] { FakeProject, FakeProject });
        }

        public static TestPackage OneProjectOneAssembly()
        {
            return new TestPackage(new[] { FakeProject, FakeAssembly });
        }

        public static TestPackage TwoProjectsOneAssembly()
        {
            return new TestPackage(new[] { FakeProject, FakeProject, FakeAssembly });
        }

        public static TestPackage TwoAssembliesOneProject()
        {
            return new TestPackage(new[] { FakeAssembly, FakeAssembly, FakeProject });
        }

        public static TestPackage OneUnknownExtension()
        {
            return new TestPackage(new[] { UnknownExtension });
        }

        public static TestPackage TwoUnknownExtension()
        {
            return new TestPackage(new[] { UnknownExtension, UnknownExtension });
        }

        public static TestPackage OneAssemblyOneProjectOneUnknown()
        {
            return new TestPackage(new[] { UnknownExtension, FakeAssembly, FakeProject });
        }
    }
}
