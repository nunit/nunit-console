// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;

namespace NUnit.Engine.Services.RuntimeLocators
{
    public static class NetCoreRuntimeLocator
    {
        public static IEnumerable<RuntimeFramework> FindRuntimes()
        {
            const string WINDOWS_INSTALL_DIR = "C:\\Program Files\\dotnet\\";
            const string LINUX_INSTALL_DIR = "/usr/shared/dotnet/";
            string INSTALL_DIR = Path.DirectorySeparatorChar == '\\'
                ? WINDOWS_INSTALL_DIR
                : LINUX_INSTALL_DIR;
            string runtimeDir = Path.Combine(INSTALL_DIR, Path.Combine("shared", "Microsoft.NETCore.App"));

            if (Directory.Exists(INSTALL_DIR) &&
                File.Exists(Path.Combine(INSTALL_DIR, "dotnet.exe")) &&
                Directory.Exists(runtimeDir))
            {
                var dirList = new DirectoryInfo(runtimeDir).GetDirectories();
                var dirNames = new List<string>();
                foreach (var dir in dirList)
                    dirNames.Add(dir.Name);

                foreach (var runtime in GetNetCoreRuntimesFromDirectoryNames(dirNames))
                    yield return runtime;
            }
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

                runtimes.Add(new RuntimeFramework(Runtime.NetCore, newVersion));
            }

            return runtimes;
        }
    }
}
#endif
