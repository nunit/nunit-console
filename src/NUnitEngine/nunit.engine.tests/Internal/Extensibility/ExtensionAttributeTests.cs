// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Extensibility
{
    public class ExtensionAttributeTests
    {
        [Test]
        public void IsEnabledByDefault()
        {
            Assert.That(new ExtensionAttribute().Enabled, Is.True);
        }

        [Test]
        public void MayBeExplicitlyDisabled()
        {
            var attr = new ExtensionAttribute() { Enabled = false };
            Assert.That(attr.Enabled, Is.False);
        }

        [Test]
        public void MayBeExplicitlyEnabled()
        {
            var attr = new ExtensionAttribute() { Enabled = true };
            Assert.That(attr.Enabled, Is.True);
        }
    }
}

