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

        [Test]
        public void EngineCanBeCreatedFromAnyWorkingDirectory()
        {
            Assert.That(() => TestEngineActivator.CreateInstance(), Throws.Nothing);
        }
    }
}
