// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Remote.Deployment;

namespace NUnit.Engine.Tests.RemoteSession
{
    /// <summary>
    /// Checks Windows command-line reference detection using one owned, bounded
    /// child. No NUnit server, installed agent, processor or network is required.
    /// </summary>
    [TestFixture, Platform("Win"), Category("Integration"), NonParallelizable]
    public class WindowsSessionProcessTests
    {
        /// <summary>
        /// Blocks deletion while an unrecorded process refers to the deployment,
        /// without mistaking a sibling prefix for a deployment reference.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ProcessReferenceRequiresTheExactDeploymentDirectory(bool sibling)
        {
            string testRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "session-process-" + Guid.NewGuid().ToString("N"));
            string deployment = Path.Combine(testRoot, "deployment");
            string childRoot = sibling ? deployment + "-sibling" : deployment;
            Directory.CreateDirectory(childRoot);
            string script = Path.Combine(childRoot, "hold.ps1");
            File.WriteAllText(script, "[Console]::WriteLine('ready'); [Console]::Out.Flush(); [void][Console]::In.ReadToEnd()");
            string sentinel = Path.Combine(childRoot, "sentinel.txt");
            File.WriteAllText(sentinel, "must-remain");
            var start = new ProcessStartInfo(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string argument in new[] { "-NoProfile", "-NonInteractive", "-File", script })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new IOException("The owned audit-test child did not start.");
            var stderr = process.StandardError.ReadToEndAsync();
            try
            {
                string? ready = process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
                Assert.That(ready, Is.EqualTo("ready"));
                if (sibling)
                    Assert.That(() => DeploymentProcessAudit.AssertNoReferences(deployment), Throws.Nothing);
                else
                    Assert.That(() => DeploymentProcessAudit.AssertNoReferences(deployment), Throws.TypeOf<IOException>());
                Assert.That(File.ReadAllText(sentinel), Is.EqualTo("must-remain"));
            }
            finally
            {
                if (!process.HasExited)
                    process.StandardInput.Close();
                if (!process.WaitForExit(10000))
                {
                    // Terminate only the child created by this test, through its retained handle.
                    process.Kill();
                    process.WaitForExit();
                    Assert.Fail("The owned audit-test child did not stop after its input closed.");
                }
            }
            Assert.That(process.ExitCode, Is.Zero, stderr.GetAwaiter().GetResult());
            Assert.That(() => DeploymentProcessAudit.AssertNoReferences(deployment), Throws.Nothing);
            Assert.That(File.ReadAllText(sentinel), Is.EqualTo("must-remain"));
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed)
            {
                Assert.That(Path.GetDirectoryName(testRoot), Is.EqualTo(Path.GetFullPath(TestContext.CurrentContext.WorkDirectory)));
                _ = DeploymentPaths.Files(testRoot).Count();
                Directory.Delete(testRoot, recursive: true);
            }
        }
    }
}
