// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
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

namespace NUnit.Engine.Internal.Tests
{
    using NSubstitute;
    using NUnit.Engine.Internal.FileSystemAccess;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using SIO = System.IO;

    public class DirectoryFinderTests
    {
        private Dictionary<string, IDirectory> fakedDirectories;
        
        private IFileSystem fileSystem;

        [SetUp]
        public void CreateFileSystem()
        {
            var directories = new[]
            {
                "tools/frobuscator/tests/abc",
                "tools/frobuscator/tests/def",
                "tools/metamorphosator/addins/empty",
                "tools/metamorphosator/addins/morph",
                "tools/metamorphosator/tests/v1",
                "tools/metamorphosator/tests/v1/tmp",
                "tools/metamorphosator/tests/v2"
            };

            this.fakedDirectories = new Dictionary<string, IDirectory>();
            this.fileSystem = Substitute.For<IFileSystem>();

            // Create fakes and configure their properties.
            this.fakedDirectories[GetRoot()] = Substitute.For<IDirectory>();
            this.fakedDirectories[GetRoot()].FullName.Returns(GetRoot());
            foreach (var path in directories)
            {
                var parts = path.Split('/');
                for (var i = 0; i < parts.Length; i++)
                {
                    var absolutePath = CreateAbsolutePath(parts.Take(i+1));
                    if (!fakedDirectories.ContainsKey(absolutePath))
                    {
                        var fake = Substitute.For<IDirectory>();
                        fake.FullName.Returns(absolutePath);
                        fake.Parent.Returns(i == 0 ? fakedDirectories[GetRoot()] : fakedDirectories[CreateAbsolutePath(parts.Take(i))]);
                        fakedDirectories.Add(absolutePath, fake);
                        this.fileSystem.GetDirectory(absolutePath).Returns(fake);
                    }
                }
            }

            foreach (var fake in this.fakedDirectories.Values)
            {
                fake.GetDirectories("*", SIO.SearchOption.AllDirectories).Returns(this.fakedDirectories.Where(kvp => kvp.Key.StartsWith(fake.FullName + SIO.Path.DirectorySeparatorChar)).Select(kvp => kvp.Value));
                fake.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly)
                    .Returns(this.fakedDirectories
                                    .Where(kvp => kvp.Key.StartsWith(fake.FullName + SIO.Path.DirectorySeparatorChar) && kvp.Key.LastIndexOf(SIO.Path.DirectorySeparatorChar) <= fake.FullName.Length)
                                    .Select(kvp => kvp.Value));
                // NSubstitute will automatically return empty enumerables for calls that were not set up.
            }
        }

        [Test]
        public void GetDirectories_Asterisk_Tools()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            var expected = new[] { CombinePath(baseDir.FullName, "metamorphosator"), CombinePath(baseDir.FullName, "frobuscator") };

