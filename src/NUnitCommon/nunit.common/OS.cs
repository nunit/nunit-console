// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;

namespace NUnit.Common
{
    public static class OS
    {
        public static bool IsWindows { get; } = Path.DirectorySeparatorChar == '\\';
    }
}
