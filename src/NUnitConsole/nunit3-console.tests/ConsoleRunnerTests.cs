using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    class ConsoleRunnerTests
    {
        [Test]
        public void ThrowsExceptionWhenWorkingDirectoryIsUnAuthorized()
        {
            var tempDirPath = Directory.GetCurrentDirectory() + "\\" + Path.GetRandomFileName();
            var directory = Directory.CreateDirectory(tempDirPath);

            try
            {
                var directorySecurity = new DirectorySecurity();
                directorySecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.Write, AccessControlType.Deny));
                directorySecurity.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.DeleteSubdirectoriesAndFiles, AccessControlType.Allow));
                Directory.SetAccessControl(tempDirPath, directorySecurity);

                var consoleRunner = new ConsoleRunner(
                    TestEngineActivator.CreateInstance(), 
                    new ConsoleOptions("tests.dll", "-result=results.xml", 
                        string.Format(@"--work={0}", directory.FullName)), new ColorConsoleWriter());

                var exception = Assert.Throws<UnauthorizedAccessException>(() => { consoleRunner.Execute(); });
                Assert.That(exception.Message, Is.EqualTo("Error: Unauthorized working directory"));
            }
            finally
            {
                directory.Delete(true);
            }
        }
    }
}
