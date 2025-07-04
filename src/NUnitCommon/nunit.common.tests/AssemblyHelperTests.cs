// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Internal
{
    [TestFixture]
    public class AssemblyHelperTests
    {
#if NETFRAMEWORK
        private static readonly string THIS_ASSEMBLY_FILENAME = "nunit.common.tests.exe";
#else
        private static readonly string THIS_ASSEMBLY_FILENAME = "nunit.common.tests.dll";
#endif

        [Test]
        public void GetPathForAssembly()
        {
            string path = AssemblyHelper.GetAssemblyPath(this.GetType().Assembly);
            Assert.That(Path.GetFileName(path), Is.EqualTo(THIS_ASSEMBLY_FILENAME).IgnoreCase);
            Assert.That(File.Exists(path));
        }

#if NETFRAMEWORK
        // The following tests are only useful to the extent that the test cases
        // match what will actually be provided to the method in production.
        // As currently used, NUnit's codebase can only use the file: schema,
        // since we don't load assemblies from anything but files. The uri's
        // provided can be absolute file paths or UNC paths.

        // Local paths - Windows Drive
        [TestCase(@"file:///C:/path/to/assembly.dll", @"C:\path\to\assembly.dll")]
        [TestCase(@"file:///C:/my path/to my/assembly.dll", @"C:/my path/to my/assembly.dll")]
        [TestCase(@"file:///C:/dev/C#/assembly.dll", @"C:\dev\C#\assembly.dll")]
        [TestCase(@"file:///C:/dev/funnychars?:=/assembly.dll", @"C:\dev\funnychars?:=\assembly.dll")]
        // Local paths - Linux or Windows absolute without a drive
        [TestCase(@"file:///path/to/assembly.dll", @"/path/to/assembly.dll")]
        [TestCase(@"file:///my path/to my/assembly.dll", @"/my path/to my/assembly.dll")]
        [TestCase(@"file:///dev/C#/assembly.dll", @"/dev/C#/assembly.dll")]
        [TestCase(@"file:///dev/funnychars?:=/assembly.dll", @"/dev/funnychars?:=/assembly.dll")]
        // Windows drive specified as if it were a server - odd case, sometimes seen
        [TestCase(@"file://C:/path/to/assembly.dll", @"C:\path\to\assembly.dll")]
        [TestCase(@"file://C:/my path/to my/assembly.dll", @"C:\my path\to my\assembly.dll")]
        [TestCase(@"file://C:/dev/C#/assembly.dll", @"C:\dev\C#\assembly.dll")]
        [TestCase(@"file://C:/dev/funnychars?:=/assembly.dll", @"C:\dev\funnychars?:=\assembly.dll")]
        // UNC format with server and path
        [TestCase(@"file://server/path/to/assembly.dll", @"//server/path/to/assembly.dll")]
        [TestCase(@"file://server/my path/to my/assembly.dll", @"//server/my path/to my/assembly.dll")]
        [TestCase(@"file://server/dev/C#/assembly.dll", @"//server/dev/C#/assembly.dll")]
        [TestCase(@"file://server/dev/funnychars?:=/assembly.dll", @"//server/dev/funnychars?:=/assembly.dll")]
        // [TestCase(@"http://server/path/to/assembly.dll", "//server/path/to/assembly.dll")]
        public void GetAssemblyPathFromCodeBase(string uri, string expectedPath)
        {
            string localPath = AssemblyHelper.GetAssemblyPathFromCodeBase(uri);
            Assert.That(localPath, Is.SamePath(expectedPath));
        }
#endif
    }
}
