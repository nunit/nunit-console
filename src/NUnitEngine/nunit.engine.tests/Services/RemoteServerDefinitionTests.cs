// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>Checks explicit selection and definition parsing without contacting any endpoint.</summary>
    public sealed class RemoteServerDefinitionTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "remote-definition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        [TestCase("tcp://127.0.0.1:12345", "127.0.0.1")]
        [TestCase("tcp://[::1]:12345", "::1")]
        public void LoadsLiteralLoopbackDefinition(string endpoint, string address)
        {
            var definition = RemoteServerDefinition.Load(Write(endpoint));

            Assert.That(definition.Name, Is.EqualTo("office"));
            Assert.That(definition.Endpoint, Is.EqualTo(new IPEndPoint(IPAddress.Parse(address), 12345)));
            Assert.That(definition.ConnectTimeout, Is.EqualTo(10000));
            Assert.That(definition.OperationTimeout, Is.EqualTo(30000));
            Assert.That(definition.RunReadTimeout, Is.EqualTo(120000));
        }

        [TestCase("tcp://192.0.2.1:12345")]
        [TestCase("tcp://localhost:12345")]
        [TestCase("https://127.0.0.1:12345")]
        [TestCase("tcp://user:password@127.0.0.1:12345")]
        [TestCase("tcp://127.0.0.1:12345/path")]
        [TestCase("tcp://127.0.0.1:0")]
        [TestCase("tcp://127.0.0.1:12345?query=value")]
        [TestCase("tcp://127.0.0.1:12345#fragment")]
        public void RejectsEndpointOutsideInitialTransportContract(string endpoint)
        {
            Assert.That(() => RemoteServerDefinition.Load(Write(endpoint)), Throws.TypeOf<InvalidDataException>());
        }

        [TestCase("")]
        [TestCase("other::agent")]
        [TestCase("white space")]
        public void RejectsInvalidServerName(string name)
        {
            Assert.That(() => RemoteServerDefinition.Load(Write("tcp://127.0.0.1:12345", new XAttribute("name", name))),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void RejectsRelativeDefinitionPath()
        {
            Assert.That(() => RemoteServerDefinition.Load("server.xml"), Throws.ArgumentException);
        }

        [Test]
        public void RejectsUnknownDefinitionAttributes()
        {
            Assert.That(() => RemoteServerDefinition.Load(Write("tcp://127.0.0.1:12345", new XAttribute("typo", "value"))),
                Throws.TypeOf<InvalidDataException>());
        }

        [TestCase("unexpected text")]
        [TestCase("<server />")]
        public void RejectsUnexpectedDefinitionContent(string content)
        {
            string path = Write("tcp://127.0.0.1:12345");
            File.WriteAllText(path, "<nunit-remote-server name='office' endpoint='tcp://127.0.0.1:12345'>"
                + content + "</nunit-remote-server>");
            Assert.That(() => RemoteServerDefinition.Load(path), Throws.TypeOf<InvalidDataException>());
        }

        [TestCase("connectTimeout", "0")]
        [TestCase("operationTimeout", "0")]
        [TestCase("runReadTimeout", "-1")]
        [TestCase("runReadTimeout", "2147483648")]
        [TestCase("operationTimeout", "invalid")]
        public void RejectsInvalidTimeouts(string name, string value)
        {
            Assert.That(() => RemoteServerDefinition.Load(Write("tcp://127.0.0.1:12345", new XAttribute(name, value))),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void AllowsExplicitInfiniteRunReadTimeout()
        {
            var definition = RemoteServerDefinition.Load(Write("tcp://127.0.0.1:12345", new XAttribute("runReadTimeout", 0)));
            Assert.That(definition.RunReadTimeout, Is.Zero);
        }

        [Test]
        public void RejectsDtd()
        {
            string path = Path.Combine(_directory, "server.xml");
            File.WriteAllText(path, "<!DOCTYPE nunit-remote-server [<!ENTITY name 'office'>]><nunit-remote-server name='&name;' endpoint='tcp://127.0.0.1:12345'/>");
            Assert.That(() => RemoteServerDefinition.Load(path), Throws.TypeOf<XmlException>());
        }

        [TestCase("Any", true)]
        [TestCase("LocalProcess", true)]
        [TestCase("RemoteProcess", false)]
        [TestCase("InProcess", false)]
        public void RespectsExplicitAgentType(string type, bool allowsLocal)
        {
            var package = new TestPackage("test.dll");
            package.AddSetting("TestAgentType", type, true);
            Assert.That(RemoteAgentSettings.AllowsLocalProcess(package), Is.EqualTo(allowsLocal));
        }

        [Test]
        public void UnconfiguredProviderOffersNoRemoteAgents()
        {
            var provider = new RemoteProcessAgentProvider();
            Assert.That(provider.AvailableAgents, Is.Empty);
            Assert.That(provider.IsAgentAvailable(new TestPackage("test.dll")), Is.False);
        }

        [Test]
        public void SelectedServerPreventsLocalLauncherProbe()
        {
            var package = new TestPackage("test.dll");
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("missing.xml"), true);
            using var agency = new TestAgentService();
            var provider = new LocalProcessAgentProvider(agency);

            // The unstarted agency has no ExtensionService. A local launcher probe would throw.
            Assert.That(provider.IsAgentAvailable(package), Is.False);
            Assert.That(provider.GetAgentsForPackage(package), Is.Empty);
        }

        [TestCase("invalid")]
        [TestCase("99")]
        public void RejectsInvalidRequestedType(string type)
        {
            var package = new TestPackage("test.dll");
            package.AddSetting("TestAgentType", type, true);
            Assert.That(() => RemoteAgentSettings.RequestedType(package), Throws.ArgumentException);
        }

        [Test]
        public void RejectsLocalTypeWithServerDefinition()
        {
            var package = new TestPackage("test.dll");
            package.AddSetting("TestAgentType", "LocalProcess", true);
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("missing.xml"), true);
            Assert.That(() => RemoteAgentSettings.RequestedType(package), Throws.ArgumentException);
        }

        [Test]
        public void ExplicitRemoteTypeRequiresDefinition()
        {
            var package = new TestPackage("test.dll");
            package.AddSetting("TestAgentType", "RemoteProcess", true);
            var provider = new RemoteProcessAgentProvider();
            Assert.That(() => provider.IsAgentAvailable(package), Throws.InvalidOperationException);
        }

        private string Write(string endpoint, XAttribute? extra = null)
        {
            var root = new XElement("nunit-remote-server", new XAttribute("name", "office"), new XAttribute("endpoint", endpoint));
            if (extra is not null)
                root.SetAttributeValue(extra.Name, extra.Value);
            string path = Path.Combine(_directory, "server.xml");
            File.WriteAllText(path, root.ToString());
            return path;
        }
    }
}
