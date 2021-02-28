// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt


using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
#if !NET35
    class NetCoreConsoleOptionsTest
    {
        [TestCaseSource(nameof(TestCases))]
        public void InvalidOptionsShowError(string arg, bool isValidOnNetCore)
        {
            var options = ConsoleMocks.Options("mock-assembly.dll", arg);
            var errorsExpected = isValidOnNetCore ? 0 : 1;
            Assert.That(options.ErrorMessages, Has.Exactly(errorsExpected).Contains("not available on this platform"));
        }

        private static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                foreach (var validOption in ValidOptions)
                    yield return new TestCaseData(validOption, true);

                foreach (var invalidOption in InvalidOptions)
                    yield return new TestCaseData(invalidOption, false);
            }
        }

        private static readonly string[] ValidOptions = new[]
        {
            "-test=ATest",
            "-testList=TestListWithEmptyLine.tst",
            "-where=astring",
            "-params=A=B",
            "-timeout=1",
            "-seed=1",
            "-workers=1",
            "-stoponerror",
            "-wait",
            "-work=.",
            "-out=.",
            "-result=result.xml",
            "-explore",
            "-noresult",
            "-labels=Before",
            "-test-name-format={m}",
            "-trace=Debug",
            "-teamcity",
            "-noheader",
            "-nocolor",
            "-help",
            "-version",
            "-encoding=ascii",
            "-config=Release",
            "-dispose-runners",
            "skipnontestassemblies",
            "-list-extensions"
        };

        private static readonly string[] InvalidOptions = new[]
        {
            "-configfile=app.config",
            "-process=Single",
            "-inprocess",
            "-domain=Single",
            "-framework=net-3.5",
            "-x86",
            "-shadowcopy",
            "-loaduserprofile",
            "-agents=1",
            "-debug",
            "-pause",
            "-set-principal-policy=NoPrincipal",
#if DEBUG
            "-debug-agent"
#endif
        };
    }
#endif
}
