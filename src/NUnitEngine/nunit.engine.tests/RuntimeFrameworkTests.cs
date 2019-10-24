// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1 && !NETCOREAPP2_1
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    [TestFixture]
    public class RuntimeFrameworkTests
    {
        static RuntimeType currentRuntime =
            Type.GetType("Mono.Runtime", false) != null
                ? RuntimeType.Mono
                : RuntimeType.Net;

        [Test]
        public void CanGetCurrentFramework()
        {
            RuntimeFramework framework = RuntimeFramework.CurrentFramework;

            Assert.That(framework.Runtime, Is.EqualTo(currentRuntime));
            Assert.That(framework.ClrVersion, Is.EqualTo(Environment.Version));
        }

        [Test]
        public void CurrentFrameworkHasBuildSpecified()
        {
            Assert.That(RuntimeFramework.CurrentFramework.ClrVersion.Build, Is.GreaterThan(0));
        }

        [Test]
        public void CurrentFrameworkMustBeAvailable()
        {
            var current = RuntimeFramework.CurrentFramework;
            Console.WriteLine("Current framework is {0} ({1})", current.DisplayName, current.Id);
            Assert.That(current.IsAvailable, "{0} not available", current);
        }

        [Test]
        public void AvailableFrameworksList()
        {
            RuntimeFramework[] available = RuntimeFramework.AvailableFrameworks;
            Assert.That(RuntimeFramework.AvailableFrameworks.Length, Is.GreaterThan(0) );
            foreach (var framework in RuntimeFramework.AvailableFrameworks)
                Console.WriteLine("Available: {0}", framework.DisplayName);
        }

        [Test]
        public void AvailableFrameworksList_IncludesCurrentFramework()
        {
            foreach (var framework in RuntimeFramework.AvailableFrameworks)
                if (RuntimeFramework.CurrentFramework.Supports(framework))
                    return;

            Assert.Fail("CurrentFramework not listed as available");
        }

        [Test]
        public void AvailableFrameworksList_ContainsNoDuplicates()
        {
            var names = new List<string>();
            foreach (var framework in RuntimeFramework.AvailableFrameworks)
                names.Add(framework.DisplayName);
            Assert.That(names, Is.Unique);
        }

        [TestCaseSource(nameof(frameworkData))]
        public void CanCreateUsingFrameworkVersion(FrameworkData data)
        {
            RuntimeFramework framework = new RuntimeFramework(data.runtime, data.frameworkVersion);
            Assert.That(framework.Runtime, Is.EqualTo(data.runtime));
            Assert.That(framework.FrameworkVersion, Is.EqualTo(data.frameworkVersion));
            Assert.That(framework.ClrVersion, Is.EqualTo(data.clrVersion));
        }

        [TestCaseSource(nameof(frameworkData))]
        public void CanCreateUsingClrVersion(FrameworkData data)
        {
            Assume.That(data.frameworkVersion.Major != 3);

            RuntimeFramework framework = new RuntimeFramework(data.runtime, data.clrVersion);
            Assert.That(framework.Runtime, Is.EqualTo(data.runtime));
            Assert.That(framework.FrameworkVersion, Is.EqualTo(data.frameworkVersion));
            Assert.That(framework.ClrVersion, Is.EqualTo(data.clrVersion));
        }

        [TestCaseSource(nameof(frameworkData))]
        public void CanParseRuntimeFramework(FrameworkData data)
        {
            RuntimeFramework framework = RuntimeFramework.Parse(data.representation);
            Assert.That(framework.Runtime, Is.EqualTo(data.runtime));
            Assert.That(framework.ClrVersion, Is.EqualTo(data.clrVersion));
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
                new RuntimeFramework(RuntimeType.Net, new Version(3,5)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Net, new Version(3,5)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(3,5)),
                new RuntimeFramework(RuntimeType.Net, new Version(3,5)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0,50727)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0,50727)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0,50727)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Mono, new Version(2,0)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Net, new Version(1,1)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0,50727)),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0,40607)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Mono, new Version(1,1)), // non-existent version but it works
                new RuntimeFramework(RuntimeType.Mono, new Version(1,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Mono, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Mono, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Any, new Version(4,0)))
                .Returns(false),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, RuntimeFramework.DefaultVersion),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Net, RuntimeFramework.DefaultVersion))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Any, RuntimeFramework.DefaultVersion),
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Net, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Any, RuntimeFramework.DefaultVersion))
                .Returns(true)
            };

        private static readonly TestCaseData[] CanLoadData = {
            new TestCaseData(
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)),
                new RuntimeFramework(RuntimeType.Any, new Version(2,0)))
                .Returns(true),
            new TestCaseData(
                    new RuntimeFramework(RuntimeType.Any, new Version(2,0)),
                    new RuntimeFramework(RuntimeType.Any, new Version(4,0)))
                .Returns(false),
            new TestCaseData(
                    new RuntimeFramework(RuntimeType.Any, new Version(4,0)),
                    new RuntimeFramework(RuntimeType.Any, new Version(2,0)))
                .Returns(true)
            };
