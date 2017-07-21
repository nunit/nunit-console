// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************
#if !PORTABLE
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace NUnit.Common.Tests
{
    public class OutputSpecificationTests
    {
        [Test]
        public void SpecMayNotBeNull()
        {
            Assert.That(
                () => new OutputSpecification(null),
                Throws.TypeOf<ArgumentNullException>());
        }


        [Test]
        public void SpecOptionMustContainEqualSign()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void SpecOptionMustContainJustOneEqualSign()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform=xslt=transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void FileNameOnly()
        {
            var spec = new OutputSpecification("MyFile.xml");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FileNamePlusFormat()
        {
            var spec = new OutputSpecification("MyFile.xml;format=nunit2");
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit2"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FolderMustBeSpecifiedForTransform()
        {
            Assert.That(() =>
                new OutputSpecification("MyFile.xml;transform=transform.xslt"),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void FolderMustNotBeNullForTransform()
        {
            Assert.That(() =>
                new OutputSpecification("MyFile.xml;transform=transform.xslt", null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void FileNamePlusTransform()
        {
            const string fileName = "transform.xslt";
            IFileSystem fileSystem = new SimpleVirtualFileSystem(".", fileName);
            var spec = new OutputSpecification($"MyFile.xml;transform={fileName}", ".", fileSystem);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyAfterTransform()
        {
            const string fileName = "transform.xslt";
            IFileSystem fileSystem = new SimpleVirtualFileSystem(".", fileName);
            var spec = new OutputSpecification($"MyFile.xml;transform={fileName};format=user", ".", fileSystem);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyBeforeTransform()
        {
            const string fileName = "transform.xslt";
            IFileSystem fileSystem = new SimpleVirtualFileSystem(".", fileName);
            var spec = new OutputSpecification($"MyFile.xml;format=user;transform={fileName}", ".", fileSystem);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void MultipleFormatSpecifiersNotAllowed()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;format=nunit2;format=nunit3"),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void MultipleTransformSpecifiersNotAllowed()
        {
            IFileSystem fileSystem = new SimpleVirtualFileSystem(".", new List<string> { "transform1.xslt", "transform2.xslt" });
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform=transform1.xslt;transform=transform2.xslt", ".", fileSystem),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TransformWithNonUserFormatNotAllowed()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;format=nunit2;transform=transform.xslt"),
                Throws.TypeOf<ArgumentException>());
        }

        class SimpleVirtualFileSystem : IFileSystem
        {
            private readonly List<string> _fullFileNames;

            public SimpleVirtualFileSystem(string folder, string fileName)
                : this(folder, new List<string> { fileName })
            {
            }

            public SimpleVirtualFileSystem(string folder, List<string> fileNames)
            {
                _fullFileNames = new List<string>(fileNames.Count);
                foreach (var fileName in fileNames)
                    _fullFileNames.Add(Path.Combine(folder, fileName));
            }

            public bool FileExists(string fullFileName)
            {
                return _fullFileNames.Contains(fullFileName);
            }

            public IEnumerable<string> ReadLines(string fileName)
            {
                throw new NotImplementedException();
            }
        }
    }
}
#endif