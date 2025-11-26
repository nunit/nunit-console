// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Common;

namespace NUnit.Engine.Services.RuntimeLocators
{
    public static class NetCoreRuntimeLocator
    {
        public static IEnumerable<RuntimeFramework> FindRuntimes(bool forX86)
        {
            List<Version> alreadyFound = new List<Version>();

            foreach (var runtime in DotNet.GetRuntimes("Microsoft.NETCore.App", forX86))
            {
                if (!alreadyFound.Contains(runtime.Version))
                {
                    alreadyFound.Add(runtime.Version);
                    yield return new RuntimeFramework(Runtime.NetCore, runtime.Version);
                }
            }
        }
    }
}
#endif
