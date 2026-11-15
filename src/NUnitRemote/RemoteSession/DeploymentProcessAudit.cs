// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Globalization;

namespace NUnit.Remote.Deployment
{
    /// <summary>Rejects cleanup while a native process still references the owned deployment.</summary>
    internal static class DeploymentProcessAudit
    {
        /// <summary>Requires a complete native process audit before any owned payload removal.</summary>
        internal static void AssertNoReferences(string root)
        {
            root = Path.GetFullPath(root);
            if (OperatingSystem.IsWindows())
            {
                WindowsProcessAudit.AssertNoReferences(root);
                return;
            }
            if (OperatingSystem.IsLinux())
            {
                foreach (string directory in Directory.EnumerateDirectories("/proc"))
                {
                    if (!int.TryParse(Path.GetFileName(directory), out int pid) || pid == Environment.ProcessId)
                    {
                        continue;
                    }
                    string command;
                    try
                    {
                        command = File.ReadAllText(Path.Combine(directory, "cmdline"));
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }
                    if (command.Split('\0').Any(argument => argument.StartsWith(root + "/", StringComparison.Ordinal)))
                    {
                        throw new IOException("An unrecorded native process references the deployment: " + pid);
                    }
                }
                return;
            }
            if (!OperatingSystem.IsMacOS())
            {
                throw new PlatformNotSupportedException("Native deployment auditing is required before cleanup.");
            }
            var start = new ProcessStartInfo("/bin/ps") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("-axo");
            start.ArgumentList.Add("pid=,command=");
            using var audit = Process.Start(start) ?? throw new IOException("Native process audit could not start.");
            var output = audit.StandardOutput.ReadToEndAsync();
            var error = audit.StandardError.ReadToEndAsync();
            try
            {
                audit.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            }
            catch
            {
                if (!audit.HasExited)
                {
                    audit.Kill();
                    audit.WaitForExit();
                }
                throw;
            }
            if (audit.ExitCode != 0)
            {
                throw new IOException("Native process audit failed: " + error.GetAwaiter().GetResult());
            }
            foreach (string line in output.GetAwaiter().GetResult().Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.TrimStart();
                int separator = trimmed.IndexOf(' ');
                if (separator < 1 || !int.TryParse(trimmed[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out int pid))
                {
                    throw new IOException("Native process audit returned an unrecognized entry.");
                }
                if (pid != Environment.ProcessId && pid != audit.Id && trimmed[(separator + 1)..].Contains(root + "/", StringComparison.Ordinal))
                {
                    throw new IOException("An unrecorded native process references the deployment: " + pid);
                }
            }
        }
    }
}
