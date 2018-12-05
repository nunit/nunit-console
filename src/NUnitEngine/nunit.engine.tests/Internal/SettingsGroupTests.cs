// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole, Rob Prouse
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

using System;
using System.ComponentModel;
using NUnit.Framework;
#if !NETCOREAPP1_1 && !NETCOREAPP2_0
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

#if !NETCOREAPP1_1 && !NETCOREAPP2_0
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
