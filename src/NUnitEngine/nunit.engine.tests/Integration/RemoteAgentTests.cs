// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System.Diagnostics;
using NUnit.Engine.Tests.Helpers;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Integration
{
    public sealed class RemoteAgentTests : IntegrationTests
    {
        [Test, Ignore("No longer works with multiple agents")]
        public void Explore_does_not_throw_SocketException()
        {
            using (var runner = new RunnerInDirectoryWithoutFramework())
            using (var test = new MockAssemblyInDirectoryWithFramework())
            {
                for (var times = 0; times < 3; times++)
                {
                    var result = ProcessUtils.Run(new ProcessStartInfo(runner.ConsoleExe, $"--explore \"{test.MockAssemblyDll}\""));
                    Assert.That(result.StandardStreamData, Has.None.With.Property("Data").Contains("System.Net.Sockets.SocketException"));
                    Assert.That(result.StandardStreamData, Has.None.With.Property("IsError").True);
                    Assert.That(result, Has.Property("ExitCode").Zero);
                }
            }
        }
    }
}
#endif