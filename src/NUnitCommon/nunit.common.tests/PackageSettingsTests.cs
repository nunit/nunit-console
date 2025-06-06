// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Common
{
    public class PackageSettingsTests
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        [Test]
        public void AllFrameworkSettingsHaveDefinitions()
        {
            Assert.That(
                typeof(SettingDefinitions).GetProperties(PublicStatic).Select(p => p.Name),
                Is.SupersetOf(typeof(FrameworkPackageSettings).GetProperties(PublicStatic).Select(p => p.Name)));
        }
    }
}
