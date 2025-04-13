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
#if NET8_0
        const string CURRENT_RUNTIME = "net8.0";
#else
        const string CURRENT_RUNTIME = "net462";
#endif
        [Test]
        public void SelfTest()
        {
            VerifyFilePath("testdata/" + CURRENT_RUNTIME + "/mock-assembly.dll");
            VerifyFilePath("testdata/" + CURRENT_RUNTIME + "/notest-assembly.dll");
        }

        private static void VerifyFilePath(string path)
        {
            path = Path.GetFullPath(path);
            Assert.That(File.Exists(path), $"File not found at {path}");
        }
    }
}
