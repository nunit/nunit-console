// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.Framework;

namespace NUnit.Engine.Drivers.Tests
{
    /// <summary>Checks the framework versions selected by the built-in NUnit driver.</summary>
    public class NUnit3DriverFactoryTests
    {
        [TestCase("nunit.framework", 3, true)]
        [TestCase("nunit.framework", 4, true)]
        [TestCase("nunit.framework", 5, true)]
        [TestCase("NUNIT.FRAMEWORK", 5, true)]
        [TestCase("nunit.framework", 2, false)]
        [TestCase("nunit.framework", 6, false)]
        [TestCase("other.framework", 5, false)]
        public void SelectsSupportedFrameworks(string name, int major, bool supported)
        {
            var reference = new AssemblyName { Name = name, Version = new Version(major, 0, 0, 0) };
            Assert.That(new NUnit3DriverFactory().IsSupportedTestFramework(reference), Is.EqualTo(supported));
        }
    }
}
