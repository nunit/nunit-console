// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK && NYI
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NUnit.Engine.Services.RuntimeLocators
{
    public static class MonoRuntimeLocator
    {
        //public static IEnumerable<RuntimeFramework> FindRuntimes()
        //{
        //    var current = RuntimeFramework.CurrentFramework;
        //    if (current.Runtime == Runtime.Mono)
        //    {
        //        yield return current;
        //    }
        //    else
        //    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        //        FindBestMonoFrameworkOnWindows();
        //}

        //private static void UseCurrentMonoFramework()
        //{
        //    Debug.Assert(CurrentFramework.Runtime == Runtime.Mono && MonoPrefix != null && MonoVersion != null);

        //    // Multiple profiles are no longer supported with Mono 4.0
        //    if (RuntimeFramework.MonoVersion.Major < 4 && FindAllMonoProfiles() > 0)
        //        return;

        //    // If Mono 4.0+ or no profiles found, just use current runtime
        //    _availableFrameworks.Add(RuntimeFramework.CurrentFramework);
        //}

        //private static void FindBestMonoFrameworkOnWindows()
        //{
        //    // First, look for recent frameworks that use the Software\Mono Key
        //    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Mono");

        //    if (key != null && (int)key.GetValue("Installed", 0) == 1)
        //    {
        //        string version = key.GetValue("Version") as string;
        //        MonoPrefix = key.GetValue("SdkInstallRoot") as string;

        //        if (version != null)
        //        {
        //            MonoVersion = new Version(version);
        //            AddMonoFramework(new Version(4, 5), null);
        //            return;
        //        }
        //    }

        //    // Some later 3.x Mono releases didn't use the registry
        //    // so check in standard location for them.
        //    if (Directory.Exists(DEFAULT_WINDOWS_MONO_DIR))
        //    {
        //        MonoPrefix = DEFAULT_WINDOWS_MONO_DIR;
        //        AddMonoFramework(new Version(4, 5), null);
        //        return;
        //    }

        //    // Look in the Software\Novell key for older versions
        //    key = Registry.LocalMachine.OpenSubKey(@"Software\Novell\Mono");
        //    if (key != null)
        //    {
        //        string version = key.GetValue("DefaultCLR") as string;
        //        if (version != null)
        //        {
        //            RegistryKey subKey = key.OpenSubKey(version);
        //            if (subKey != null)
        //            {
        //                MonoPrefix = subKey.GetValue("SdkInstallRoot") as string;
        //                MonoVersion = new Version(version);

        //                FindAllMonoProfiles();
        //            }
        //        }
        //    }
        //}

        //private static int FindAllMonoProfiles()
        //{
        //    int count = 0;

        //    if (MonoPrefix != null)
        //    {
        //        if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/1.0/mscorlib.dll")))
        //        {
        //            AddMonoFramework(new Version(1, 1, 4322), "1.0");
        //            count++;
        //        }

        //        if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/2.0/mscorlib.dll")))
        //        {
        //            AddMonoFramework(new Version(2, 0), "2.0");
        //            count++;
        //        }

        //        if (Directory.Exists(Path.Combine(MonoPrefix, "lib/mono/3.5")))
        //        {
        //            AddMonoFramework(new Version(3, 5), "3.5");
        //            count++;
        //        }

        //        if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/4.0/mscorlib.dll")))
        //        {
        //            AddMonoFramework(new Version(4, 0), "4.0");
        //            count++;
        //        }

        //        if (File.Exists(Path.Combine(MonoPrefix, "lib/mono/4.5/mscorlib.dll")))
        //        {
        //            AddMonoFramework(new Version(4, 5), "4.5");
        //            count++;
        //        }
        //    }

        //    return count;
        //}

        //private static void AddMonoFramework(Version frameworkVersion, string profile)
        //{
        //    var framework = new RuntimeFramework(Runtime.Mono, frameworkVersion)
        //    {
        //        Profile = profile,
        //        DisplayName = MonoVersion != null
        //            ? "Mono " + MonoVersion.ToString() + " - " + profile + " Profile"
        //            : "Mono - " + profile + " Profile"
        //    };

        //    _availableFrameworks.Add(framework);
        //}
    }
}
#endif
