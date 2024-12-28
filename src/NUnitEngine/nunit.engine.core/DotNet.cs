﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NUnit.Engine
{
    public static class DotNet
    {
        public static string GetInstallDirectory() => Environment.Is64BitProcess
            ? GetX64InstallDirectory() : GetX86InstallDirectory();

        public static string GetInstallDirectory(bool x86) => x86
            ? GetX86InstallDirectory() : GetX64InstallDirectory();

        private static string _x64InstallDirectory;
        public static string GetX64InstallDirectory()
        {
            if (_x64InstallDirectory == null)
                _x64InstallDirectory = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (_x64InstallDirectory == null)
            {
#if NETFRAMEWORK
                if (Path.DirectorySeparatorChar == '\\')
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                    _x64InstallDirectory = (string)key?.GetValue("Path");
                }
                else
                    _x64InstallDirectory = "/usr/shared/dotnet/";
            }

            return _x64InstallDirectory;
        }

        private static string _x86InstallDirectory;
        public static string GetX86InstallDirectory()
        {
            if (_x86InstallDirectory == null)
                _x86InstallDirectory = Environment.GetEnvironmentVariable("DOTNET_ROOT_X86");

            if (_x86InstallDirectory == null)
            {
#if NETFRAMEWORK
                if (Path.DirectorySeparatorChar == '\\')
#else
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\");
                    _x86InstallDirectory = (string)key?.GetValue("InstallLocation");
                }
                else
                    _x86InstallDirectory = "/usr/shared/dotnet/";
            }

            return _x86InstallDirectory;
        }
    }
}
