// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Verifies that built-in and independently compiled providers share the public
    /// API contract rather than an engine-local interface with the same name.
    /// </summary>
    public class AgentProviderContractTests
    {
        [Test]
        public void ProviderContractIsDefinedOnlyInThePublicApi()
        {
            var contract = typeof(ITestAgentProvider);

            Assert.That(contract.Assembly, Is.SameAs(typeof(ITestEngine).Assembly));
            Assert.That(contract.IsPublic, Is.True);
            Assert.That(typeof(TestEngine).Assembly.GetType(contract.FullName!), Is.Null);
        }

        [TestCase(typeof(TestAgency))]
        [TestCase(typeof(InProcessAgentProvider))]
        [TestCase(typeof(LocalProcessAgentProvider))]
        [TestCase(typeof(RemoteProcessAgentProvider))]
        public void BuiltInProviderImplementsThePublicContract(Type providerType)
        {
            Assert.That(typeof(ITestAgentProvider).IsAssignableFrom(providerType), Is.True);
        }
    }
}
