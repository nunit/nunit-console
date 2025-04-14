// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.FileSystemAccess;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace NUnit.Extensibility
{
    /// <summary>
    /// Tests the implementation of <see cref="AddinsFile"/>.
    /// </summary>
    [TestFixture]
    public class AddinsFileTests
    {
        [Test]
        public void Read_IFile_Null()
        {
            Assert.That(() => AddinsFile.Read((IFile)null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Read_Stream()
        {
            var content = new[]
            {
                "# This line is a comment and is ignored. The next (blank) line is ignored as well.",
                string.Empty,
                "*.dll                   # include all dlls in the same directory",
                "addins/*.dll            # include all dlls in the addins directory too",
                "special/myassembly.dll  # include a specific dll in a special directory",
                "some/other/directory/  # process another directory, which may contain its own addins file",
                "# note that an absolute path is allowed, but is probably not a good idea in most cases",
                "/unix/absolute/directory"
            };

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, content))))
            {
                var result = AddinsFile.Read(stream);

                Assert.That(result, Has.Count.EqualTo(8));
                for (int i = 0; i < 8; i++)
                    Assert.That(result[i], Is.EqualTo(
                        new AddinsFileEntry(i + 1, content[i])));
            }
        }

        [Test]
        public void Read_InvalidEntry()
        {
            var content = "// This is not valid";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                Assert.That(() => AddinsFile.Read(stream), Throws.Exception);
            }
        }

        [Test]
        [Platform("win")]
        public void Read_Stream_TransformBackslash_Windows()
        {
            var content = "c:\\windows\\absolute\\directory";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var result = AddinsFile.Read(stream);

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0], Is.EqualTo(new AddinsFileEntry(1, content)));
                Assert.That(result[0].Text, Is.EqualTo("c:/windows/absolute/directory"));
            }
        }

        [Test]
        [Platform("linux,macosx,unix")]
        public void Read_Stream_TransformBackslash_NonWindows()
        {
            var content = "this/is/a\\ path\\ with\\ spaces/";

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var result = AddinsFile.Read(stream);

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[0], Is.EqualTo(new AddinsFileEntry(1, content)));
                Assert.That(result[0].Text, Is.EqualTo(content));
            }
        }
    }
}
