// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NSubstitute;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Internal
{
    /// <summary>
    /// Tests the implementation of <see cref="AddinsFileReader"/>.
    /// </summary>
    /// <remarks>All tests in this fixture modify the file-system.</remarks>
    [TestFixture, Category("WritesToDisk"), NonParallelizable]
    public class AddinsFileReaderTests2
    {
        private readonly string tempFileLocation;

        public AddinsFileReaderTests2()
        {
            string[] content = new string[]
            {
                        "# This line is a comment and is ignored. The next (blank) line is ignored as well.",
                        "",
                        "*.dll                   # include all dlls in the same directory",
                        "addins/*.dll            # include all dlls in the addins directory too",
                        "special/myassembly.dll  # include a specific dll in a special directory",
                        "some/other/directory/  # process another directory, which may contain its own addins file",
                        "# note that an absolute path is allowed, but is probably not a good idea in most cases",
                        "/unix/absolute/directory"
            };

            this.tempFileLocation = Path.GetTempFileName();

            using (var writer = new StreamWriter(this.tempFileLocation))
            {
                foreach (var line in content)
                {
                    writer.WriteLine(line);
                }
            }
        }

        [TearDown]
        public void DeleteTestFile()
        {
            File.Delete(this.tempFileLocation);
        }

        [Test]
        public void Read_IFile()
        {
            var reader = new AddinsFileReader();
            var file = Substitute.For<IFile>();
            file.FullName.Returns(this.tempFileLocation);

            var result = reader.Read(file);
            
            Assert.That(result, Has.Count.EqualTo(5));
            Assert.That(result, Contains.Item("*.dll"));
            Assert.That(result, Contains.Item("addins/*.dll"));
            Assert.That(result, Contains.Item("special/myassembly.dll"));
            Assert.That(result, Contains.Item("some/other/directory/"));
            Assert.That(result, Contains.Item("/unix/absolute/directory"));
        }
    }
}
