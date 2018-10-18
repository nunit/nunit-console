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

#if !NETCOREAPP1_1
using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Engine.Extensibility;
using System.IO;

namespace NUnit.Engine.Services.Tests
{
    public class ExtensionServiceTests
    {
        private ExtensionService _serviceClass;
        private IExtensionService _serviceInterface;

#pragma warning disable 414
        private static readonly string[] KnownExtensionPointPaths = {
            "/NUnit/Engine/TypeExtensions/IDriverFactory",
            "/NUnit/Engine/TypeExtensions/IProjectLoader",
            "/NUnit/Engine/TypeExtensions/IResultWriter",
            "/NUnit/Engine/TypeExtensions/ITestEventListener",
            "/NUnit/Engine/TypeExtensions/IService",
            "/NUnit/Engine/NUnitV2Driver"
        };

        private static readonly Type[] KnownExtensionPointTypes = {
            typeof(IDriverFactory),
            typeof(IProjectLoader),
            typeof(IResultWriter),
            typeof(ITestEventListener),
            typeof(IService),
            typeof(IFrameworkDriver)
        };

        private static readonly int[] KnownExtensionPointCounts = { 1, 1, 1, 2, 1, 1 };
#pragma warning restore 414

        [SetUp]
        public void CreateService()
        {
            _serviceInterface = _serviceClass = new ExtensionService();

            // Rather than actually starting the service, which would result
            // in finding the extensions actually in use on the current system,
            // we simulate the start using this assemblies dummy extensions.
            _serviceClass.FindExtensionPoints(typeof(TestEngine).Assembly);
            _serviceClass.FindExtensionPoints(typeof(ITestEngine).Assembly);

            _serviceClass.FindExtensionsInAssembly(new ExtensionAssembly(GetType().Assembly.Location, false));
        }

        [Test]
        public void AllExtensionPointsAreKnown()
        {
            Assert.That(_serviceInterface.ExtensionPoints.Select(ep => ep.Path), Is.EquivalentTo(KnownExtensionPointPaths));
        }

        [Test, Sequential]
        public void CanGetExtensionPointByPath(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _serviceInterface.GetExtensionPoint(path);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

        [Test, Sequential]
        public void CanGetExtensionPointByType(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointTypes))] Type type)
        {
            var ep = _serviceClass.GetExtensionPoint(type);
            Assert.NotNull(ep);
            Assert.That(ep.Path, Is.EqualTo(path));
            Assert.That(ep.TypeName, Is.EqualTo(type.FullName));
        }

#pragma warning disable 414
        private static readonly string[] KnownExtensions = {
            "NUnit.Engine.Tests.DummyFrameworkDriverExtension",
            "NUnit.Engine.Tests.DummyProjectLoaderExtension",
            "NUnit.Engine.Tests.DummyResultWriterExtension",
            "NUnit.Engine.Tests.DummyEventListenerExtension",
            "NUnit.Engine.Tests.DummyServiceExtension",
            "NUnit.Engine.Tests.V2DriverExtension"
        };
#pragma warning restore 414

        [TestCaseSource(nameof(KnownExtensions))]
        public void CanListExtensions(string typeName)
        {
            Assert.That(_serviceInterface.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo(typeName)
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test, Sequential]
        public void ExtensionsAreAddedToExtensionPoint(
            [ValueSource(nameof(KnownExtensionPointPaths))] string path,
            [ValueSource(nameof(KnownExtensionPointCounts))] int extensionCount)
        {
            var ep = _serviceClass.GetExtensionPoint(path);
            Assume.That(ep, Is.Not.Null);

            Assert.That(ep.Extensions.Count, Is.EqualTo(extensionCount));
        }

        [Test]
        public void ExtensionMayBeDisabledByDefault()
        {
            Assert.That(_serviceInterface.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).False);
        }

        [Test]
        public void DisabledExtensionMayBeEnabled()
        {
            _serviceInterface.EnableExtension("NUnit.Engine.Tests.DummyDisabledExtension", true);

            Assert.That(_serviceInterface.Extensions,
                Has.One.Property(nameof(ExtensionNode.TypeName)).EqualTo("NUnit.Engine.Tests.DummyDisabledExtension")
                   .And.Property(nameof(ExtensionNode.Enabled)).True);
        }

        [Test]
        public void FailsGracefullyLoadingOtherFrameworkExtensionAssembly()
        {
#if NETCOREAPP2_0
            string other = "net35"; // Attempt to load the .NET 3.5 version of the extensions from the .NET Core 2.0 tests
#elif NET35
            string other = "netcoreapp2.0"; // Attempt to load the .NET Core 2.0 version of the extensions from the .NET 3.5 tests
#endif
            var file = new FileInfo(GetType().Assembly.Location);
            var assemblyName = Path.Combine(Path.Combine(file.Directory.Parent.FullName, other), "nunit.engine.tests.dll");
            Assert.That(assemblyName, Does.Exist);

            Assert.That(() =>
            {
                var service = new ExtensionService();
                service.FindExtensionPoints(typeof(TestEngine).Assembly);
                service.FindExtensionPoints(typeof(ITestEngine).Assembly);
                service.FindExtensionsInAssembly(new ExtensionAssembly(assemblyName, false));
            }, Throws.TypeOf<NUnitEngineException>());
        }
    }
}
#endif