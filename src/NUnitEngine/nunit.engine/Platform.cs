// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

namespace NUnit.Engine
{
    internal class Platform
    {
        /// <summary>
        /// Gets a value indicating whether the engine is hosted by Mono.
        /// </summary>
        public static bool IsMono => Type.GetType("Mono.Runtime", false) is not null;

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
