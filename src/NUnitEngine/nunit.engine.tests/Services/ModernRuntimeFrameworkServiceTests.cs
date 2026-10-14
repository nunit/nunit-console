// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETFRAMEWORK
using System;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class ModernRuntimeFrameworkServiceTests
    {
        [Test]
        public void CurrentFrameworkMatchesExecutingRuntime()
        {
            using var service = new RuntimeFrameworkService();
            var framework = service.CurrentFramework as RuntimeFramework;

            Assert.That(framework, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(framework!.FrameworkName.Identifier, Is.EqualTo(FrameworkIdentifiers.NetCoreApp));
                Assert.That(framework.FrameworkVersion,
                    Is.EqualTo(new Version(Environment.Version.Major, Environment.Version.Minor)));
            });
        }
    }
}
#endif
