// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace NUnit.Common
{
    [TestFixture]
    public class PathUtilsTests
    {
        [TestCase("c:\\", ExpectedResult = true)]
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
        public bool IsFullyQualifiedWindowsPath(string path)
        {
            return PathUtils.IsFullyQualifiedWindowsPath(path);
        }

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
        public bool IsFullyQualifiedUnixPath(string path)
        {
            return PathUtils.IsFullyQualifiedUnixPath(path);
        }

        [Test]
        public void IsFullyQualifiedUnixPath_PathIsNull()
        {
            Assert.That(() => PathUtils.IsFullyQualifiedUnixPath(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void IsFullyQualifiedWindowsPath_PathIsNull()
        {
            Assert.That(() => PathUtils.IsFullyQualifiedWindowsPath(null!), Throws.ArgumentNullException);
        }
    }

    [TestFixture]
    public class PathUtilDefaultsTests : PathUtils
    {
        [Test]
        public void CheckDefaults()
        {
            Assert.That(PathUtils.DirectorySeparatorChar, Is.EqualTo(Path.DirectorySeparatorChar));
            Assert.That(PathUtils.AltDirectorySeparatorChar, Is.EqualTo(Path.AltDirectorySeparatorChar));
        }
    }

    // Local Assert extension
    internal class PathAssert : NUnit.Framework.Assert
    {
        public static void SamePathOrUnder(string path1, string path2)
        {
            Assert.That(PathUtils.SamePathOrUnder(path1, path2), Is.True, $"\r\n\texpected: Same path or under <{path1}>\r\n\t but was: <{path2}>");
        }

        public static void NotSamePathOrUnder(string path1, string path2)
        {
            Assert.That(PathUtils.SamePathOrUnder(path1, path2), Is.False, $"\r\n\texpected: Not same path or under <{path1}>\r\n\t but was: <{path2}>");
        }
    }

    [TestFixture]
    public class PathUtilTests_Windows : PathUtils
    {
        [OneTimeSetUp]
        public static void SetUpUnixSeparators()
        {
            PathUtils.DirectorySeparatorChar = '\\';
            PathUtils.AltDirectorySeparatorChar = '/';
        }

        [OneTimeTearDown]
        public static void RestoreDefaultSeparators()
        {
            PathUtils.DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
            PathUtils.AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar;
        }

        [Test]
        public void Canonicalize()
        {
            Assert.That(PathUtils.Canonicalize(@"C:\folder1\.\folder2\..\file.tmp"), Is.EqualTo(@"C:\folder1\file.tmp"));
            Assert.That(PathUtils.Canonicalize(@"folder1\.\folder2\..\file.tmp"), Is.EqualTo(@"folder1\file.tmp"));
            Assert.That(PathUtils.Canonicalize(@"folder1\folder2\.\..\file.tmp"), Is.EqualTo(@"folder1\file.tmp"));
            Assert.That(PathUtils.Canonicalize(@"folder1\folder2\..\.\..\file.tmp"), Is.EqualTo(@"file.tmp"));
            Assert.That(PathUtils.Canonicalize(@"folder1\folder2\..\..\..\file.tmp"), Is.EqualTo(@"file.tmp"));
        }

        [Test]
        public void RelativePath()
        {
            bool windows = false;

#if NETCOREAPP
            windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            var platform = Environment.OSVersion.Platform;
            windows = platform == PlatformID.Win32NT;
#endif

            Assume.That(windows, Is.True);

            Assert.That(PathUtils.RelativePath(
                @"c:\folder1", @"c:\folder1\folder2\folder3"), Is.EqualTo(@"folder2\folder3"));
            Assert.That(PathUtils.RelativePath(
                @"c:\folder1", @"c:\folder2\folder3"), Is.EqualTo(@"..\folder2\folder3"));
            Assert.That(PathUtils.RelativePath(
                @"c:\folder1", @"bin\debug"), Is.EqualTo(@"bin\debug"));
            Assert.That(PathUtils.RelativePath(@"C:\folder", @"D:\folder"), Is.Null,
                "Unrelated paths should return null");
            Assert.That(PathUtils.RelativePath(@"C:\", @"D:\"), Is.Null,
                "Unrelated roots should return null");
            Assert.That(PathUtils.RelativePath(@"C:", @"D:"), Is.Null,
                "Unrelated roots (no trailing separators) should return null");
            Assert.That(PathUtils.RelativePath(@"C:\folder1", @"C:\folder1"), Is.EqualTo(string.Empty));
            Assert.That(PathUtils.RelativePath(@"C:\", @"C:\"), Is.EqualTo(string.Empty));

            // First filePath consisting just of a root:
            Assert.That(PathUtils.RelativePath(
                @"C:\", @"C:\folder1\folder2"), Is.EqualTo(@"folder1\folder2"));

            // Trailing directory separator in first filePath shall be ignored:
            Assert.That(PathUtils.RelativePath(
                @"c:\folder1\", @"c:\folder1\folder2\folder3"), Is.EqualTo(@"folder2\folder3"));

            // Case-insensitive behavior, preserving 2nd filePath directories in result:
            Assert.That(PathUtils.RelativePath(
                @"C:\folder1", @"c:\folder1\Folder2\Folder3"), Is.EqualTo(@"Folder2\Folder3"));
            Assert.That(PathUtils.RelativePath(
                @"c:\folder1", @"C:\Folder2\folder3"), Is.EqualTo(@"..\Folder2\folder3"));
        }

        [Test]
        public void SamePathOrUnder()
        {
            PathAssert.SamePathOrUnder(@"C:\folder1\folder2\folder3", @"C:\folder1\.\folder2\junk\..\folder3");
            PathAssert.SamePathOrUnder(@"C:\folder1\folder2\", @"C:\folder1\.\folder2\junk\..\folder3");
            PathAssert.SamePathOrUnder(@"C:\folder1\folder2", @"C:\folder1\.\folder2\junk\..\folder3");
            PathAssert.NotSamePathOrUnder(@"C:\folder1\folder2", @"C:\folder1\.\folder22\junk\..\folder3");
            PathAssert.NotSamePathOrUnder(@"C:\folder1\folder2ile.tmp", @"D:\folder1\.\folder2\folder3\file.tmp");
            PathAssert.NotSamePathOrUnder(@"C:\", @"D:\");
            PathAssert.SamePathOrUnder(@"C:\", @"C:\");
            PathAssert.SamePathOrUnder(@"C:\", @"C:\bin\debug");
        }
    }

    [TestFixture]
    public class PathUtilTests_Unix : PathUtils
    {
        [OneTimeSetUp]
        public static void SetUpUnixSeparators()
        {
            PathUtils.DirectorySeparatorChar = '/';
            PathUtils.AltDirectorySeparatorChar = '\\';
        }

        [OneTimeTearDown]
        public static void RestoreDefaultSeparators()
        {
            PathUtils.DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
            PathUtils.AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar;
        }

        [Test]
        public void Canonicalize()
        {
            Assert.That(PathUtils.Canonicalize("/folder1/./folder2/../file.tmp"), Is.EqualTo("/folder1/file.tmp"));
            Assert.That(PathUtils.Canonicalize("folder1/./folder2/../file.tmp"), Is.EqualTo("folder1/file.tmp"));
            Assert.That(PathUtils.Canonicalize("folder1/folder2/./../file.tmp"), Is.EqualTo("folder1/file.tmp"));
            Assert.That(PathUtils.Canonicalize("folder1/folder2/.././../file.tmp"), Is.EqualTo("file.tmp"));
            Assert.That(PathUtils.Canonicalize("folder1/folder2/../../../file.tmp"), Is.EqualTo("file.tmp"));
        }

        [Test]
        public void RelativePath()
        {
            Assert.That(PathUtils.RelativePath("/folder1", "/folder1/folder2/folder3"), Is.EqualTo("folder2/folder3"));
            Assert.That(PathUtils.RelativePath("/folder1", "/folder2/folder3"), Is.EqualTo("../folder2/folder3"));
            Assert.That(PathUtils.RelativePath("/folder1", "bin/debug"), Is.EqualTo("bin/debug"));
            Assert.That(PathUtils.RelativePath("/folder", "/other/folder"), Is.EqualTo("../other/folder"));
            Assert.That(PathUtils.RelativePath("/a/b/c", "/a/d"), Is.EqualTo("../../d"));
            Assert.That(PathUtils.RelativePath("/a/b", "/a/b"), Is.EqualTo(string.Empty));
            Assert.That(PathUtils.RelativePath("/", "/"), Is.EqualTo(string.Empty));

            // First filePath consisting just of a root:
            Assert.That(PathUtils.RelativePath(
                "/", "/folder1/folder2"), Is.EqualTo("folder1/folder2"));

            // Trailing directory separator in first filePath shall be ignored:
            Assert.That(PathUtils.RelativePath(
                "/folder1/", "/folder1/folder2/folder3"), Is.EqualTo("folder2/folder3"));

            // Case-sensitive behavior:
            Assert.That(PathUtils.RelativePath("/folder1", "/Folder1/Folder2/folder3"), Is.EqualTo("../Folder1/Folder2/folder3"), "folders differing in case");
        }

        [Test]
        public void SamePathOrUnder()
        {
            PathAssert.SamePathOrUnder("/folder1/folder2/folder3", "/folder1/./folder2/junk/../folder3");
            PathAssert.SamePathOrUnder("/folder1/folder2/", "/folder1/./folder2/junk/../folder3");
            PathAssert.SamePathOrUnder("/folder1/folder2", "/folder1/./folder2/junk/../folder3");
            PathAssert.NotSamePathOrUnder("/folder1/folder2", "/folder1/./Folder2/junk/../folder3");
            PathAssert.NotSamePathOrUnder("/folder1/folder2", "/folder1/./folder22/junk/../folder3");
            PathAssert.SamePathOrUnder("/", "/");
            PathAssert.SamePathOrUnder("/", "/bin/debug");
        }
    }
}
