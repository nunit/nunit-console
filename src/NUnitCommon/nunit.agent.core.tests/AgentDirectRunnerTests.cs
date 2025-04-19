// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

            var startInfo = new ProcessStartInfo(agentExe);
            startInfo.Arguments = testAssembly;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

            Process? process = Process.Start(startInfo);

            if (process is not null)
            {
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                int index = output.IndexOf("Test Run Summary");
                if (index > 0)
                    output = output.Substring(index);

                Console.WriteLine(output);
                Console.WriteLine($"Agent process exited with rc={process.ExitCode}");

                if (index < 0)
                    Assert.Fail("No Summary Report found");
            }
        }
    }
}
