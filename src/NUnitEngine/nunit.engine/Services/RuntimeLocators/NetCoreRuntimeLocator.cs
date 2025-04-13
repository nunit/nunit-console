// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NUnit.Engine.Services.RuntimeLocators
{
    public static class NetCoreRuntimeLocator
    {
        public static IEnumerable<RuntimeFramework> FindRuntimes(bool forX86)
        {
            List<Version> alreadyFound = new List<Version>();

            foreach (string dirName in GetRuntimeDirectories(forX86))
            {
                Version newVersion;
                if (TryGetVersionFromString(dirName, out newVersion) && !alreadyFound.Contains(newVersion))
                {
                    alreadyFound.Add(newVersion);
                    yield return new RuntimeFramework(Runtime.NetCore, newVersion);
                }
            }

            foreach (string line in GetRuntimeList())
            {
                Version newVersion;
                if (TryGetVersionFromString(line, out newVersion) && !alreadyFound.Contains(newVersion))
                {
                    alreadyFound.Add(newVersion);
                    yield return new RuntimeFramework(Runtime.NetCore, newVersion);
                }
            }
        }

        private static IEnumerable<string> GetRuntimeDirectories(bool x86)
        {
            string? installDir = DotNet.GetInstallDirectory(x86);

            if (installDir is not null && Directory.Exists(installDir) &&
                File.Exists(Path.Combine(installDir, "dotnet.exe")))
            {
                string runtimeDir = Path.Combine(installDir, Path.Combine("shared", "Microsoft.NETCore.App"));
                if (Directory.Exists(runtimeDir))
                    foreach (var dir in new DirectoryInfo(runtimeDir).GetDirectories())
                        yield return dir.Name;
            }
        }

        private static IEnumerable<string> GetRuntimeList()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
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
                // Failed to start dotnet command. Assume no versions are installed and just r eturn just return
                yield break;
            }

            const string PREFIX = "Microsoft.NETCore.App ";
            const int VERSION_START = 22;

            string? line;
            while ((line = process.StandardOutput.ReadLine()) is not null)
            {
                if (line.StartsWith(PREFIX))
                    yield return line.Substring(VERSION_START);
            }
        }

        private static bool TryGetVersionFromString(string text, out Version newVersion)
        {
            const string VERSION_CHARS = ".0123456789";

            int len = 0;
            foreach (char c in text)
            {
                if (VERSION_CHARS.IndexOf(c) >= 0)
                    len++;
                else
                    break;
            }

            try
            {
                var fullVersion = new Version(text.Substring(0, len));
                newVersion = new Version(fullVersion.Major, fullVersion.Minor);
                return true;
            }
            catch
            {
                newVersion = new Version();
                return false;
            }
        }
    }
}
#endif
