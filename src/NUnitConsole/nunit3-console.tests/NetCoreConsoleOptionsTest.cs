// ***********************************************************************
// Copyright (c) 2019 Charlie Poole, Rob Prouse
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
