// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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

        public static TestPackage OneUnknownOneAssemblyOneProject()
        {
            return new TestPackage(new[] { UnknownExtension, FakeAssembly, FakeProject });
        }
    }
}
