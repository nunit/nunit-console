// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Linq;
using System.Reflection;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Common
{
    public class PackageSettingsTests
    {
        private static readonly BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
        private static readonly string[] CurrentFrameworkSettingNames =
            typeof(NUnit.FrameworkPackageSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();
        private static readonly string[] SupportedFrameworkSettingNames =
            typeof(FrameworkPackageSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();
        private static readonly string[] SettingDefinitions =
            typeof(PackageSettings).GetProperties(PublicStatic).Select(p => p.Name).ToArray();

        [Test]
        public void CurrentFrameworkSettingsAreAllSupported()
        {
            Assert.That(SupportedFrameworkSettingNames, Is.SupersetOf(CurrentFrameworkSettingNames));
        }

        [Test]
        public void AllFrameworkSettingsHaveDefinitions()
        {
            // Currently equivalent, but may change if we combine SettingDefinitions into one class.
            Assert.That(SettingDefinitions, Is.SupersetOf(SupportedFrameworkSettingNames));
        }
    }
}
