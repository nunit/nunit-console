// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.TestRunnerFactoryTests
{
    /// <summary>Separates backend assembly validation from unchanged frontend local-file validation.</summary>
    public class RemoteRunnerSelectionTests
    {
        private TestRunnerFactory _factory;
        private TestAgentService _agency;

        [SetUp]
        public void CreateFactory()
        {
            var services = new ServiceContext();
            _agency = new TestAgentService();
            services.Add(_agency);
            _factory = new TestRunnerFactory();
            services.Add(_factory);
        }

        [TearDown]
        public void DisposeServices()
        {
            _factory.Dispose();
            _agency.Dispose();
        }

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void RemoteAssemblyDoesNotRequireAFrontendCopy(bool serverDefinition, bool skipNonTestAssemblies)
        {
            var package = MissingAssembly();
            if (serverDefinition)
                package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(Path.GetFullPath("selected-server.xml")), false);
            else
                package.AddSetting(RemoteAgentSettings.AgentType.WithValue("RemoteProcess"), false);
            package.AddSetting(SettingDefinitions.SkipNonTestAssemblies.WithValue(skipNonTestAssemblies), false);

            using var runner = _factory.MakeTestRunner(package);

            Assert.That(runner, Is.TypeOf<ProcessRunner>());
        }

        [TestCase("Any")]
        [TestCase("LocalProcess")]
        [TestCase("InProcess")]
        public void MissingLocalAssemblyRemainsInvalid(string requestedType)
        {
            var package = MissingAssembly();
            package.AddSetting(RemoteAgentSettings.AgentType.WithValue(requestedType), false);

            using var runner = _factory.MakeTestRunner(package);

            Assert.That(runner, Is.TypeOf<InvalidAssemblyTestRunner>());
        }

        private static TestPackage MissingAssembly()
        {
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"), "RemoteTests.dll");
            Assert.That(File.Exists(path), Is.False);
            return new TestPackage(path).SubPackages[0];
        }
    }
}
