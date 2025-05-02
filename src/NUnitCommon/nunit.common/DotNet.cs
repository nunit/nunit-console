// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NUnit.Common
{
    public static class DotNet
    {
        public static string? GetInstallDirectory() => Environment.Is64BitProcess
            ? GetX64InstallDirectory() : GetX86InstallDirectory();

        public static string? GetInstallDirectory(bool x86) => x86
            ? GetX86InstallDirectory() : GetX64InstallDirectory();

        private static string? _x64InstallDirectory;
        public static string? GetX64InstallDirectory()
        {
            if (_x64InstallDirectory is null)
                _x64InstallDirectory = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (_x64InstallDirectory is null)
            {
#if NETFRAMEWORK
                if (Path.DirectorySeparatorChar == '\\')
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\"))
                        _x64InstallDirectory = (string?)key?.GetValue("Path");
                }
                else
                    _x64InstallDirectory = "/usr/shared/dotnet/";
            }

            return _x64InstallDirectory;
        }

        private static string? _x86InstallDirectory;
        public static string? GetX86InstallDirectory()
        {
            if (_x86InstallDirectory is null)
                _x86InstallDirectory = Environment.GetEnvironmentVariable("DOTNET_ROOT_X86");

            if (_x86InstallDirectory is null)
            {
#if NETFRAMEWORK
                if (Path.DirectorySeparatorChar == '\\')
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\"))
                        _x86InstallDirectory = (string?)key?.GetValue("InstallLocation");
                }
                else
                    _x86InstallDirectory = "/usr/shared/dotnet/";
            }

            return _x86InstallDirectory;
        }

        public static string GetDotNetExe(bool runAsX86)
        {
            string? installDirectory = DotNet.GetInstallDirectory(runAsX86);
            if (installDirectory is not null)
            {
                var dotnet_exe = Path.Combine(installDirectory, "dotnet.exe");
                if (File.Exists(dotnet_exe))
                    return dotnet_exe;
            }

            var msg = runAsX86
                ? "The X86 version of dotnet.exe is not installed."
                : "Unable to locate dotnet.exe.";

            throw new Exception(msg);
        }
    }
}
