// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace NUnit.Engine
{
    public static class DotNetHelperTests
    {
        [Test]
        public static void CanGetInstallDirectory([Values] bool x86)
        {
            string path = DotNet.GetInstallDirectory(x86);
            Assert.That(Directory.Exists(path));
            Assert.That(File.Exists(Path.Combine(path, OS.IsWindows ? "dotnet.exe" : "dotnet")));
        }

        [Test]
        public static void CanGetExecutable([Values] bool x86)
        {
            string path = DotNet.GetDotnetExecutable(x86);
            Assert.That(File.Exists(path));
            Assert.That(Path.GetFileName(path), Is.EqualTo(OS.IsWindows ? "dotnet.exe" : "dotnet"));
        }

        [Test]
        public static void CanIssueDotNetCommand([Values] bool x86)
        {
            var output = DotNet.DotnetCommand("--help", x86);
            Assert.That(output.Count(), Is.GreaterThan(0));
        }

        [TestCaseSource(nameof(RuntimeCases))]
        public static void CanParseInputLine(string line, string name, string packageVersion, string path,
            bool isPreRelease, Version version, string suffix)
        {
            DotNet.RuntimeInfo runtime = DotNet.RuntimeInfo.Parse(line);
            Assert.That(runtime.Name, Is.EqualTo(name));
            Assert.That(runtime.PackageVersion, Is.EqualTo(packageVersion));
            Assert.That(runtime.Path, Is.EqualTo(path));
            Assert.That(runtime.IsPreRelease, Is.EqualTo(isPreRelease));
            Assert.That(runtime.Version, Is.EqualTo(version));
            Assert.That(runtime.PreReleaseSuffix, Is.EqualTo(suffix));
        }

        static TestCaseData[] RuntimeCases = [
            new TestCaseData("Microsoft.NETCore.App 8.0.22 [C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App]",
                "Microsoft.NETCore.App", "8.0.22", "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App",
                false, new Version(8,0,22), null),
            new TestCaseData("Microsoft.WindowsDesktop.App 9.0.11 [C:\\Program Files\\dotnet\\shared\\Microsoft.WindowsDesktop.App]",
                "Microsoft.WindowsDesktop.App", "9.0.11", "C:\\Program Files\\dotnet\\shared\\Microsoft.WindowsDesktop.App",
                false, new Version(9,0,11), null),
            new TestCaseData("Microsoft.AspNetCore.App 7.0.20 [C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App]",
                "Microsoft.AspNetCore.App", "7.0.20", "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App",
                false, new Version(7,0,20), null),
            new TestCaseData("Microsoft.AspNetCore.App 9.0.0-rc.2.24474.3 [C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App]",
                "Microsoft.AspNetCore.App", "9.0.0-rc.2.24474.3", "C:\\Program Files\\dotnet\\shared\\Microsoft.AspNetCore.App",
                true, new Version(9,0,0), "rc.2.24474.3")];

        [TestCase("Microsoft.NETCore.App", "8.0.0", "8.0.22")]
        [TestCase("Microsoft.NETCore.App", "8.0.0.0", "8.0.22")]
        [TestCase("Microsoft.NETCore.App", "8.0.0.100", "8.0.22")]
        [TestCase("Microsoft.NETCore.App", "8.0.100", "9.0.11")]
        [TestCase("Microsoft.AspNetCore.App", "5.0.0", "8.0.22")] // Rather than 8.0.2
        [TestCase("Microsoft.AspNetCore.App", "7.0.0", "8.0.22")] // Rather than 8.0.2
        [TestCase("Microsoft.AspNetCore.App", "8.0.0", "8.0.22")] // Rather than 8.0.2
        [TestCase("Microsoft.WindowsDesktop.App", "9.0.0", "9.0.11")] // Rather than the pre-release version
        [TestCase("Microsoft.WindowsDesktop.App", "10.0.0", "10.0.0-rc.2.25502.107")]
        public static void FindBestRuntimeTests(string runtimeName, string targetVersion, string expectedVersion)
        {
            var availableRuntimes = SimulatedListRuntimesOutput.Where(r => r.Name == runtimeName);
            Assert.That(DotNet.FindBestRuntime(new Version(targetVersion), availableRuntimes, out DotNet.RuntimeInfo bestRuntime));
            Assert.That(bestRuntime, Is.Not.Null);
            Assert.That(bestRuntime.PackageVersion, Is.EqualTo(expectedVersion));
        }

        static DotNet.RuntimeInfo[] SimulatedListRuntimesOutput = [
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 8.0.2 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 8.0.22 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 9.0.0-rc.2.24474.3 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 9.0.11 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 10.0.0-rc.2.25502.107 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.AspNetCore.App 10.0.0 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.NETCore.App 8.0.22 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.NETCore.App 9.0.0-rc.2.24473.5 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.NETCore.App 9.0.11 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.NETCore.App 10.0.0-rc.2.25502.107 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.NETCore.App 10.0.0 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.WindowsDesktop.App 8.0.22 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.WindowsDesktop.App 9.0.0-rc.2.24474.4 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.WindowsDesktop.App 9.0.11 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]"),
            DotNet.RuntimeInfo.Parse(@"Microsoft.WindowsDesktop.App 10.0.0-rc.2.25502.107 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]") ];
            //DotNet.RuntimeInfo.Parse(@"Microsoft.WindowsDesktop.App 10.0.0 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]") ];
    }
}
