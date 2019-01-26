using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    internal static class TestPackageFactory
    {
        public static TestPackage OneAssembly()
        {
            return new TestPackage("a.dll");
        }

        public static TestPackage TwoAssemblies()
        {
            return new TestPackage(new[] { "a.dll", "b.dll" });
        }
    }
}