            var result = finder.GetDirectories(baseDir, "*");
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("*", SIO.SearchOption.TopDirectoryOnly);
            this.GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "metamorphosator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetDirectories_Asterisk_Metamorphosator()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator");
            var expected = new[] { CombinePath(baseDir.FullName, "addins"), CombinePath(baseDir.FullName, "tests") };

            var result = finder.GetDirectories(baseDir, "*");
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "metamorphosator", "addins").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "metamorphosator", "tests").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetDirectories_Greedy_Tools()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            var expected = this.fakedDirectories.Values.Select(x => x.FullName).Where(x => x != GetRoot());

            var result = finder.GetDirectories(baseDir, "**");
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            this.GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "metamorphosator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetDirectories_Greedy_Metamorphosator()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator");
            var expected = new[] { CombinePath(baseDir.FullName),
                                          CombinePath(baseDir.FullName, "addins"),
                                          CombinePath(baseDir.FullName, "tests"),
                                          CombinePath(baseDir.FullName, "tests", "v1"),
                                          CombinePath(baseDir.FullName, "tests", "v1", "tmp"),
                                          CombinePath(baseDir.FullName, "tests", "v2"),
                                          CombinePath(baseDir.FullName, "addins", "morph"),
                                          CombinePath(baseDir.FullName, "addins", "empty") };

            var result = finder.GetDirectories(baseDir, "**");
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            this.GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [TestCase("*ddi*")]
        [TestCase("addi*")]
        [TestCase("addins")]
        public void GetDirectories_WordWithWildcard_NoMatch(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");

            var result = finder.GetDirectories(baseDir, pattern);

            Assert.That(result, Is.Empty);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories(pattern, SIO.SearchOption.TopDirectoryOnly);
        }

        [TestCase("*din*")]
        [TestCase("addi*")]
        [TestCase("addins")]
        [TestCase("a?dins")]
        [TestCase("a?din?")]
        public void GetDirectories_WordWithWildcard_OneMatch(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator");
            baseDir.GetDirectories(pattern, SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "addins") });
            var expected = new[] { CombinePath(baseDir.FullName, "addins") };

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories(pattern, SIO.SearchOption.TopDirectoryOnly);
        }

        [TestCase("v*")]
        [TestCase("*")]
        [TestCase("v?")]
        public void GetDirectories_WordWithWildcard_MultipleMatches(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator", "tests");
            baseDir.GetDirectories(pattern, SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            var expected = new[] { CombinePath(baseDir.FullName, "v1"), CombinePath(baseDir.FullName, "v2") };

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories(pattern, SIO.SearchOption.TopDirectoryOnly);
        }

        [TestCase("tests/v*")]
        [TestCase("tests/*")]
        [TestCase("tests/v?")]
        [TestCase("*/v*")]
        [TestCase("*/v?")]
        [TestCase("**/v*")]
        [TestCase("**/v?")]
        [TestCase("te*/v*")]
        [TestCase("te*/*")]
        [TestCase("te*/v?")]
        [TestCase("t?sts/v*")]
        [TestCase("t?sts/*")]
        [TestCase("t?sts/v?")]
        [TestCase("./tests/v*")]
        [TestCase("./tests/*")]
        [TestCase("./tests/v?")]
        [TestCase("./*/v*")]
        [TestCase("./*/v?")]
        [TestCase("./**/v*")]
        [TestCase("./**/v?")]
        [TestCase("./te*/v*")]
        [TestCase("./te*/*")]
        [TestCase("./te*/v?")]
        [TestCase("./t?sts/v*")]
        [TestCase("./t?sts/*")]
        [TestCase("./t?sts/v?")]
        [TestCase("**/tests/v*")]
        [TestCase("**/tests/*")]
        [TestCase("**/tests/v?")]
        [TestCase("**/*/v*")]
        [TestCase("**/*/v?")]
        [TestCase("**/te*/v*")]
        [TestCase("**/te*/*")]
        [TestCase("**/te*/v?")]
        [TestCase("**/t?sts/v*")]
        [TestCase("**/t?sts/*")]
        [TestCase("**/t?sts/v?")]
        public void GetDirectories_MultipleComponents_MultipleMatches(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator");
            var testsDir = this.GetFakeDirectory("tools", "metamorphosator", "tests");
            baseDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir });
            baseDir.GetDirectories("te*", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir });
            baseDir.GetDirectories("t?sts", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir });
            testsDir.GetDirectories("v*", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            testsDir.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            testsDir.GetDirectories("v?", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            var expected = new[] { CombinePath(baseDir.FullName, "tests", "v1"), CombinePath(baseDir.FullName, "tests", "v2") };

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
        }

        [TestCase("*/tests/v*")]
        [TestCase("**/tests/v*")]
        public void GetDirectories_MultipleComponents_MultipleMatches_Asterisk(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            var frobuscatorDir = this.GetFakeDirectory("tools", "frobuscator");
            var metamorphosatorDir = this.GetFakeDirectory("tools", "metamorphosator");
            var testsDir = this.GetFakeDirectory("tools", "frobuscator", "tests");
            var testsDir2 = this.GetFakeDirectory("tools", "metamorphosator", "tests");
            frobuscatorDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir });
            metamorphosatorDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir2 });
            testsDir2.GetDirectories("v*", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            testsDir2.GetDirectories("v?", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            var expected = new[] { CombinePath(baseDir.FullName, "metamorphosator", "tests", "v1"), CombinePath(baseDir.FullName, "metamorphosator", "tests", "v2") };

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            testsDir.Received().GetDirectories("v*", SIO.SearchOption.TopDirectoryOnly);
        }

        [TestCase("*/tests/v?")]
        [TestCase("**/tests/v?")]
        public void GetDirectories_MultipleComponents_MultipleMatches_QuestionMark(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            var frobuscatorDir = this.GetFakeDirectory("tools", "frobuscator");
            var metamorphosatorDir = this.GetFakeDirectory("tools", "metamorphosator");
            var testsDir = this.GetFakeDirectory("tools", "frobuscator", "tests");
            var testsDir2 = this.GetFakeDirectory("tools", "metamorphosator", "tests");
            frobuscatorDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir });
            metamorphosatorDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { testsDir2 });
            testsDir2.GetDirectories("v*", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            testsDir2.GetDirectories("v?", SIO.SearchOption.TopDirectoryOnly).Returns(new IDirectory[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") });
            var expected = new[] { CombinePath(baseDir.FullName, "metamorphosator", "tests", "v1"), CombinePath(baseDir.FullName, "metamorphosator", "tests", "v2") };

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            testsDir.Received().GetDirectories("v?", SIO.SearchOption.TopDirectoryOnly);
        }

        [TestCase("./**/*")]
        [TestCase("**/*")]
        public void GetDirectories_MultipleComponents_AllDirectories(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.fakedDirectories[GetRoot()];
            var expected = this.fakedDirectories.Values.Select(x => x.FullName).Where(x => x != GetRoot());

            var result = finder.GetDirectories(baseDir, pattern);
            var actual = result.Select(x => x.FullName);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            foreach (var dir in this.fakedDirectories.Values.Where(x => x != baseDir))
            {
                dir.Received().GetDirectories("*", SIO.SearchOption.TopDirectoryOnly);
            }
        }

        [Test]
        public void GetDirectories_GreedyThenWordThenGreedy()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            this.GetFakeDirectory("tools", "frobuscator").GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "frobuscator", "tests") });
            this.GetFakeDirectory("tools", "metamorphosator").GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests") });
            var expected = new[] { this.GetFakeDirectory("tools", "frobuscator", "tests"),
                                   this.GetFakeDirectory("tools", "frobuscator", "tests", "abc"),
                                   this.GetFakeDirectory("tools", "frobuscator", "tests", "def"),
                                   this.GetFakeDirectory("tools", "metamorphosator", "tests"),
                                   this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"),
                                   this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1", "tmp"),
                                   this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2")};

            var actual = finder.GetDirectories(baseDir, "**/tests/**");

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            this.GetFakeDirectory("tools", "frobuscator").Received().GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly);
            this.GetFakeDirectory("tools", "metamorphosator").Received().GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly);
            this.GetFakeDirectory("tools", "frobuscator", "tests").Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            this.GetFakeDirectory("tools", "metamorphosator", "tests").Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
        }

        [Test]
        public void GetDirectories_WordWithAsteriskThenGreedyThenWord()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            baseDir.GetDirectories("meta*", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator") });
            this.GetFakeDirectory("tools", "metamorphosator", "tests").GetDirectories("v1", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1") });
            this.GetFakeDirectory("tools", "metamorphosator").GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests") });
            var expected = new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1") };

            var actual = finder.GetDirectories(baseDir, "meta*/**/v1");

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Received().GetDirectories("meta*", SIO.SearchOption.TopDirectoryOnly);
            this.GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            this.GetFakeDirectory("tools", "metamorphosator").Received().GetDirectories("*", SIO.SearchOption.AllDirectories);
            this.GetFakeDirectory("tools", "metamorphosator", "tests").Received().GetDirectories("v1", SIO.SearchOption.TopDirectoryOnly);
        }

        [Test]
        public void GetDirectories_Parent()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "frobuscator");
            var expected = new[] { this.GetFakeDirectory("tools") };

            var actual = finder.GetDirectories(baseDir, "../");

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetDirectories_ParentThenParentThenWordThenWord()
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "frobuscator", "tests");
            this.GetFakeDirectory("tools").GetDirectories("metamorphosator", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator") });
            this.GetFakeDirectory("tools", "metamorphosator").GetDirectories("addins", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator", "addins") });
            var expected = new[] { this.GetFakeDirectory("tools", "metamorphosator", "addins") };

            var actual = finder.GetDirectories(baseDir, "../../metamorphosator/addins");

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            baseDir.Parent.Parent.Received().GetDirectories("metamorphosator", SIO.SearchOption.TopDirectoryOnly);
            this.GetFakeDirectory("tools", "metamorphosator").Received().GetDirectories("addins", SIO.SearchOption.TopDirectoryOnly);
        }

        [Test]
        public void GetDirectories_StartDirectoryIsNull()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetDirectories((IDirectory)null, "notused"), Throws.ArgumentNullException.With.Message.Contains(" startDirectory "));
        }

        [Test]
        public void GetDirectories_PatternIsNull()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetDirectories(Substitute.For<IDirectory>(), null), Throws.ArgumentNullException.With.Message.Contains(" pattern "));
        }

        [Test]
        public void GetDirectories_PatternIsEmpty()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetDirectories(Substitute.For<IDirectory>(), string.Empty), Throws.ArgumentException.With.Message.Contains(" pattern "));
        }

        ////[TestCase("net-4.0/nunit.framework.dll", 1)]
        ////[TestCase("net-*/nunit.framework.dll", 4)]
        ////[TestCase("net-*/*.framework.dll", 4)]
        ////[TestCase("*/v2-tests/*.dll", 2)]
        ////[TestCase("add*/v?-*/*.dll", 2)]
        ////[TestCase("**/v2-tests/*.dll", 2)]
        ////[TestCase("addins/**/*.dll", 10)]
        ////[TestCase("addins/../net-*/nunit.framework.dll", 4)]
        //public void GetFiles(string pattern, int count)
        //{
        //    var finder = new DirectoryFinder();
        //    var files = finder.GetFiles(_baseDir, pattern);
        //    Assert.That(files.Count, Is.EqualTo(count));
        //}

        private static string CreateAbsolutePath(IEnumerable<string> parts)
        {
            return CreateAbsolutePath(parts.ToArray());
        }

        private static string CreateAbsolutePath(params string[] parts)
        {
            string relativePath = CombinePath(parts);
            return SIO.Path.DirectorySeparatorChar == '\\' ? "c:\\" + relativePath : "/" + relativePath;
        }

        private IDirectory GetFakeDirectory(params string[] parts)
        {
            return this.fakedDirectories[CreateAbsolutePath(parts)];
        }

        private static string CombinePath(params string[] parts)
        {
            var path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                path = SIO.Path.Combine(path, parts[i]);
            }
            return path;
        }

        private static string GetRoot()
        {
            if (SIO.Path.DirectorySeparatorChar == '\\')
            {
                return "c:";
            }
            else
            {
                return "/";
            }
        }
    }
}