#pragma warning restore 414

        public struct FrameworkData
        {
            public RuntimeType runtime;
            public Version frameworkVersion;
            public Version clrVersion;
            public string representation;
            public string displayName;

            public FrameworkData(RuntimeType runtime, Version frameworkVersion, Version clrVersion,
                string representation, string displayName)
            {
                this.runtime = runtime;
                this.frameworkVersion = frameworkVersion;
                this.clrVersion = clrVersion;
                this.representation = representation;
                this.displayName = displayName;
            }

            public override string ToString()
            {
                return string.Format("<{0}-{1}>", this.runtime, this.frameworkVersion);
            }
        }

#pragma warning disable 414
        static FrameworkData[] frameworkData = new FrameworkData[] {
            new FrameworkData(RuntimeType.Net, new Version(1,0), new Version(1,0,3705), "net-1.0", ".NET 1.0"),
            //new FrameworkData(RuntimeType.Net, new Version(1,0,3705), new Version(1,0,3705), "net-1.0.3705", "Net 1.0.3705"),
            //new FrameworkData(RuntimeType.Net, new Version(1,0), new Version(1,0,3705), "net-1.0.3705", "Net 1.0.3705"),
            new FrameworkData(RuntimeType.Net, new Version(1,1), new Version(1,1,4322), "net-1.1", ".NET 1.1"),
            //new FrameworkData(RuntimeType.Net, new Version(1,1,4322), new Version(1,1,4322), "net-1.1.4322", "Net 1.1.4322"),
            new FrameworkData(RuntimeType.Net, new Version(2,0), new Version(2,0,50727), "net-2.0", ".NET 2.0"),
            //new FrameworkData(RuntimeType.Net, new Version(2,0,40607), new Version(2,0,40607), "net-2.0.40607", "Net 2.0.40607"),
            //new FrameworkData(RuntimeType.Net, new Version(2,0,50727), new Version(2,0,50727), "net-2.0.50727", "Net 2.0.50727"),
            new FrameworkData(RuntimeType.Net, new Version(3,0), new Version(2,0,50727), "net-3.0", ".NET 3.0"),
            new FrameworkData(RuntimeType.Net, new Version(3,5), new Version(2,0,50727), "net-3.5", ".NET 3.5"),
            new FrameworkData(RuntimeType.Net, new Version(4,0), new Version(4,0,30319), "net-4.0", ".NET 4.0"),
            new FrameworkData(RuntimeType.Net, RuntimeFramework.DefaultVersion, RuntimeFramework.DefaultVersion, "net", ".NET"),
            new FrameworkData(RuntimeType.Mono, new Version(1,0), new Version(1,1,4322), "mono-1.0", "Mono 1.0"),
            new FrameworkData(RuntimeType.Mono, new Version(2,0), new Version(2,0,50727), "mono-2.0", "Mono 2.0"),
            //new FrameworkData(RuntimeType.Mono, new Version(2,0,50727), new Version(2,0,50727), "mono-2.0.50727", "Mono 2.0.50727"),
            new FrameworkData(RuntimeType.Mono, new Version(3,5), new Version(2,0,50727), "mono-3.5", "Mono 3.5"),
            new FrameworkData(RuntimeType.Mono, new Version(4,0), new Version(4,0,30319), "mono-4.0", "Mono 4.0"),
            new FrameworkData(RuntimeType.Mono, RuntimeFramework.DefaultVersion, RuntimeFramework.DefaultVersion, "mono", "Mono"),
            new FrameworkData(RuntimeType.Any, new Version(1,1), new Version(1,1,4322), "v1.1", "v1.1"),
            new FrameworkData(RuntimeType.Any, new Version(2,0), new Version(2,0,50727), "v2.0", "v2.0"),
            //new FrameworkData(RuntimeType.Any, new Version(2,0,50727), new Version(2,0,50727), "v2.0.50727", "v2.0.50727"),
            new FrameworkData(RuntimeType.Any, new Version(3,5), new Version(2,0,50727), "v3.5", "v3.5"),
            new FrameworkData(RuntimeType.Any, new Version(4,0), new Version(4,0,30319), "v4.0", "v4.0"),
            new FrameworkData(RuntimeType.Any, RuntimeFramework.DefaultVersion, RuntimeFramework.DefaultVersion, "any", "Any")
        };
#pragma warning restore 414
    }
}
#endif