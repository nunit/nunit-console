// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    internal static class TestPackageFactory
    {
        internal const string FakeProject = "a.nunit";
        private const string FakeAssembly = "a.dll";
        private const string UnknownExtension = "a.junk";

        public static TestPackage OneAssembly()
        {
            return new TestPackage(FakeAssembly);
        }

        public static TestPackage TwoAssemblies()
        {
            return new TestPackage(FakeAssembly, FakeAssembly);
        }

        public static TestPackage OneProject()
        {
            return new TestPackage(FakeProject);
        }

        public static TestPackage TwoProjects()
        {
            return new TestPackage(FakeProject, FakeProject);
        }

        public static TestPackage OneProjectOneAssembly()
        {
            return new TestPackage(FakeProject, FakeAssembly);
        }

        public static TestPackage TwoProjectsOneAssembly()
        {
            return new TestPackage(FakeProject, FakeProject, FakeAssembly);
        }

        public static TestPackage TwoAssembliesOneProject()
        {
            return new TestPackage(FakeAssembly, FakeAssembly, FakeProject);
        }

        public static TestPackage OneUnknownExtension()
        {
            return new TestPackage(UnknownExtension);
        }

        public static TestPackage TwoUnknownExtension()
        {
            return new TestPackage(UnknownExtension, UnknownExtension);
        }

        public static TestPackage OneUnknownOneAssemblyOneProject()
        {
            return new TestPackage(UnknownExtension, FakeAssembly, FakeProject);
        }
    }
}
