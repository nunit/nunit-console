// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Runners
{
    public static class DomainManagerStaticTests
    {
        private static string path1 = TestPath("/test/bin/debug/test1.dll");
        private static string path2 = TestPath("/test/bin/debug/test2.dll");
        private static string path3 = TestPath("/test/utils/test3.dll");

        private const string STANDARD_CONFIG_FILE = "nunit.agent.core.tests.exe.config";
        private const string ALTERNATE_CONFIG_FILE = "alt.config";

        [Test]
        public static void GetPrivateBinPath()
        {
            string[] assemblies = new string[] { path1, path2, path3 };

            Assert.That(DomainManager.GetPrivateBinPath(TestPath("/test"), assemblies), Is.EqualTo(TestPath("bin/debug") + Path.PathSeparator + TestPath("utils")));
        }

        [Test]
        public static void GetCommonAppBase_OneElement()
        {
            string[] assemblies = new string[] { path1 };

            Assert.That(DomainManager.GetCommonAppBase(assemblies), Is.EqualTo(TestPath("/test/bin/debug")));
        }

        [Test]
        public static void GetCommonAppBase_TwoElements_SameDirectory()
        {
            string[] assemblies = new string[] { path1, path2 };

            Assert.That(DomainManager.GetCommonAppBase(assemblies), Is.EqualTo(TestPath("/test/bin/debug")));
        }

        [Test]
        public static void GetCommonAppBase_TwoElements_DifferentDirectories()
        {
            string[] assemblies = new string[] { path1, path3 };

            Assert.That(DomainManager.GetCommonAppBase(assemblies), Is.EqualTo(TestPath("/test")));
        }

        [Test]
        public static void GetCommonAppBase_ThreeElements_DifferentDirectories()
        {
            string[] assemblies = new string[] { path1, path2, path3 };

            Assert.That(DomainManager.GetCommonAppBase(assemblies), Is.EqualTo(TestPath("/test")));
        }

        [Test]
        public static void ProperConfigFileIsUsed()
        {
            // NOTE: The alternate config file, alt.config, is copied to the bin directory and
            // may be specified from the command-line, using --configfile=alt.config. This allows
            // manual testing of the option while permitting this test to still pass.
            var expectedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, STANDARD_CONFIG_FILE);
            var alternatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, ALTERNATE_CONFIG_FILE);
            Assert.That(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, Is.SamePath(expectedPath).Or.SamePath(alternatePath));
        }

        [Test]
        public static void CanReadConfigFile()
        {
            // NOTE: The alternate config file has a different value so we can see it being used
            var expectedSetting = Path.GetFileName(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile) == ALTERNATE_CONFIG_FILE
                ? "Alternate config used"
                : "54321";
            Assert.That(ConfigurationManager.AppSettings.Get("test.setting"), Is.EqualTo(expectedSetting));
        }

        [TestCase("/path/to/mytest.dll", null, "/path/to/")]
        [TestCase("/path/to/mytest.dll", "/path", "/path/")]
        public static void ApplicationBaseTests(string filePath, string? appBase, string expected)
        {
            filePath = TestPath(filePath);
            appBase = TestPath(appBase);
            expected = TestPath(expected);

            var package = new TestPackage(filePath);
            if (appBase is not null)
                package.Settings["BasePath"] = appBase;

            Assert.That(DomainManager.GetApplicationBase(package), Is.SamePath(expected));
        }

        [TestCase("/path/to/mytest.dll", "/path/to", null)]
        [TestCase("/path/to/mytest.dll", "/path", "to")]
        public static void PrivateBinPathTests(string filePath, string appBase, string? expected)
        {
            filePath = TestPath(filePath);
            appBase = TestPath(appBase);
            expected = TestPath(expected);

            var package = new TestPackage(filePath);

            Assert.That(DomainManager.GetPrivateBinPath(appBase, package), Is.EqualTo(expected));
        }

        [TestCase("/path/to/mytest.dll", "/path/to", null, "/path/to/mytest.dll.config")]
        [TestCase("/path/to/mytest.dll", "/path", null, "/path/to/mytest.dll.config")]
        [TestCase("/path/to/mytest.nunit", "/path/to", null, null)]
        [TestCase("/path/to/mytest.nunit", "/path/to", "/path/to/mytest.config", "/path/to/mytest.config")]
        public static void ConfigFileTests(string filePath, string appBase, string? configSetting, string? expected)
        {
            filePath = TestPath(filePath);
            appBase = TestPath(appBase);
            configSetting = TestPath(configSetting);
            expected = TestPath(expected);

            var package = new TestPackage(filePath);
            if (configSetting is not null)
                package.Settings["ConfigurationFile"] = configSetting;

            Assert.That(DomainManager.GetConfigFile(appBase, package), Is.EqualTo(expected));
        }

        /// <summary>
        /// Take a valid Linux filePath and make a valid windows filePath out of it
        /// if we are on Windows. Change slashes to backslashes and, if the
        /// filePath starts with a slash, add C: in front of it.
        /// </summary>
        [return: NotNullIfNotNull(nameof(path))]
        private static string? TestPath(string? path)
        {
            if (path is not null && Path.DirectorySeparatorChar != '/')
            {
                path = path.Replace('/', Path.DirectorySeparatorChar);
                if (path[0] == Path.DirectorySeparatorChar)
                    path = "C:" + path;
            }

            return path;
        }

        private static IEnumerable<TestCaseData> AppDomainData()
        {
            yield return new TestCaseData(new TestPackage(@"C:\path\to\mytest.dll"), @"C:\path\to");
        }
    }
}
#endif