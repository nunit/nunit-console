// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public enum Architecture
        {
            Unspecified,
            X64,
            X86
        }

        public class RuntimeInfo
        {
            public string Name;
            public Version Version;
            public string Path;

            public RuntimeInfo(string name, string version, string path)
                : this(name, new Version(version), path) { }

            public RuntimeInfo(string name, Version version, string path)
            {
                Name = name;
                Version = version;
                Path = path;
            }
        }

        /// <summary>
        /// Get the correct install directory, depending on whether we need X86 or X64 architecture.
        /// </summary>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        /// <returns></returns>
        public static string GetInstallDirectory(bool x86) => x86
            ? _x86InstallDirectory.Value : _x64InstallDirectory.Value;

        /// <summary>
        /// Get the correct dotnet.exe, depending on whether we need X86 or X64 architecture.
        /// </summary>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        /// <returns></returns>
        public static string GetDotnetExecutable(bool x86) => Path.Combine(GetInstallDirectory(x86), OS.IsWindows ? "dotnet.exe" : "dotnet");

        public static IEnumerable<RuntimeInfo> GetRuntimes(string name, bool x86)
        {
            var runtimes = x86 ? _x86Runtimes.Value : _x64Runtimes.Value;
            return runtimes.Where(r => r.Name == name);
        }

        private static IEnumerable<RuntimeInfo> GetAllRuntimes(bool x86)
        {
            foreach (string line in DotnetCommand("--list-runtimes", x86: x86))
            {
                string[] parts = line.Trim().Split([' '], 3);
                yield return new RuntimeInfo(parts[0], parts[1], parts[2].Trim(['[', ']']));
            }
        }

        private static IEnumerable<string> DotnetCommand(string arguments, bool x86 = false)
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
