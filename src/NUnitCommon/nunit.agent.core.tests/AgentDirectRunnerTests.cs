// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Common;
using NUnit.Framework;
using NUnit.TestData.Assemblies;

namespace NUnit.Agents
{
    public class AgentDirectRunnerTests
    {
        [Test]
        public void RunAgentDirectly()
        {
            RunTestUnderTestBed(typeof(MockAssembly).Assembly.Location);
        }

        private static void RunTestUnderTestBed(string testAssembly)
        {
            string agentAssembly = typeof(DirectTestAgent).Assembly.Location;

#if NETFRAMEWORK
            string agentExe = agentAssembly;
#else
            string agentExe = Path.ChangeExtension(agentAssembly, ".exe");
#endif
            MockAssembly.DisplayCounts();

            var startInfo = new ProcessStartInfo(agentExe);
            startInfo.Arguments = testAssembly;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            Process? process = Process.Start(startInfo);

            if (process is not null)
            {
                process.WaitForExit();
                Console.WriteLine($"Agent process exited with rc={process.ExitCode}");

                string output = process.StandardOutput.ReadToEnd();
                if (!output.Contains("Test Run Summary"))
                    Assert.Fail("No Summary Report found");
            }
        }
    }
}
