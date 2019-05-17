#if !NETCOREAPP1_1
using System;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Integration
{
    internal sealed class RunnerInDirectoryWithoutFramework : IDisposable
    {
        private readonly DirectoryWithNeededAssemblies directory;

        public string ConsoleExe => Path.Combine(directory.Directory, "nunit3-console.exe");
        public string AgentExe => Path.Combine(directory.Directory, "nunit-agent.exe");
        public string AgentX86Exe => Path.Combine(directory.Directory, "nunit-agent-x86.exe");

        public RunnerInDirectoryWithoutFramework()
        {
            directory = new DirectoryWithNeededAssemblies("nunit3-console", "nunit-agent", "nunit-agent-x86");

            Assert.That(Path.Combine(directory.Directory, "nunit.framework.dll"), Does.Not.Exist, "This test must be run without nunit.framework.dll in the same directory as the console runner.");
        }

        public void Dispose()
        {
            for (;;) try
            {
                File.Delete(AgentExe);
                File.Delete(AgentX86Exe);
                break;
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(100);
            }

            directory.Dispose();
        }
    }
}
#endif