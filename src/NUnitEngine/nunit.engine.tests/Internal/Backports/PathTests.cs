using NUnit.Framework;

namespace NUnit.Engine.Internal.Backports.Tests
{
    [TestFixture]
    public sealed class PathTests
    {
        [Platform("win")]
        [TestCase("c:\\foo\\bar\\", ExpectedResult = true)]
        [TestCase("c:/foo/bar/", ExpectedResult = true)]
        [TestCase("c:\\foo\\bar", ExpectedResult = true)]
        [TestCase("c:/foo/bar", ExpectedResult = true)]
        [TestCase("c:bar\\", ExpectedResult = false)]
        [TestCase("c:bar/", ExpectedResult = false)]
        [TestCase("c:bar", ExpectedResult = false)]
        [TestCase("ä:\\bar", ExpectedResult = false)]
        [TestCase("ä://bar", ExpectedResult = false)]
        [TestCase("\\\\server01\\foo", ExpectedResult = true)]
        [TestCase("\\server01\\foo", ExpectedResult = false)]
        [TestCase("c:", ExpectedResult = false)]
        [TestCase("/foo/bar", ExpectedResult = false)]
        [TestCase("/", ExpectedResult = false)]
        [TestCase("\\a\\b", ExpectedResult = false)]
        public bool IsPathFullyQualified_Windows(string path)
        {
            return Path.IsPathFullyQualified(path);
        }

        [Platform("linux")]
        [Platform("macos")]
        [Platform("unix")]
        [TestCase("/foo/bar", ExpectedResult = true)]
        [TestCase("/", ExpectedResult = true)]
        [TestCase("/z", ExpectedResult = true)]
        [TestCase("c:\\foo\\bar\\", ExpectedResult = false)]
        [TestCase("c:/foo/bar/", ExpectedResult = false)]
        [TestCase("c:\\foo\\bar", ExpectedResult = false)]
        [TestCase("c:/foo/bar", ExpectedResult = false)]
        [TestCase("c:bar\\", ExpectedResult = false)]
        [TestCase("c:bar/", ExpectedResult = false)]
        [TestCase("c:bar", ExpectedResult = false)]
        [TestCase("ä:\\bar", ExpectedResult = false)]
        [TestCase("ä://bar", ExpectedResult = false)]
        [TestCase("\\\\server01\\foo", ExpectedResult = false)]
        [TestCase("\\server01\\foo", ExpectedResult = false)]
        [TestCase("c:", ExpectedResult = false)]
        [TestCase("\\a\\b", ExpectedResult = false)]
        public bool IsPathFullyQualified_NonWindows(string path)
        {
            return Path.IsPathFullyQualified(path);
        }

        [Test]
        public void IsPathFullyQualified_PathIsNull()
        {
            Assert.That(() => Path.IsPathFullyQualified(null), Throws.ArgumentNullException);
        }
    }
}
