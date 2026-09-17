// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Common;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Runtime.Versioning;

namespace NUnit.Engine
{
    [TestFixture]
    public class RuntimeFrameworkTests
    {
        private const string NETFX = FrameworkIdentifiers.NetFramework;
        private const string NETCORE = FrameworkIdentifiers.NetCoreApp;

        [TestCaseSource(nameof(frameworkData))]
        public void ConstructFromFrameworkName(FrameworkData data)
        {
            RuntimeFramework framework = new RuntimeFramework(data._frameworkName);
            CheckFrameworkValues(framework, data);
        }

        [TestCaseSource(nameof(frameworkData))]
        public void FromTFM(FrameworkData data)
        {
            RuntimeFramework framework = RuntimeFramework.FromTFM(data._tfm);
            CheckFrameworkValues(framework, data);
        }

        [TestCaseSource(nameof(matchData))]
        public bool CanMatchRuntimes(RuntimeFramework f1, RuntimeFramework f2)
        {
            return f1.Supports(f2);
        }

        [TestCaseSource(nameof(CanLoadData))]
        public bool CanLoad(RuntimeFramework f1, RuntimeFramework f2)
        {
            return f1.CanLoad(f2);
        }

        private static void CheckFrameworkValues(RuntimeFramework framework, FrameworkData data)
        {
            Assert.That(framework.FrameworkVersion, Is.EqualTo(data._version));
            Assert.That(framework.FrameworkName.ToString(), Is.EqualTo(data._frameworkName));
            Assert.That(framework.TFM, Is.EqualTo(data._tfm));
            Assert.That(framework.DisplayName, Is.EqualTo(data._displayName));
        }

#pragma warning disable 414
        private static TestCaseData[] matchData = new TestCaseData[]
        {
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(3, 5)),
                new RuntimeFramework(NETFX, new Version(2, 0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(2, 0)),
                new RuntimeFramework(NETFX, new Version(3, 5)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(3, 5)),
                new RuntimeFramework(NETFX, new Version(3, 5)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(2, 0)),
                new RuntimeFramework(NETFX, new Version(2, 0)))
                .Returns(true),
            //new TestCaseData(
            //    new RuntimeFramework(NETFX, new Version(2, 0)),
            //    new RuntimeFramework(Runtime.Mono, new Version(2, 0)))
            //    .Returns(true),
            //new TestCaseData(
            //    new RuntimeFramework(Runtime.Mono, new Version(2, 0)),
            //    new RuntimeFramework(NETFX, new Version(2, 0)))
            //    .Returns(true),
            //new TestCaseData(
            //    new RuntimeFramework(NETFX, new Version(4, 0)),
            //    new RuntimeFramework(Runtime.Mono, new Version(4, 0)))
            //    .Returns(true),
            //new TestCaseData(
            //    new RuntimeFramework(Runtime.Mono, new Version(4, 0)),
            //    new RuntimeFramework(NETFX, new Version(4, 0)))
            //    .Returns(true),
            //new TestCaseData(
            //    new RuntimeFramework(Runtime.Mono, new Version(4, 0)),
            //    new RuntimeFramework(NETFX, new Version(4, 6, 2)))
            //    .Returns(true),
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(2, 0)),
                new RuntimeFramework(NETFX, new Version(1, 1)))
                .Returns(false),
            //new TestCaseData(
            //    new RuntimeFramework(Runtime.Mono, new Version(1, 1)), // non-existent version but it works
            //    new RuntimeFramework(Runtime.Mono, new Version(1, 0)))
            //    .Returns(true),
        };

        private static readonly TestCaseData[] CanLoadData =
        {
            new TestCaseData(
                new RuntimeFramework(NETFX, new Version(2, 0)),
                new RuntimeFramework(NETFX, new Version(2, 0)))
                .Returns(true),
            new TestCaseData(
                    new RuntimeFramework(NETFX, new Version(2, 0)),
                    new RuntimeFramework(NETFX, new Version(4, 0)))
                .Returns(false),
            new TestCaseData(
                    new RuntimeFramework(NETFX, new Version(4, 0)),
                    new RuntimeFramework(NETFX, new Version(2, 0)))
                .Returns(true)
        };
