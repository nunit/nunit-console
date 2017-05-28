// ***********************************************************************
// Copyright (c) 2017 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace NUnit.Engine.Services.FrameworkUtilities
{
    internal static class DotNetFrameworkLocator
    {
        internal static ICollection<RuntimeFramework> FindAvailableDotNetFrameworks()
        {
            var availableFrameworks = new List<RuntimeFramework>();

            // Handle Version 1.0, using a different registry key
            availableFrameworks.AddRange(FindExtremelyOldDotNetFrameworkVersions());

            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\NET Framework Setup\NDP");
            if (key == null) return availableFrameworks;

            foreach (string name in key.GetSubKeyNames())
            {
                if (!name.StartsWith("v") || name == "v4.0") // v4.0 is a duplicate, legacy key
                    continue;

                var versionKey = key.OpenSubKey(name);
                if (versionKey == null)
                    continue;

                if (name.StartsWith("v4", StringComparison.Ordinal))
                    // Version 4 and 4.5
                    availableFrameworks.AddRange(FindDotNetFourFrameworkVersions(versionKey));
                else if (CheckInstallDword(versionKey))
                {
                    // Versions 1.1 through 3.5
                    var version = new Version(name.Substring(1));
                    availableFrameworks.Add(new RuntimeFramework(RuntimeType.Net, version));
                }
            }

            return availableFrameworks;
        }

        private static IEnumerable<RuntimeFramework> FindExtremelyOldDotNetFrameworkVersions()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\.NETFramework\policy\v1.0");
            if (key == null) yield break;

            foreach (string build in key.GetValueNames())
                yield return new RuntimeFramework(RuntimeType.Net, new Version("1.0." + build));
        }

        // Note: this method cannot be generalized past V4, because (a) it has
        // specific code for detecting .NET 4.5 and (b) we don't know what
        // Microsoft will do in the future
        private static IEnumerable<RuntimeFramework> FindDotNetFourFrameworkVersions(RegistryKey versionKey)
        {
            foreach (string profile in new[] { "Full", "Client" })
            {
                var profileKey = versionKey.OpenSubKey(profile);
                if (profileKey == null) continue;

                if (CheckInstallDword(profileKey))
                {
                    yield return new RuntimeFramework(RuntimeType.Net, new Version(4, 0), profile);

                    var release = (int)profileKey.GetValue("Release", 0);
                    if (release > 0) // TODO: Other higher versions?
                    {
                        yield return new RuntimeFramework(RuntimeType.Net, new Version(4, 5));
                    }

                    yield break;     //If full profile found, return and don't check for client profile
                }
            }
        }

        private static bool CheckInstallDword(RegistryKey key)
        {
            return (int)key.GetValue("Install", 0) == 1;
        }
    }
}
