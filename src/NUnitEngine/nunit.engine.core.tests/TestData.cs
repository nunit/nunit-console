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
#if NETCOREAPP3_1
        const string CURRENT_RUNTIME = "netcoreapp3.1";
#elif NET6_0
        const string CURRENT_RUNTIME = "net6.0";
#elif NET8_0
        const string CURRENT_RUNTIME = "net8.0";
#else
        const string CURRENT_RUNTIME = "net462";
#endif
        public static string MockAssemblyPath(string runtime)
            => $"testdata/{runtime}/mock-assembly.dll";
        public static string NoTestAssemblyPath(string runtime)
            => $"testdata/{runtime}/notest-assembly.dll";

        [Test]
        public void SelfTest()
        {
            VerifyFilePath(MockAssemblyPath(CURRENT_RUNTIME));
        }

        private void VerifyFilePath(string path)
        {
            Assert.That(File.Exists(path), $"File not found at {path}");
        }
    }
}
