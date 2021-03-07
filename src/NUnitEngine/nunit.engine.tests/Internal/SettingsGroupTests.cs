// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.ComponentModel;
using NUnit.Framework;
#if NETFRAMEWORK
using System.Drawing;
#endif

namespace NUnit.Engine.Internal.Tests
{
    [TestFixture]
    public class SettingsGroupTests
    {
        private SettingsGroup settings;

        [SetUp]
        public void BeforeEachTest()
        {
            settings = new SettingsGroup();
        }

        [Test]
        public void WhenSettingIsNotInitialized_NullIsReturned()
        {
            Assert.IsNull(settings.GetSetting("X"));
            Assert.IsNull(settings.GetSetting("NAME"));
        }

        [TestCase("X", 5)]
        [TestCase("Y", 2.5)]
        [TestCase("NAME", "Charlie")]
        [TestCase("Flag", true)]
        [TestCase("Priority", PriorityValue.A)]
        public void WhenSettingIsInitialized_ValueIsReturned(string name, object expected)
        {
            settings.SaveSetting(name, expected);
            object actual = settings.GetSetting(name);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.IsInstanceOf(expected.GetType(),actual);
        }

        private enum PriorityValue
        {
            A,
            B,
            C
        };

        [Test]
        public void WhenSettingIsRemoved_NullIsReturnedAndOtherSettingsAreNotAffected()
        {
            settings.SaveSetting("X", 5);
            settings.SaveSetting("NAME", "Charlie");

            settings.RemoveSetting("X");
            Assert.IsNull(settings.GetSetting("X"), "X not removed");
            Assert.That(settings.GetSetting("NAME"), Is.EqualTo("Charlie"));

            settings.RemoveSetting("NAME");
            Assert.IsNull(settings.GetSetting("NAME"), "NAME not removed");
        }

        [Test]
        public void WhenSettingIsNotInitialized_DefaultValueIsReturned()
        {

            Assert.That(settings.GetSetting( "X", 5 ), Is.EqualTo(5));
            Assert.That(settings.GetSetting( "X", 6 ), Is.EqualTo(6));
            Assert.That(settings.GetSetting( "X", "7" ), Is.EqualTo("7"));

            Assert.That(settings.GetSetting( "NAME", "Charlie" ), Is.EqualTo("Charlie"));
            Assert.That(settings.GetSetting( "NAME", "Fred" ), Is.EqualTo("Fred"));
        }

        [Test]
        public void WhenSettingIsNotValid_DefaultSettingIsReturned()
        {
            settings.SaveSetting( "X", "1y25" );
            Assert.That(settings.GetSetting( "X", 42 ), Is.EqualTo(42));
        }

#if NETFRAMEWORK
        [Test]
        [SetCulture("da-DK")]
        public void SaveAndGetSettingShouldReturnTheOriginalValue()
        {
            var settingName = "MySetting";
            var settingValue = new Point(10, 20);
            var typeConverter = TypeDescriptor.GetConverter(settingValue);
            var settingsValue = typeConverter.ConvertToInvariantString(settingValue);

            settings.SaveSetting(settingName, settingsValue);
            var point = settings.GetSetting(settingName, new Point(30, 40));
            Assert.That(point, Is.EqualTo(settingValue));
        }
#endif
    }
}
