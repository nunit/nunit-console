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
        private TestPackage _package;
        private PackageSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _package = new TestPackage("test.dll");
            _settings = _package.Settings;
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
            _package.AddSetting(SettingDefinitions.MaxAgents.WithValue(42), false);
            Assert.That(_settings.GetSetting("MaxAgents"), Is.EqualTo(42));
        }

        [Test]
        public void AddKnownSettingUsingSettingDefinitionThrowsWithWrongType()
        {
            Assert.That(() => _package.AddSetting(SettingDefinitions.MaxAgents.WithValue("42"), false), Throws.ArgumentException);
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
            _package.AddSetting("ActiveConfig", "Release", false);
            Assert.That(_settings.GetSetting("ActiveConfig"), Is.EqualTo("Release"));
        }

        [Test, Ignore("TestPackage not distinguishing known from unknown settings")]
        public void AddKnownStringSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _package.AddSetting("ActiveConfig", 5, false), Throws.ArgumentException);
        }

        [Test]
        public void AddKnownBoolSettingUsingName()
        {
            _package.AddSetting("RunAsX86", true, false);
            Assert.That(_settings.GetSetting("RunAsX86"), Is.True);
        }

        [Test, Ignore("TestPackage not distinguishing known from unknown settings")]
        public void AddKnownBoolSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _package.AddSetting("RunAsX86", 5, false), Throws.ArgumentException);
        }

        [Test]
        public void AddKnownIntSettingUsingName()
        {
            _package.AddSetting("MaxAgents", 42, false);
            Assert.That(_settings.GetSetting("MaxAgents"), Is.EqualTo(42));
        }

        [Test, Ignore("TestPackage not distinguishing known from unknown settings")]
        public void AddKnownintSettingUsingNameThrowsWithWrongType()
        {
            Assert.That(() => _package.AddSetting("MaxAgents", "Bad", false), Throws.ArgumentException);
        }

        [Test]
        public void AddUnknownStringSettingUsingName()
        {
            _package.AddSetting("MySetting", "Something", false);
            Assert.That(_settings.GetSetting("MySetting"), Is.EqualTo("Something"));
        }

        [Test]
        public void AddUnknownBoolSettingUsingName()
        {
            _package.AddSetting("MySetting", true, false);
            Assert.That(_settings.GetSetting("MySetting"), Is.True);
        }

        [Test]
        public void AddUnknownIntSettingUsingName()
        {
            _package.AddSetting("MySetting", 999, false);
            Assert.That(_settings.GetSetting("MySetting"), Is.EqualTo(999));
        }

        [Test]
        public void CanRemoveSettingUsingSettingDefinition()
        {
            var setting = SettingDefinitions.MaxAgents.WithValue(20);
            _package.AddSetting(setting, false);
            Assert.That(_settings.HasSetting("MaxAgents"));
            _package.RemoveSetting(SettingDefinitions.MaxAgents, false);
            Assert.That(_settings.HasSetting("MaxAgents"), Is.False);
        }

        [Test]
        public void CanRemoveSettingUsingPackageSetting()
        {
            var setting = SettingDefinitions.MaxAgents.WithValue(20);
            _package.AddSetting(setting, false);
            Assert.That(_settings.HasSetting("MaxAgents"));
            _package.RemoveSetting(setting, false);
            Assert.That(_settings.HasSetting("MaxAgents"), Is.False);
        }

        [Test]
        public void CanRemoveSettingUsingName()
        {
            var setting = SettingDefinitions.MaxAgents.WithValue(20);
            _package.AddSetting(setting, false);
            Assert.That(_settings.HasSetting("MaxAgents"));
            _package.RemoveSetting("MaxAgents", false);
            Assert.That(_settings.HasSetting("MaxAgents"), Is.False);
        }

        // Adding special types is NYI
        //[Test]
        //public void AddKnownSpecialSettingUsingName()
        //{
        //    var dict = new Dictionary<string, string>();
        //    _package.AddSetting("TestParametersDictionary", dict, false);
        //    Assert.That(_settings.GetSetting("TestParametersDictionary"), Is.SameAs(dict));
        //}
    }
}
