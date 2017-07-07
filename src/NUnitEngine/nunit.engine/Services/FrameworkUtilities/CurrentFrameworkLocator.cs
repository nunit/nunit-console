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
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace NUnit.Engine.Services.FrameworkUtilities
{
    internal class CurrentFrameworkLocator
    {
        public static RuntimeFramework GetCurrentFramework()
        {
            Type monoRuntimeType = Type.GetType("Mono.Runtime", false);
            bool isMono = monoRuntimeType != null;

            RuntimeType runtime = isMono
                ? RuntimeType.Mono
                : Environment.OSVersion.Platform == PlatformID.WinCE
                    ? RuntimeType.NetCF
                    : RuntimeType.Net;

            int major = Environment.Version.Major;
            int minor = Environment.Version.Minor;

            if (isMono)
            {
                switch (major)
                {
                    case 1:
                        minor = 0;
                        break;
                    case 2:
                        major = 3;
                        minor = 5;
                        break;
                }
            }
            else /* It's windows */
            if (major == 2)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
                string installRoot = key?.GetValue("InstallRoot") as string;
                if (installRoot != null)
                {
                    if (Directory.Exists(Path.Combine(installRoot, "v3.5")))
                    {
                        major = 3;
                        minor = 5;
                    }
                    else if (Directory.Exists(Path.Combine(installRoot, "v3.0")))
                    {
                        major = 3;
                        minor = 0;
                    }
                }
            }
            else if (major == 4 && Type.GetType("System.Reflection.AssemblyMetadataAttribute") != null)
            {
                minor = 5;
            }

            var currentFramework = new RuntimeFramework(runtime, new Version(major, minor), clrVersion: Environment.Version);

            if (!isMono)
                return currentFramework;

            // TODO: This was left where it always has been - probably wants to be moved.
            if (RuntimeFramework.MonoPrefix == null)
                RuntimeFramework.MonoPrefix = GetMonoPrefixFromAssembly(monoRuntimeType.Assembly);

            return currentFramework;
        }

        private static string GetMonoPrefixFromAssembly(Assembly assembly)
        {
            string prefix = assembly.Location;

            // In all normal mono installations, there will be sufficient
            // levels to complete the four iterations. But just in case
            // files have been copied to some non-standard place, we check.
            for (int i = 0; i < 4; i++)
            {
                string dir = Path.GetDirectoryName(prefix);
                if (string.IsNullOrEmpty(dir)) break;

                prefix = dir;
            }

            return prefix;
        }
    }
}
