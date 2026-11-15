// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Remote.Deployment
{
    /// <summary>Identifies one standard launcher bundle and its source-relative entry point.</summary>
    internal sealed record LauncherBundle(string Name, string ToolsDirectory, string LauncherRelativePath);

    /// <summary>
    /// Supplies explicit local inputs for an ephemeral session. Backend runtime installation,
    /// transport credentials and server discovery remain outside this definition.
    /// </summary>
    internal sealed record DeploymentDefinition
    {
        public required string DeploymentRoot { get; init; }
        public required string EvidenceDirectory { get; init; }
        public required string HostDirectory { get; init; }

        /// <summary>
        /// Gets the manifest-relative managed host entry point. The legacy default
        /// preserves existing prototype plans; new plans select nunit-test-server.dll.
        /// </summary>
        public string HostAssemblyRelativePath { get; init; } = "RemoteProviderHost.dll";
        public required string PackageDirectory { get; init; }
        public required string AssemblyRelativePath { get; init; }
        public required IReadOnlyList<LauncherBundle> Launchers { get; init; }
        public string DotnetExecutable { get; init; } = "dotnet";
        public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(15);
        public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(35);
    }

    /// <summary>Records source-relative payload bytes without embedding coordinator file paths.</summary>
    internal sealed record DeploymentFile(string Name, long Bytes, string Sha256);

    /// <summary>Preserves lifecycle and cleanup evidence outside the disposable deployment.</summary>
    internal sealed class DeploymentReceipt
    {
        public string Owner { get; init; } = string.Empty;
        public string Directory { get; init; } = string.Empty;
        public string ManifestSha256 { get; set; } = string.Empty;
        public string State { get; set; } = "Preparing";
        public int? HostPid { get; set; }
        public DateTime? HostStartUtc { get; set; }
        public int? HostExitCode { get; set; }
        public bool StartupConfirmed { get; set; }
        public bool NativeAuditPassed { get; set; }
        public bool Cleaned { get; set; }
        public List<string> Errors { get; } = new List<string>();
    }
}
