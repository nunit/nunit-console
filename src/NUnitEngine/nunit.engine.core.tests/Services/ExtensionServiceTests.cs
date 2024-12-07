// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NSubstitute;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class ExtensionServiceTests
    {
        private ExtensionManager _extensionManager;
        private ExtensionService _extensionService;

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
            _extensionManager = Substitute.For<ExtensionManager>();
            _extensionService = new ExtensionService(_extensionManager);
        }

        [Test]
        public void StartServiceInitializesExtensionManager()
        {
            Assembly hostAssembly = typeof(ExtensionService).Assembly;

            _extensionService.StartService();

            _extensionManager.ReceivedWithAnyArgs().FindExtensionPoints(typeof(ExtensionService).Assembly, typeof(ITestEngine).Assembly);
            _extensionManager.Received().FindStandardExtensions(hostAssembly);
            Assert.That(_extensionService.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void StartServiceInitializesExtensionManagerUsingAdditionalDirectories()
        {
            Assembly hostAssembly = typeof(ExtensionService).Assembly;
            _extensionService.StartService();

            var tempPath = Path.GetTempPath();
            _extensionService.FindExtensions(tempPath);

            _extensionManager.Received().FindExtensions(tempPath);
            Assert.That(_extensionService.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void GetExtensionPointCallsExtensionManager()
        {
            ((IExtensionService)_extensionService).GetExtensionPoint("SOMEPATH");
            _extensionManager.Received().GetExtensionPoint("SOMEPATH");
        }

        [Test]
        public void GetExtensionNodesCallsExtensionManager()
        {
            ((IExtensionService)_extensionService).GetExtensionNodes("SOMEPATH");
            _extensionManager.Received().GetExtensionNodes("SOMEPATH");
        }

        [Test]
        public void EnableExtensionCallsExtensionManager()
        {
            _extensionService.EnableExtension("TYPENAME", true);
            _extensionManager.Received().EnableExtension("TYPENAME", true);
        }

        [Test]
        public void GetExtensionsTCallsExtensionManager()
        {
            _extensionService.GetExtensions<IFrameworkDriver>();
            _extensionManager.Received().GetExtensions<IFrameworkDriver>();
        }

        [Test]
        public void GetExtensionNodeCallsExtensionManager()
        {
            _extensionService.GetExtensionNode("SOMEPATH");
            _extensionManager.Received().GetExtensionNode("SOMEPATH");
        }

        [Test]
        public void GetExtensionNodesTCallsExtensionManager()
        {
            _extensionService.GetExtensionNodes<IFrameworkDriver>();
            _extensionManager.Received().GetExtensionNodes<IFrameworkDriver>();
        }

    }
}
