// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    [NonParallelizable]
    class WorkingDirectoryTests
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

        [Test, Platform("Net")] //https://github.com/nunit/nunit-console/issues/946
        public void EngineCanBeCreatedFromAnyWorkingDirectory()
        {
            Assert.That(() =>
            {
                var engine = TestEngineActivator.CreateInstance();
                engine.Dispose();
            }, Throws.Nothing);
        }
    }
}
