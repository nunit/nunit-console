// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NUnit.Engine.Internal.RuntimeFrameworks
{
    internal static class NetCoreFrameworkLocator
    {
        public static IEnumerable<RuntimeFramework> FindDotNetCoreFrameworks()
        {
            string installDir = GetDotNetInstallDirectory();

            if (installDir == null || !Directory.Exists(installDir) ||
                !File.Exists(Path.Combine(installDir, "dotnet.exe")))
                return new RuntimeFramework[0];

            string runtimeDir = Path.Combine(installDir, Path.Combine("shared", "Microsoft.NETCore.App"));
            if (!Directory.Exists(runtimeDir))
                return new RuntimeFramework[0];

            var dirList = new DirectoryInfo(runtimeDir).GetDirectories();
            var dirNames = new List<string>();
            foreach (var dir in dirList)
                dirNames.Add(dir.Name);

            return GetNetCoreRuntimesFromDirectoryNames(dirNames);
        }

        internal static string GetDotNetInstallDirectory()
        {
            if (Path.DirectorySeparatorChar == '\\')
            {
                // Running on Windows so use registry
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                return (string)key?.GetValue("Path");
            }
            else
                return "/usr/shared/dotnet/";
        }

        // Deal with oddly named directories, which may sometimes appear when previews are installed
        internal static IList<RuntimeFramework> GetNetCoreRuntimesFromDirectoryNames(IEnumerable<string> dirNames)
        {
            const string VERSION_CHARS = ".0123456789";
            var runtimes = new List<RuntimeFramework>();

            foreach (string dirName in dirNames)
            {
                int len = 0;
                foreach (char c in dirName)
                {
                    if (VERSION_CHARS.IndexOf(c) >= 0)
                        len++;
                    else
                        break;
                }

                if (len == 0)
                    continue;

                Version fullVersion = null;
                try
                {
                    fullVersion = new Version(dirName.Substring(0, len));
                }
                catch
                {
                    continue;
                }

                var newVersion = new Version(fullVersion.Major, fullVersion.Minor);
                int count = runtimes.Count;
                if (count > 0 && runtimes[count - 1].FrameworkVersion == newVersion)
                    continue;

                runtimes.Add(new RuntimeFramework(RuntimeType.NetCore, newVersion));
            }

            return runtimes;
        }
    }
}
#endif
