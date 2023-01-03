// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NUnit.Engine
{
    internal class TestData
    {
#if DEBUG
        const string CONFIG = "Debug";
#else
        const string CONFIG = "Release";
#endif

#if NETCOREApp2_1
        const string RUNTIME = "netcoreapp2.1";
#elif NETCOREAPP3_1
        const string RUNTIME = "netcoreapp3.1";
#else
        const string RUNTIME = "net35";
#endif
        static readonly string BASE_DIR = Path.GetFullPath("../../../../../TestData/");
        static readonly string BIN_DIR = $"bin/{CONFIG}/";

        public static string MockAssemblyPath(string runtime)
            => $"{BASE_DIR}mock-assembly/{BIN_DIR}/{runtime}/mock-assembly.dll";
        public static string NoTestAssemblyPath(string runtime)
            => $"{BASE_DIR}notest-assembly/{BIN_DIR}/{runtime}/notest-assembly.dll";

        [Test]
        public void SelfTest()
        {
            VerifyFilePath(MockAssemblyPath("net35"));
            VerifyFilePath(NoTestAssemblyPath("net35"));
        }

        private void VerifyFilePath(string path)
        {
            Assert.That(File.Exists(path), $"File not found at {path}");
        }
    }
}
