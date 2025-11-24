// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace NUnit.Engine
{
    public static class DotNet
    {
        private const string X64_SUBKEY1 = @"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\";
        private const string X64_SUBKEY2 = @"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x64\";
        private const string X86_SUBKEY1 = @"SOFTWARE\dotnet\SetUp\InstalledVersions\x86\InstallLocation\";
        private const string X86_SUBKEY2 = @"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\";

        public static readonly string X64InstallDirectory;
        public static readonly string X86InstallDirectory;
        public static readonly List<RuntimeInfo> Runtimes;

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
        /// Static constructor initializes everything once
        /// </summary>
        static DotNet()
        {
#pragma warning disable CA1416
            X64InstallDirectory = 
                Environment.GetEnvironmentVariable("DOTNET_ROOT") ?? (
                    IsWindows
                        ? (string)Registry.LocalMachine.OpenSubKey(X64_SUBKEY1)?.GetValue("Path") ??
                          (string)Registry.LocalMachine.OpenSubKey(X64_SUBKEY2)?.GetValue("Path") ?? @"C:\Program Files\dotnet"
                        : "/usr/shared/dotnet/");
            X86InstallDirectory = 
                Environment.GetEnvironmentVariable("DOTNET_ROOT_X86") ?? (
                    IsWindows
                        ? (string)Registry.LocalMachine.OpenSubKey(X86_SUBKEY1)?.GetValue("InstallLocation") ??
                          (string)Registry.LocalMachine.OpenSubKey(X86_SUBKEY2)?.GetValue("InstallLocation") ?? @"C:\Program Files (x86)\dotnet"
                        : "/usr/shared/dotnet/");
#pragma warning restore CA1416
            Runtimes = new List<RuntimeInfo>();
            foreach (string line in DotnetCommand("--list-runtimes"))
            {
                string[] parts = line.Trim().Split([' '], 3);
                Runtimes.Add(new RuntimeInfo(parts[0], parts[1], parts[2].Trim(['[', ']'])));
            }
        }

        /// <summary>
        /// Get the correct install directory, depending on whether we need X86 or X64 architecture.
        /// </summary>
        /// <param name="x86">Flag indicating whether the X86 architecture is needed</param>
        /// <returns></returns>
        public static string GetInstallDirectory(bool x86) => x86
            ? X86InstallDirectory : X64InstallDirectory;

        public static IEnumerable<RuntimeInfo> GetRuntimes(string name) => Runtimes.Where(r => r.Name == name);

        private static IEnumerable<string> DotnetCommand(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
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

#if NETFRAMEWORK
        private static bool IsWindows => Path.DirectorySeparatorChar == '\\';
#else
        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif

    }
}
