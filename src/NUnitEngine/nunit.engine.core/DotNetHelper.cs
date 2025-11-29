// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace NUnit.Engine
{
    public static class DotNet
    {
        private const string X64_SUBKEY1 = @"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\";
        private const string X64_SUBKEY2 = @"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x64\";
        private const string X86_SUBKEY1 = @"SOFTWARE\dotnet\SetUp\InstalledVersions\x86\InstallLocation\";
        private const string X86_SUBKEY2 = @"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\";

#pragma warning disable CA1416
        private static readonly Lazy<string> _x64InstallDirectory = new Lazy<string>(() =>
                Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? (
                    OS.IsWindows
                        ? (string) Registry.LocalMachine.OpenSubKey(X64_SUBKEY1)?.GetValue("Path") ??
                          (string) Registry.LocalMachine.OpenSubKey(X64_SUBKEY2)?.GetValue("Path") ?? @"C:\Program Files\dotnet"
                        : "/usr/shared/dotnet/"));
        private static readonly Lazy<string> _x86InstallDirectory = new Lazy<string>(() =>
                Environment.GetEnvironmentVariable("DOTNET_ROOT_X86") ?? (
                    OS.IsWindows
                        ? (string) Registry.LocalMachine.OpenSubKey(X86_SUBKEY1)?.GetValue("InstallLocation") ??
                          (string) Registry.LocalMachine.OpenSubKey(X86_SUBKEY2)?.GetValue("InstallLocation") ?? @"C:\Program Files (x86)\dotnet"
                        : "/usr/shared/dotnet/"));
#pragma warning restore CA1416

        private static Lazy<List<RuntimeInfo>> _x64Runtimes = new Lazy<List<RuntimeInfo>>(() => [.. GetAllRuntimes(x86: false)]);
        private static Lazy<List<RuntimeInfo>> _x86Runtimes = new Lazy<List<RuntimeInfo>>(() => [.. GetAllRuntimes(x86: true)]);

        /// <summary>
        /// DotNet.RuntimeInfo holds information about a single installed runtime.
        /// </summary>
        public class RuntimeInfo
        {
            /// <summary>
            /// Gets the runtime name, e.g. Microsoft.NetCore.App, Microsoft.AspNetCore.App or Microsoft.WindowsDesktop.App.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the package version as a string, possibly including a pre-release suffix.
            /// </summary>
            public string PackageVersion { get; }

            /// <summary>
            /// Gets the path to the directory containing assemblies for this runtime
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// Gets a flag, which is true if this runtime is a pre-release, otherwise fales.
            /// </summary>
            public bool IsPreRelease { get; }

            /// <summary>
            /// Gets the three-part version of this  runtime.
            /// </summary>
            public Version Version { get; }

            /// <summary>
            /// Gets the pre-release suffix if IsPreRelease is true, otherwise null
            /// </summary>
            public string PreReleaseSuffix { get; }


            /// <summary>
            /// Constructs a Runtime instance.
            /// </summary>
            public RuntimeInfo(string name, string packageVersion, string path)
            {
                Name = name;
                PackageVersion = packageVersion;
                Path = path;

                int dash = PackageVersion.IndexOf('-');
                IsPreRelease = dash > 0;

                if (IsPreRelease)
                {
                    Version = new Version(packageVersion.Substring(0, dash));
                    PreReleaseSuffix = packageVersion.Substring(dash + 1);
                }
                else
                    Version = new Version(packageVersion);
            }

            /// <summary>
            /// Parses a single line from the --list-runtimes display to create 
            /// an instance of DotNet.RuntimeInfo.
            /// </summary>
            /// <param name="line">Line from execution of dotnet --list-runtimes</param>
            /// <returns>A DotNet.RuntimeInfo</returns>
            public static RuntimeInfo Parse(string line)
            {
                string[] parts = line.Trim().Split([' '], 3);
                return new RuntimeInfo(parts[0], parts[1], parts[2].Trim(['[', ']']));
            }
        }

        /// <summary>
        /// Get the correct install directory, depending on whether we need X86 or X64 architecture.
        /// </summary>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        public static string GetInstallDirectory(bool x86) => x86
            ? _x86InstallDirectory.Value : _x64InstallDirectory.Value;

        /// <summary>
        /// Get the correct dotnet.exe, depending on whether we need X86 or X64 architecture.
        /// </summary>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        public static string GetDotnetExecutable(bool x86) => 
            Path.Combine(GetInstallDirectory(x86), OS.IsWindows ? "dotnet.exe" : "dotnet");

        /// <summary>
        /// Gets an enumeration of all installed runtimes matching the specified name and x86 flag.
        /// </summary>
        /// <param name="name">Name of the requested runtime</param>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        public static IEnumerable<RuntimeInfo> GetRuntimes(string name, bool x86)
        {
            var runtimes = x86 ? _x86Runtimes.Value : _x64Runtimes.Value;
            return runtimes.Where(r => r.Name == name);
        }

        /// <summary>
        /// Finds the "best" runtime for a particular asssembly version among those installed.
        /// May return null, if no suitable runtime is available.
        /// </summary>
        /// <param name="targetVersion">The version of assembly sought.</param>
        /// <param name="name">Name of the requested runtime</param>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        /// <param name="bestRuntime">Output variable set to the runtime that was found or null</param>
        /// <returns>True if a runtime was found, otherwise false</returns>
        public static bool FindBestRuntime(Version targetVersion, string name, bool x86, out RuntimeInfo bestRuntime) =>
            FindBestRuntime(targetVersion, GetRuntimes(name, x86), out bestRuntime);

        // Internal method used to facilitate testing
        internal static bool FindBestRuntime(Version targetVersion, IEnumerable<RuntimeInfo> availableRuntimes, out RuntimeInfo bestRuntime)
        {
            bestRuntime = null;

            if (targetVersion is null)
                return false;

            foreach (var candidate in availableRuntimes)
            {
                if (candidate.Version >= targetVersion)
                    if (bestRuntime is null || candidate.Version.Major == bestRuntime.Version.Major)
                        bestRuntime = candidate;
            }

            return bestRuntime is not null;
        }

        private static IEnumerable<RuntimeInfo> GetAllRuntimes(bool x86)
        {
            foreach (string line in DotnetCommand("--list-runtimes", x86: x86))
                yield return RuntimeInfo.Parse(line);
        }

        internal static IEnumerable<string> DotnetCommand(string arguments, bool x86 = false)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetDotnetExecutable(x86),
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
            }
            catch (Exception)
            {
                // Failed to start dotnet command. Assume no versions are installed and just return
                yield break;
            }

            while (!process.StandardOutput.EndOfStream)
                yield  return process.StandardOutput.ReadLine();
        }
    }
}
