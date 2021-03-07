// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Provides static methods for accessing configuration info
    /// </summary>
    public static class NUnitConfiguration
    {
        private static string _engineDirectory;
        public static string EngineDirectory
        {
            get
            {
                if (_engineDirectory == null)
                    _engineDirectory =
                        AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());

                return _engineDirectory;
            }
        }

        private static string _applicationDirectory;
        public static string ApplicationDirectory
        {
            get
            {
                if (_applicationDirectory == null)
                {
                    _applicationDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "NUnit");
                }

                return _applicationDirectory;
            }
        }
    }
}