#pragma warning restore 414

        public struct FrameworkData
        {
            public Runtime _runtime;
            public Version _version;
            public string _id;
            public string _frameworkName;
            public string _tfm;
            public string _displayName;

            public FrameworkData(Runtime runtime, Version version, string id, string frameworkName, string tfm,
                string displayName)
            {
                _runtime = runtime;
                _version = version;
                _id = id;
                _frameworkName = frameworkName;
                _tfm = tfm;
                _displayName = displayName;
            }

            public override string ToString()
            {
                return string.Format("<{0}-{1}>", _runtime, _version);
            }
        }

        private static readonly FrameworkData[] frameworkData =
        [
            new FrameworkData(Runtime.Net, new Version(1, 0), "net-1.0", ".NETFramework,Version=v1.0", "net10", ".NET Framework 1.0"),
            new FrameworkData(Runtime.Net, new Version(1, 1), "net-1.1", ".NETFramework,Version=v1.1", "net11", ".NET Framework 1.1"),
            new FrameworkData(Runtime.Net, new Version(2, 0), "net-2.0", ".NETFramework,Version=v2.0", "net20", ".NET Framework 2.0"),
            new FrameworkData(Runtime.Net, new Version(3, 0), "net-3.0", ".NETFramework,Version=v3.0", "net30", ".NET Framework 3.0"),
            new FrameworkData(Runtime.Net, new Version(3, 5), "net-3.5", ".NETFramework,Version=v3.5", "net35", ".NET Framework 3.5"),
            new FrameworkData(Runtime.Net, new Version(4, 0), "net-4.0", ".NETFramework,Version=v4.0", "net40", ".NET Framework 4.0"),
            new FrameworkData(Runtime.Net, new Version(4, 5), "net-4.5", ".NETFramework,Version=v4.5", "net45", ".NET Framework 4.5"),
            new FrameworkData(Runtime.Net, new Version(4, 5, 1), "net-4.5.1", ".NETFramework,Version=v4.5.1", "net451", ".NET Framework 4.5.1"),
            new FrameworkData(Runtime.Net, new Version(4, 5, 2), "net-4.5.2", ".NETFramework,Version=v4.5.2", "net452", ".NET Framework 4.5.2"),
            new FrameworkData(Runtime.Net, new Version(4, 6), "net-4.6", ".NETFramework,Version=v4.6", "net46", ".NET Framework 4.6"),
            new FrameworkData(Runtime.Net, new Version(4, 6, 1), "net-4.6.1", ".NETFramework,Version=v4.6.1", "net461", ".NET Framework 4.6.1"),
            new FrameworkData(Runtime.Net, new Version(4, 6, 2), "net-4.6.2", ".NETFramework,Version=v4.6.2", "net462", ".NET Framework 4.6.2"),
            new FrameworkData(Runtime.Net, new Version(4, 7), "net-4.7", ".NETFramework,Version=v4.7", "net47", ".NET Framework 4.7"),
            new FrameworkData(Runtime.Net, new Version(4, 7, 1), "net-4.7.1", ".NETFramework,Version=v4.7.1", "net471", ".NET Framework 4.7.1"),
            new FrameworkData(Runtime.Net, new Version(4, 7, 2), "net-4.7.2", ".NETFramework,Version=v4.7.2", "net472", ".NET Framework 4.7.2"),
            new FrameworkData(Runtime.Net, new Version(4, 8), "net-4.8", ".NETFramework,Version=v4.8", "net48", ".NET Framework 4.8"),
            new FrameworkData(Runtime.Net, new Version(4, 8, 1), "net-4.8.1", ".NETFramework,Version=v4.8.1", "net481", ".NET Framework 4.8.1"),
            new FrameworkData(Runtime.NetCore, new Version(2, 1), "netcore-2.1", ".NETCoreApp,Version=v2.1", "netcoreapp2.1", ".NETCore 2.1"),
            new FrameworkData(Runtime.NetCore, new Version(3, 1), "netcore-3.1", ".NETCoreApp,Version=v3.1", "netcoreapp3.1", ".NETCore 3.1"),
            new FrameworkData(Runtime.NetCore, new Version(5, 0), "netcore-5.0", ".NETCoreApp,Version=v5.0", "net5.0", ".NETCore 5.0"),
            new FrameworkData(Runtime.NetCore, new Version(6, 0), "netcore-6.0", ".NETCoreApp,Version=v6.0", "net6.0", ".NETCore 6.0"),
            new FrameworkData(Runtime.NetCore, new Version(7, 0), "netcore-7.0", ".NETCoreApp,Version=v7.0", "net7.0", ".NETCore 7.0"),
            new FrameworkData(Runtime.NetCore, new Version(8, 0), "netcore-8.0", ".NETCoreApp,Version=v8.0", "net8.0", ".NETCore 8.0"),
            new FrameworkData(Runtime.NetCore, new Version(9, 0), "netcore-9.0", ".NETCoreApp,Version=v9.0", "net9.0", ".NETCore 9.0"),
            new FrameworkData(Runtime.NetCore, new Version(10, 0), "netcore-10.0", ".NETCoreApp,Version=v10.0", "net10.0", ".NETCore 10.0"),
        ];
    }
}
