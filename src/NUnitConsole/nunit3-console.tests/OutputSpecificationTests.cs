// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt
using System;
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
                () => new OutputSpecification(null, null),
                Throws.TypeOf<ArgumentNullException>());
        }


        [Test]
        public void SpecOptionMustContainEqualSign()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform.xslt", null),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void SpecOptionMustContainJustOneEqualSign()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform=xslt=transform.xslt", null),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void FileNameOnly()
        {
            var spec = new OutputSpecification("MyFile.xml", null);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FileNamePlusFormat()
        {
            var spec = new OutputSpecification("MyFile.xml;format=nunit2", null);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit2"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void FileNamePlusTransform()
        {
            const string fileName = "transform.xslt";
            var spec = new OutputSpecification($"MyFile.xml;transform={fileName}", null);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyAfterTransform()
        {
            const string fileName = "transform.xslt";
            var spec = new OutputSpecification($"MyFile.xml;transform={fileName};format=user", null);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void UserFormatMayBeIndicatedExplicitlyBeforeTransform()
        {
            const string fileName = "transform.xslt";
            var spec = new OutputSpecification($"MyFile.xml;format=user;transform={fileName}", null);
            Assert.That(spec.OutputPath, Is.EqualTo("MyFile.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            Assert.That(spec.Transform, Is.EqualTo(fileName));
        }

        [Test]
        public void MultipleFormatSpecifiersNotAllowed()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;format=nunit2;format=nunit3", null),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void MultipleTransformSpecifiersNotAllowed()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;transform=transform1.xslt;transform=transform2.xslt", null),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TransformWithNonUserFormatNotAllowed()
        {
            Assert.That(
                () => new OutputSpecification("MyFile.xml;format=nunit2;transform=transform.xslt", null),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(@"C:\")]
        [TestCase(@"C:\Temp")]

        public void TransformFolderIsUsedToSpecifyTransform(string transformFolder)
        {
            const string fileName = "transform.xslt";
            var spec = new OutputSpecification($"MyFile.xml;transform=transform.xslt", transformFolder);
            var expectedTransform = Path.Combine(transformFolder ?? "", fileName);
            Assert.That(spec.Transform, Is.EqualTo(expectedTransform));
        }
    }
}