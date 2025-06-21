﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine
{
    [NonParallelizable]
    internal class WorkingDirectoryTests
    {
        private string _origWorkingDir;

        [OneTimeSetUp]
        public void SetWorkingDirToTempDir()
        {
            _origWorkingDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetTempPath());
        }

        [OneTimeTearDown]
        public void ResetWorkingDir()
        {
            Directory.SetCurrentDirectory(_origWorkingDir);
        }

        [Test]
        public void EngineCanBeCreatedFromAnyWorkingDirectory()
        {
            Assert.That(() => new TestEngine(), Throws.Nothing);
        }
    }
}
