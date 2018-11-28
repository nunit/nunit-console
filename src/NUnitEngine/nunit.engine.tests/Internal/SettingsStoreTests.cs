// ***********************************************************************
// Copyright (c) 2017 Charlie Poole, Rob Prouse
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
using System.IO;
using System.Xml.Schema;
using NUnit.Framework;

namespace NUnit.Engine.Internal.Tests
{
    [TestFixture]
    public class SettingsStoreTests
    {
        private string _settingsFile;
        private SettingsStore _settings;

        [SetUp]
        public void SetUp()
        {
            _settingsFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _settings = new SettingsStore(_settingsFile, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_settingsFile))
                File.Delete(_settingsFile);
        }

        [Test]
        public void SaveSettingsThrowsExceptionOnErrorInSetting()
        {
            var wrongValue = -1;
            XmlTypeCode settingValue = (XmlTypeCode)wrongValue;
            _settings.SaveSetting("setting", settingValue);
            Assert.Throws<ArgumentException>(() => _settings.SaveSettings());
            Assert.That(_settingsFile, Does.Not.Exist);
        }

        [Test]
        public void SaveSettingsCanBeSavedAndLoaded()
        {
            const string xmlTypeCodeSettingName = "setting";
            const string dateSettingName = "date";
            var xmlTypeCodeSettingValue = XmlTypeCode.ProcessingInstruction;
            var dateSettingValue = new DateTime(2017, 05, 28);

            _settings.SaveSetting(xmlTypeCodeSettingName, xmlTypeCodeSettingValue);
            _settings.SaveSetting(dateSettingName, dateSettingValue);
            _settings.SaveSettings();
            _settings.LoadSettings();

            var actualXmlTypeCodeValue = _settings.GetSetting(xmlTypeCodeSettingName, XmlTypeCode.Comment);
            var actualDateValue = _settings.GetSetting(dateSettingName, DateTime.MinValue);
            Assert.That(actualXmlTypeCodeValue, Is.EqualTo(xmlTypeCodeSettingValue));
            Assert.That(actualDateValue, Is.EqualTo(dateSettingValue));
        }

        [Test]
        public void SaveSettingsDoesNotOverwriteExistingFileWhenFailing()
        {
            const string xmlTypeCodeSettingName = "setting";
            const string dateSettingName = "date";
            var xmlTypeCodeSettingValue = XmlTypeCode.ProcessingInstruction;
            var dateSettingValue = new DateTime(2017, 03, 19);

            // Save initial version of the settings file
            _settings.SaveSetting(xmlTypeCodeSettingName, xmlTypeCodeSettingValue);
            _settings.SaveSetting(dateSettingName, dateSettingValue);
            _settings.SaveSettings();

            // Try to save setting that fails
            _settings.LoadSettings();
            var wrongValue = -1;
            var settingValue = (XmlTypeCode)wrongValue;
            _settings.SaveSetting("setting", settingValue);
            Assert.Throws<ArgumentException>(() => _settings.SaveSettings());

            // Assert that the initial version is not overwritten
            _settings.LoadSettings();
            var actualXmlTypeCodeValue = _settings.GetSetting(xmlTypeCodeSettingName, XmlTypeCode.Comment);
            var actualDateValue = _settings.GetSetting(dateSettingName, DateTime.MinValue);
            Assert.That(actualXmlTypeCodeValue, Is.EqualTo(xmlTypeCodeSettingValue));
            Assert.That(actualDateValue, Is.EqualTo(dateSettingValue));
        }
    }
}
#endif