// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace NUnit.Engine
{
    internal class Platform
    {
#if !NETFRAMEWORK
        [SupportedOSPlatformGuard("windows")]
#endif
        public static bool IsWindows =>
#if NETFRAMEWORK
            Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#endif
    }
}
