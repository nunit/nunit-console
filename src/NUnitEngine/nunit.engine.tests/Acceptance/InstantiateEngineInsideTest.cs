﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine
{
    internal class InstantiateEngineInsideTest
    {
        /// <summary>
        /// Historically it wasn't possible to instantiate a full engine inside
        /// a running NUnit test due to issues with URI collisions for
        /// inter-process communication.
        ///
        /// This is a useful feature to maintain, to allow runners to create full
        /// end-to-end acceptance-style tests in NUnit.
        /// </summary>
        [Test, Platform("Net")] //TODO: Make test work on .NET Core. Tracked as https://github.com/nunit/nunit-console/issues/946
        public void CanInstantiateEngineInsideTest()
        {
            Assert.DoesNotThrow(() =>
            {
                using (var engine = new TestEngine())
                {
                    var mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");
                    var package = new TestPackage(mockAssemblyPath).SubPackages[0];
                    using (var runner = engine.GetRunner(package))
                    {
                        runner.Run(null, TestFilter.Empty);
                    }
                }
            });
        }
    }
}
