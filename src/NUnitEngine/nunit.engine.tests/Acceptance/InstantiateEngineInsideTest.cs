// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Acceptance
{
    class InstantiateEngineInsideTest
    {

        /// <summary>
        /// Historically it wasn't possible to instantiate a full engine inside
        /// a running NUnit test due to issues with URI collisions for
        /// inter-process communication.
        ///
        /// This is a useful feature to maintain, to allow runners to create full
        /// end-to-end acceptance-style tests in NUnit.
        /// </summary>
        [Test]
        public void CanInstantiateEngineInsideTest()
        {
            Assert.DoesNotThrow(() =>
            {
                var engine = TestEngineActivator.CreateInstance();
                var mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");
                var package = new TestPackage(mockAssemblyPath);
                engine.GetRunner(package).Run(null, null);
            });
        }

    }
}
