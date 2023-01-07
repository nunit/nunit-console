// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.Engine
{
    [TestFixture]
    public class RuntimeFrameworkTests
    {
        [TestCaseSource(nameof(frameworkData))]
        public void CanCreateUsingFrameworkVersion(FrameworkData data)
        {
            RuntimeFramework framework = new RuntimeFramework(data.runtime, data.frameworkVersion);
            Assert.That(framework.Runtime, Is.EqualTo(data.runtime));
            Assert.That(framework.FrameworkVersion, Is.EqualTo(data.frameworkVersion));
        }

        [TestCaseSource(nameof(frameworkData))]
        public void CanParseRuntimeFramework(FrameworkData data)
        {
            RuntimeFramework framework = RuntimeFramework.Parse(data.representation);
            Assert.That(framework.Runtime, Is.EqualTo(data.runtime));
        }

        [TestCaseSource(nameof(frameworkData))]
        public void CanDisplayFrameworkAsString(FrameworkData data)
        {
            RuntimeFramework framework = new RuntimeFramework(data.runtime, data.frameworkVersion);
            Assert.That(framework.ToString(), Is.EqualTo(data.representation));
            Assert.That(framework.DisplayName, Is.EqualTo(data.displayName));
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

#pragma warning disable 414
        static TestCaseData[] matchData = new TestCaseData[] {
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(3,5)),
                new RuntimeFramework(Runtime.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(2,0)),
                new RuntimeFramework(Runtime.Net, new Version(3,5)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(3,5)),
                new RuntimeFramework(Runtime.Net, new Version(3,5)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(2,0)),
                new RuntimeFramework(Runtime.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(2,0)),
                new RuntimeFramework(Runtime.Mono, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Mono, new Version(2,0)),
                new RuntimeFramework(Runtime.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(4,0)),
                new RuntimeFramework(Runtime.Mono, new Version(4,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Mono, new Version(4,0)),
                new RuntimeFramework(Runtime.Net, new Version(4,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Mono, new Version(4,0)),
                new RuntimeFramework(Runtime.Net, new Version(4,6,2)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(2,0)),
                new RuntimeFramework(Runtime.Net, new Version(1,1)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(Runtime.Mono, new Version(1,1)), // non-existent version but it works
                new RuntimeFramework(Runtime.Mono, new Version(1,0)))
                .Returns(true),
            };

        private static readonly TestCaseData[] CanLoadData = {
            new TestCaseData(
                new RuntimeFramework(Runtime.Net, new Version(2,0)),
                new RuntimeFramework(Runtime.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                    new RuntimeFramework(Runtime.Net, new Version(2,0)),
                    new RuntimeFramework(Runtime.Net, new Version(4,0)))
                .Returns(false),
            new TestCaseData(
                    new RuntimeFramework(Runtime.Net, new Version(4,0)),
                    new RuntimeFramework(Runtime.Net, new Version(2,0)))
                .Returns(true)
            };
#pragma warning restore 414

        public struct FrameworkData
        {
            public Runtime runtime;
            public Version frameworkVersion;
            public string representation;
            public string displayName;

            public FrameworkData(Runtime runtime, Version frameworkVersion, string representation,
                string displayName)
            {
                this.runtime = runtime;
                this.frameworkVersion = frameworkVersion;
                this.representation = representation;
                this.displayName = displayName;
            }

            public override string ToString()
            {
                return string.Format("<{0}-{1}>", this.runtime, this.frameworkVersion);
            }
        }

        static readonly FrameworkData[] frameworkData = new FrameworkData[] {
            new FrameworkData(Runtime.Net, new Version(1,0), "net-1.0", ".NET 1.0"),
            new FrameworkData(Runtime.Net, new Version(1,1), "net-1.1", ".NET 1.1"),
            new FrameworkData(Runtime.Net, new Version(2,0), "net-2.0", ".NET 2.0"),
            new FrameworkData(Runtime.Net, new Version(3,0), "net-3.0", ".NET 3.0"),
            new FrameworkData(Runtime.Net, new Version(3,5), "net-3.5", ".NET 3.5"),
            new FrameworkData(Runtime.Net, new Version(4,0), "net-4.0", ".NET 4.0"),
            new FrameworkData(Runtime.Net, new Version(4,5), "net-4.5", ".NET 4.5"),
            new FrameworkData(Runtime.Net, new Version(4,5,1), "net-4.5.1", ".NET 4.5.1"),
            new FrameworkData(Runtime.Net, new Version(4,5,2), "net-4.5.2", ".NET 4.5.2"),
            new FrameworkData(Runtime.Net, new Version(4,6), "net-4.6", ".NET 4.6"),
            new FrameworkData(Runtime.Net, new Version(4,6,1), "net-4.6.1", ".NET 4.6.1"),
            new FrameworkData(Runtime.Net, new Version(4,6,2), "net-4.6.2", ".NET 4.6.2"),
            new FrameworkData(Runtime.Net, new Version(4,7), "net-4.7", ".NET 4.7"),
            new FrameworkData(Runtime.Net, new Version(4,7,1), "net-4.7.1", ".NET 4.7.1"),
            new FrameworkData(Runtime.Net, new Version(4,7,2), "net-4.7.2", ".NET 4.7.2"),
            new FrameworkData(Runtime.Net, new Version(4,8), "net-4.8", ".NET 4.8"),
            new FrameworkData(Runtime.Mono, new Version(1,0), "mono-1.0", "Mono 1.0"),
            new FrameworkData(Runtime.Mono, new Version(2,0), "mono-2.0", "Mono 2.0"),
            new FrameworkData(Runtime.Mono, new Version(3,5), "mono-3.5", "Mono 3.5"),
            new FrameworkData(Runtime.Mono, new Version(4,0), "mono-4.0", "Mono 4.0"),
            new FrameworkData(Runtime.NetCore, new Version(2,1), "netcore-2.1", ".NETCore 2.1"),
            new FrameworkData(Runtime.NetCore, new Version(3,1), "netcore-3.1", ".NETCore 3.1"),
            new FrameworkData(Runtime.NetCore, new Version(5,0), "netcore-5.0", ".NETCore 5.0"),
            new FrameworkData(Runtime.NetCore, new Version(6,0), "netcore-6.0", ".NETCore 6.0"),
            new FrameworkData(Runtime.NetCore, new Version(7,0), "netcore-7.0", ".NETCore 7.0"),
        };
    }
}
#endif