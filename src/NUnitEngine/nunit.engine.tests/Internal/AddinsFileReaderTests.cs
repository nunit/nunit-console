// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NUnit.Engine.Internal.Tests
{
    /// <summary>
    /// Tests the implementation of <see cref="AddinsFileReader"/>.
    /// </summary>
    [TestFixture]
    public class AddinsFileReaderTests
    {
        private readonly string[] content1 = new string[]
        {
            "# This line is a comment and is ignored. The next (blank) line is ignored as well.",
            "",
            "*.dll                   # include all dlls in the same directory",
            "addins/*.dll            # include all dlls in the addins directory too",
            "special/myassembly.dll  # include a specific dll in a special directory",
            "some/other/directory/  # process another directory, which may contain its own addins file",
            "# note that an absolute path is allowed, but is probably not a good idea in most cases",
            "c:\\windows\\absolute\\directory",
            "/unix/absolute/directory",
            "\\transform\\backslash\\to\\slash"
        };

        [Test]
        public void Inheritance()
        {
            Assert.That(typeof(IAddinsFileReader), Is.AssignableFrom(typeof(AddinsFileReader)));
        }

        [Test]
        public void Read_IFile_Null()
        {
            var reader = new AddinsFileReader();

            Assert.That(() => reader.Read((IFile)null), Throws.ArgumentNullException);
        }

        [Test]
        public void Read_Stream()
        {
            var input = string.Join(Environment.NewLine, content1);
            var reader = new AddinsFileReader();
            IEnumerable<string> result;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                // Act
                result = reader.Read(stream);
            }

            Assert.That(result, Has.Count.EqualTo(7));
            Assert.That(result, Contains.Item("*.dll"));
            Assert.That(result, Contains.Item("addins/*.dll"));
            Assert.That(result, Contains.Item("special/myassembly.dll"));
            Assert.That(result, Contains.Item("some/other/directory/"));
            Assert.That(result, Contains.Item("c:/windows/absolute/directory"));
            Assert.That(result, Contains.Item("/unix/absolute/directory"));
            Assert.That(result, Contains.Item("/transform/backslash/to/slash"));
        }
    }
}
