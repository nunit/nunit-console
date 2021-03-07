// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace NUnit.Engine.Internal.RuntimeFrameworks
{
    internal static class DotNetFrameworkLocator
    {
        // Note: this method cannot be generalized past V4, because (a)  it has
        // specific code for detecting .NET 4.5 and (b) we don't know what
        // microsoft will do in the future
        public static IEnumerable<RuntimeFramework> FindDotNetFrameworks()
        {
            // Handle Version 1.0, using a different registry key
            foreach (var framework in FindExtremelyOldDotNetFrameworkVersions())
                yield return framework;

            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP");
            if (key != null)
            {
                foreach (string name in key.GetSubKeyNames())
                {
                    if (name.StartsWith("v") && name != "v4.0") // v4.0 is a duplicate, legacy key
                    {
                        var versionKey = key.OpenSubKey(name);
                        if (versionKey == null) continue;

                        if (name.StartsWith("v4", StringComparison.Ordinal))
                        {
                            // Version 4 and 4.5
                            foreach (var framework in FindDotNetFourFrameworkVersions(versionKey))
                            {
                                yield return framework;
                            }
                        }
                        else if (CheckInstallDword(versionKey))
                        {
                            // Versions 1.1, 2.0, 3.0 and 3.5 are possible here
                            yield return new RuntimeFramework(RuntimeType.Net, new Version(name.Substring(1, 3)));
                        }
                    }
                }
            }
        }

        private static IEnumerable<RuntimeFramework> FindExtremelyOldDotNetFrameworkVersions()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework\policy\v1.0");
            if (key == null)
                yield break;

            foreach (var build in key.GetValueNames())
                yield return new RuntimeFramework(RuntimeType.Net, new Version("1.0." + build));
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

        private static IEnumerable<RuntimeFramework> FindDotNetFourFrameworkVersions(RegistryKey versionKey)
        {
            foreach (string profile in new string[] { "Full", "Client" })
            {
                var profileKey = versionKey.OpenSubKey(profile);
                if (profileKey == null) continue;

                if (CheckInstallDword(profileKey))
                {
                    yield return new RuntimeFramework(RuntimeType.Net, new Version(4, 0), profile);

                    var release = (int)profileKey.GetValue("Release", 0);
                    foreach (var entry in ReleaseTable)
                        if (release >= entry.Release)
                            yield return new RuntimeFramework(RuntimeType.Net, entry.Version);


                    yield break;     //If full profile found don't check for client profile
                }
            }
        }

        private static bool CheckInstallDword(RegistryKey key)
        {
            return ((int)key.GetValue("Install", 0) == 1);
        }
    }
}
#endif
