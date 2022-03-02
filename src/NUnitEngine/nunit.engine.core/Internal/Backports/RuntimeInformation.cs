// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace NUnit.Engine.Internal.Backports
{
    /// <summary>
    /// Partially replaces System.Runtime.InteropServices.RuntimeInformation
    /// under the .NET Framework. Only FrameworkDescription is implemented.
    /// </summary>
    public static class RuntimeInformation
    {
        public static string FrameworkDescription
        {
            get
            {
                bool isMono = Type.GetType("Mono.Runtime", false) != null;
                string runtime = isMono ? "Mono" : ".NET";

                Version version = new Version(Environment.Version.Major, Environment.Version.Minor);

                if (isMono)
                {
                    switch (version.Major)
                    {
                        case 1:
                            version = new Version(1, 0);
                            break;
                        case 2:
                            version = new Version(3, 5);
                            break;
                    }
                }
                else /* We must be on Windows */
                {
                    switch (version.Major)
                    {
                        case 2:
                            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
                            if (key != null)
                            {
                                string installRoot = key.GetValue("InstallRoot") as string;
                                if (installRoot != null)
                                {
                                    if (Directory.Exists(System.IO.Path.Combine(installRoot, "v3.5")))
                                    {
                                        version = new Version(3, 5);
                                    }
                                    else if (Directory.Exists(System.IO.Path.Combine(installRoot, "v3.0")))
                                    {
                                        version = new Version(3, 0);
                                    }
                                }
                            }
                            break;
                        case 4:
                            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
                            if (key != null)
                            {
                                version = new Version(4, 5);
                                int release = (int)key.GetValue("Release", 0);
                                foreach (var entry in ReleaseTable)
                                    if (release >= entry.Release)
                                        version = entry.Version;
                            }
                            break;
                    }
                }

                return $"{runtime} {version}";
            }
        }

        private struct MinimumRelease
        {
            public readonly int Release;
            public readonly Version Version;

            public MinimumRelease(int release, Version version)
            {
                Release = release;
                Version = version;
            }
        }

        private static readonly MinimumRelease[] ReleaseTable = new MinimumRelease[]
        {
            new MinimumRelease(378389, new Version(4, 5)),
            new MinimumRelease(378675, new Version(4, 5, 1)),
            new MinimumRelease(379893, new Version(4, 5, 2)),
            new MinimumRelease(393295, new Version(4, 6)),
            new MinimumRelease(394254, new Version(4, 6, 1)),
            new MinimumRelease(394802, new Version(4, 6, 2)),
            new MinimumRelease(460798, new Version(4, 7)),
            new MinimumRelease(461308, new Version(4, 7, 1)),
            new MinimumRelease(461808, new Version(4, 7, 2)),
            new MinimumRelease(528040, new Version(4, 8))
        };
    }
}
#endif
