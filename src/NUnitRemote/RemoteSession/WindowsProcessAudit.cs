// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace NUnit.Remote.Deployment
{
    internal static class WindowsProcessAudit
    {
        internal static void AssertNoReferences(string root)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }
            const string script = """
$ErrorActionPreference='Stop'
$ProgressPreference='SilentlyContinue'
$root=$env:NUNIT_REMOTE_CONTROL_ROOT.Replace('/','\')+'\'
$controlPid=[int]$env:NUNIT_REMOTE_CONTROL_PID
$matches=@(Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -ne $controlPid -and $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf($root,[StringComparison]::OrdinalIgnoreCase) -ge 0 } | Select-Object ProcessId,Name)
ConvertTo-Json -InputObject $matches -Compress
""";
            string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            var start = new ProcessStartInfo(shell) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string value in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) })
            {
                start.ArgumentList.Add(value);
            }
            start.Environment["NUNIT_REMOTE_CONTROL_ROOT"] = root;
            start.Environment["NUNIT_REMOTE_CONTROL_PID"] = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            using var process = Process.Start(start) ?? throw new IOException("Windows process audit did not start");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
                throw new TimeoutException("Windows process audit is incomplete; preserve deployment");
            }
            if (!Task.WhenAll(stdout, stderr).Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Windows process audit output is incomplete");
            }
            if (process.ExitCode != 0)
            {
                throw new IOException("Windows process audit failed: " + stderr.Result);
            }
            using var document = JsonDocument.Parse(stdout.Result);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() != 0)
            {
                throw new IOException("Unrecorded process references the remote deployment: " + stdout.Result);
            }
        }
    }
}
