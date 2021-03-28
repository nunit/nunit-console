// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Linq;
using SIO = System.IO;
using NUnit.Framework;
using NSubstitute;
using NUnit.Engine.Internal.FileSystemAccess;

namespace NUnit.Engine.Internal.Tests
{
    public class DirectoryFinderTests
    {
        private Dictionary<string, IDirectory> fakedDirectories;
        private Dictionary<string, IFile> fakedFiles;
        
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

            var files = new[]
            {
                "tools/frobuscator/tests/config.cfg",
                "tools/frobuscator/tests/abc/tests.abc.dll",
                "tools/frobuscator/tests/abc/tests.123.dll",
                "tools/frobuscator/tests/def/tests.def.dll",
                "tools/metamorphosator/addins/readme.txt",
                "tools/metamorphosator/addins/morph/setup.ini",
                "tools/metamorphosator/addins/morph/code.cs",
                "tools/metamorphosator/tests/v1/test-assembly.dll",
                "tools/metamorphosator/tests/v1/test-assembly.pdb",
                "tools/metamorphosator/tests/v1/result.xml",
                "tools/metamorphosator/tests/v1/tmp/tmp.dat",
                "tools/metamorphosator/tests/v2/test-assembly.dll",
                "tools/metamorphosator/tests/v2/test-assembly.pdb",
                "tools/metamorphosator/tests/v2/result.xml",
            };

            this.fileSystem = Substitute.For<IFileSystem>();
            this.fakedDirectories = new Dictionary<string, IDirectory>();

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

            foreach (var directory in this.fakedDirectories.Values)
            {
                directory.GetDirectories("*", SIO.SearchOption.AllDirectories).Returns(this.fakedDirectories.Where(kvp => kvp.Key.StartsWith(directory.FullName + SIO.Path.DirectorySeparatorChar)).Select(kvp => kvp.Value));
                directory.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly)
                    .Returns(this.fakedDirectories
                                    .Where(kvp => kvp.Key.StartsWith(directory.FullName + SIO.Path.DirectorySeparatorChar) && kvp.Key.LastIndexOf(SIO.Path.DirectorySeparatorChar) <= directory.FullName.Length)
                                    .Select(kvp => kvp.Value));
                // NSubstitute will automatically return empty enumerables for calls that were not set up.
            }

            this.fakedFiles = new Dictionary<string, IFile>();
            foreach (var filePath in files)
            {
                var fileName = filePath.Split('/').Reverse().Take(1);
                var directory = GetFakeDirectory(filePath.Split('/').Reverse().Skip(1).Reverse().ToArray());
                var file = Substitute.For<IFile>();
                file.FullName.Returns(CreateAbsolutePath(filePath.Split('/')));
                file.Parent.Returns(directory);
                this.fakedFiles.Add(file.FullName, file);
            }

