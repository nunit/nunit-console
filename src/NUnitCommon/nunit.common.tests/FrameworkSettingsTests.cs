// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Linq;
using System.Reflection;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Common
{
    public class FrameworkSettingsTests
    {
        private static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private static readonly string[] FrameworkSettingNames =
            typeof(NUnit.FrameworkPackageSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();
        private static readonly string[] SupportedSettingNames =
            typeof(NUnit.Common.FrameworkPackageSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();
        private static readonly string[] SettingDefinitions =
            typeof(NUnit.Common.FrameworkSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();

        [Test]
        public void AllFrameworkSettingsAreSupported()
        {
            Assert.That(SupportedSettingNames, Is.SupersetOf(FrameworkSettingNames));
        }

        [Test]
        public void AllSupportedSettingsHaveDefinitions()
        {
            // Currently equivalent, but may change if we combine SettingDefinitions into one class.
            Assert.That(SettingDefinitions, Is.SupersetOf(SupportedSettingNames));
        }
    }
}
