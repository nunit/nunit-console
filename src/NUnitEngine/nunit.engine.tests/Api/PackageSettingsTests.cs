// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.Engine.Api
{
    public class PackageSettingsTests
    {
        private PackageSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new PackageSettings();
        }

        [Test]
        public static void LookupSettingDefinitionSucceeds()
        {
            var definition = SettingDefinitions.MaxAgents;
            var lookup = SettingDefinitions.Lookup("MaxAgents");

            Assert.That(lookup, Is.SameAs(definition));
        }

        [Test]
        public static void LookupSettingDefinitionFails()
        {
            var lookup = SettingDefinitions.Lookup("NonExistent");

            Assert.That(lookup, Is.Null);
        }

        [Test]
        public void AddKnownSettingUsingSettingDefinition()
        {
            _settings.Add(SettingDefinitions.MaxAgents.WithValue(42));
            Assert.That(_settings.GetSetting("MaxAgents"), Is.EqualTo(42));
        }

        [Test]
        public void AddKnownSettingUsingSettingDefinitionThrowsWithWrongType()
        {
            Assert.That(() => _settings.Add(SettingDefinitions.MaxAgents.WithValue("42")), Throws.ArgumentException);
        }

        //[TestCase("ActiveConfig", "Release")]
        //public void AddKnownSettingUsingName<T>(string name, T value)
        //    where T : notnull
        //{
        //    var setting = SettingDefinitions.Lookup(name);
        //    Assert.That(setting, Is.Not.Null);
        //    var type = setting.ValueType;
        //    Assert.That(type, Is.EqualTo(typeof(T)));

        //    _settings.Add<T>(name, value);
        //}

        [Test]
        public void AddKnownStringSettingUsingName()
        {
            _settings.Add("ActiveConfig", "Release");
            Assert.That(_settings.GetSetting("ActiveConfig"), Is.EqualTo("Release"));
        }

        [Test]
        public void AddKnownStringSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _settings.Add("ActiveConfig", 5), Throws.ArgumentException);
        }

        [Test]
        public void AddKnownBoolSettingUsingName()
        {
            _settings.Add("RunAsX86", true);
            Assert.That(_settings.GetSetting("RunAsX86"), Is.True);
        }

        [Test]
        public void AddKnownBoolSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _settings.Add("RunAsX86", 5), Throws.ArgumentException);
        }

        [Test]
        public void AddKnownIntSettingUsingName()
        {
            _settings.Add("MaxAgents", 42);
            Assert.That(_settings.GetSetting("MaxAgents"), Is.EqualTo(42));
        }

        [Test]
        public void AddKnownintSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _settings.Add("MaxAgents", "Bad"), Throws.ArgumentException);
        }

        [Test]
        public void AddUnknownStringSettingUsingName()
        {
            _settings.Add("MySetting", "Something");
            Assert.That(_settings.GetSetting("MySetting"), Is.EqualTo("Something"));
        }

        [Test]
        public void AddUnknownBoolSettingUsingName()
        {
            _settings.Add("MySetting", true);
            Assert.That(_settings.GetSetting("MySetting"), Is.True);
        }

        [Test]
        public void AddUnknownIntSettingUsingName()
        {
            _settings.Add("MySetting", 999);
            Assert.That(_settings.GetSetting("MySetting"), Is.EqualTo(999));
        }

        [Test]
        public void CanRemoveSettingUsingSettingDefinition()
        {
            var setting = SettingDefinitions.MaxAgents.WithValue(20);
            _settings.Add(setting);
            Assert.That(_settings.HasSetting("MaxAgents"));
            _settings.Remove(SettingDefinitions.MaxAgents);
            Assert.That(_settings.HasSetting("MaxAgents"), Is.False);
        }

        [Test]
        public void CanRemoveSettingUsingName()
        {
            var setting = SettingDefinitions.MaxAgents.WithValue(20);
            _settings.Add(setting);
            Assert.That(_settings.HasSetting("MaxAgents"));
            _settings.Remove("MaxAgents");
            Assert.That(_settings.HasSetting("MaxAgents"), Is.False);
        }

        // Adding special types is NYI
        //[Test]
        //public void AddKnownSpecialSettingUsingName()
        //{
        //    var dict = new Dictionary<string, string>();
        //    _settings.Add("TestParametersDictionary", dict);
        //    Assert.That(_settings.GetSetting("TestParametersDictionary"), Is.SameAs(dict));
        //}
    }
}