            foreach (var directory in this.fakedDirectories.Values)
            {
                var directoryContent = this.fakedFiles.Values.Where(x => x.Parent == directory).ToArray();
                directory.GetFiles("*").Returns(directoryContent);
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
            var expected = new[]
            {
                CombinePath(baseDir.FullName),
                CombinePath(baseDir.FullName, "addins"),
                CombinePath(baseDir.FullName, "tests"),
                CombinePath(baseDir.FullName, "tests", "v1"),
                CombinePath(baseDir.FullName, "tests", "v1", "tmp"),
                CombinePath(baseDir.FullName, "tests", "v2"),
                CombinePath(baseDir.FullName, "addins", "morph"),
                CombinePath(baseDir.FullName, "addins", "empty")
            };

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
            var baseDirContent = new[] { testsDir };
            var testsDirContent = new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"), this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2") };
            baseDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(baseDirContent);
            baseDir.GetDirectories("te*", SIO.SearchOption.TopDirectoryOnly).Returns(baseDirContent);
            baseDir.GetDirectories("t?sts", SIO.SearchOption.TopDirectoryOnly).Returns(baseDirContent);
            testsDir.GetDirectories("v*", SIO.SearchOption.TopDirectoryOnly).Returns(testsDirContent);
            testsDir.GetDirectories("*", SIO.SearchOption.TopDirectoryOnly).Returns(testsDirContent);
            testsDir.GetDirectories("v?", SIO.SearchOption.TopDirectoryOnly).Returns(testsDirContent);
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
            var expected = this.fakedDirectories.Values.Where(x => x != baseDir);

            var actual = finder.GetDirectories(baseDir, pattern);

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
            var expected = new[]
            {
                this.GetFakeDirectory("tools", "frobuscator", "tests"),
                this.GetFakeDirectory("tools", "frobuscator", "tests", "abc"),
                this.GetFakeDirectory("tools", "frobuscator", "tests", "def"),
                this.GetFakeDirectory("tools", "metamorphosator", "tests"),
                this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1"),
                this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1", "tmp"),
                this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2")
            };

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
            var directory = Substitute.For<IDirectory>();
            var sut = new DirectoryFinder(Substitute.For<IFileSystem>());

            var directories = sut.GetDirectories(directory, string.Empty);

            Assert.That(directories, Has.Count.EqualTo(1));
            Assert.That(directories.First(), Is.EqualTo(directory));
        }

        [TestCase("tests.*.dll")]
        [TestCase("tests.???.dll")]
        [TestCase("t*.???.dll")]
        public void GetFiles_WordWithWildcard(string pattern)
        {
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "frobuscator", "tests", "abc");
            baseDir.GetFiles(pattern).Returns(new[] { this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll") });
            var expected = new[] { this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll") };

            var actual = finder.GetFiles(baseDir, pattern);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Received().GetFiles(pattern);
            baseDir.Parent.DidNotReceive().GetFiles(Arg.Any<string>());
        }

        [TestCase("*/tests.*.dll")]
        [TestCase("*/tests.???.dll")]
        [TestCase("*/t*.???.dll")]
        public void GetFiles_AsteriskThenWordWithWildcard(string pattern)
        {
            var filePattern = pattern.Split('/')[1];
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "frobuscator", "tests");
            var abcDir = this.GetFakeDirectory("tools", "frobuscator", "tests", "abc");
            var defDir = this.GetFakeDirectory("tools", "frobuscator", "tests", "def");
            abcDir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll") });
            defDir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "frobuscator", "tests", "def", "tests.def.dll") });
            var expected = new[] { this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "def", "tests.def.dll") };

            var actual = finder.GetFiles(baseDir, pattern);

            CollectionAssert.AreEquivalent(expected, actual);
            abcDir.Received().GetFiles(filePattern);
            defDir.Received().GetFiles(filePattern);
            baseDir.Parent.DidNotReceive().GetFiles(Arg.Any<string>());
            baseDir.DidNotReceive().GetFiles(Arg.Any<string>());
            abcDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            defDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [TestCase("**/test*.dll")]
        [TestCase("**/t*.???")]
        public void GetFiles_GreedyThenWordWithWildcard(string pattern)
        {
            var filePattern = pattern.Split('/')[1];
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools");
            var abcDir = this.GetFakeDirectory("tools", "frobuscator", "tests", "abc");
            var defDir = this.GetFakeDirectory("tools", "frobuscator", "tests", "def");
            var v1Dir = this.GetFakeDirectory("tools", "metamorphosator", "tests", "v1");
            var v2Dir = this.GetFakeDirectory("tools", "metamorphosator", "tests", "v2");
            abcDir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"), this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll") });
            defDir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "frobuscator", "tests", "def", "tests.def.dll") });
            v1Dir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "metamorphosator", "tests", "v1", "test-assembly.dll"), this.GetFakeFile("tools", "metamorphosator", "tests", "v1", "test-assembly.pdb") });
            v2Dir.GetFiles(filePattern).Returns(new[] { this.GetFakeFile("tools", "metamorphosator", "tests", "v2", "test-assembly.dll"), this.GetFakeFile("tools", "metamorphosator", "tests", "v2", "test-assembly.pdb") });
            var expected = new[]
            {
                this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.abc.dll"),
                this.GetFakeFile("tools", "frobuscator", "tests", "abc", "tests.123.dll"),
                this.GetFakeFile("tools", "frobuscator", "tests", "def", "tests.def.dll"),
                this.GetFakeFile("tools", "metamorphosator", "tests", "v1", "test-assembly.dll"),
                this.GetFakeFile("tools", "metamorphosator", "tests", "v1", "test-assembly.pdb"),
                this.GetFakeFile("tools", "metamorphosator", "tests", "v2", "test-assembly.dll"),
                this.GetFakeFile("tools", "metamorphosator", "tests", "v2", "test-assembly.pdb")
            };

            var actual = finder.GetFiles(baseDir, pattern);

            CollectionAssert.AreEquivalent(expected, actual);
            foreach (var dir in this.fakedDirectories.Values.Where(x => x.FullName != GetRoot()))
            {
                dir.Received().GetFiles(filePattern);
            }
            baseDir.Parent.DidNotReceive().GetFiles(Arg.Any<string>());
            abcDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            defDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            v1Dir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            v2Dir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetFiles_WordThenParentThenWordWithWildcardThenWord()
        {
            var filename = "readme.txt";
            var pattern = "tests/../addin?/" + filename;
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator");
            var targetDir = this.GetFakeDirectory("tools", "metamorphosator", "addins");
            baseDir.GetDirectories("tests", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { this.GetFakeDirectory("tools", "metamorphosator", "tests") });
            this.GetFakeDirectory("tools", "metamorphosator").GetDirectories("addin?", SIO.SearchOption.TopDirectoryOnly).Returns(new[] { targetDir });
            targetDir.GetFiles(filename).Returns(new[] { this.GetFakeFile("tools", "metamorphosator", "addins", filename) });
            var expected = new[] { this.GetFakeFile("tools", "metamorphosator", "addins", filename) };

            var actual = finder.GetFiles(baseDir, pattern);

            CollectionAssert.AreEquivalent(expected, actual);
            targetDir.Received().GetFiles(filename);
            foreach (var dir in this.fakedDirectories.Values.Where(x => x != targetDir))
            {
                dir.DidNotReceive().GetFiles(Arg.Any<string>());
            }
            targetDir.Received().GetFiles(filename);
            targetDir.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
            GetFakeDirectory("tools", "frobuscator").DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetFiles_CurrentDirThenAsterisk()
        {
            var pattern = "./*";
            var finder = new DirectoryFinder(this.fileSystem);
            var baseDir = this.GetFakeDirectory("tools", "metamorphosator", "addins", "morph");
            var expected = new[] { this.GetFakeFile("tools", "metamorphosator", "addins", "morph", "setup.ini"), this.GetFakeFile("tools", "metamorphosator", "addins", "morph", "code.cs") };
            baseDir.GetFiles("*").Returns(expected);

            var actual = finder.GetFiles(baseDir, pattern);

            CollectionAssert.AreEquivalent(expected, actual);
            baseDir.Received().GetFiles("*");
            baseDir.Parent.DidNotReceive().GetFiles(Arg.Any<string>());
            baseDir.Parent.DidNotReceive().GetDirectories(Arg.Any<string>(), Arg.Any<SIO.SearchOption>());
        }

        [Test]
        public void GetFiles_StartDirectoryIsNull()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetFiles((IDirectory)null, "notused"), Throws.ArgumentNullException.With.Message.Contains(" startDirectory "));
        }

        [Test]
        public void GetFiles_PatternIsNull()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetDirectories(Substitute.For<IDirectory>(), null), Throws.ArgumentNullException.With.Message.Contains(" pattern "));
        }

        [Test]
        public void GetFiles_PatternIsEmpty()
        {
            var finder = new DirectoryFinder(Substitute.For<IFileSystem>());

            Assert.That(() => finder.GetFiles(Substitute.For<IDirectory>(), string.Empty), Throws.ArgumentException.With.Message.Contains(" pattern "));
        }

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

        private IFile GetFakeFile(params string[] parts)
        {
            return this.fakedFiles[CreateAbsolutePath(parts)];
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
                return string.Empty;
            }
        }
    }
}
