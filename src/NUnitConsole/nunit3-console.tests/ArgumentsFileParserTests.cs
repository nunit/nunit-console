// ***********************************************************************
// Copyright (c) 2011 Charlie Poole
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

using System;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    [TestFixture]
    public class ArgumentsFileParserTests
    {
        [Test]
        [TestCase("--arg", "--arg")]
        [TestCase("--arg1 --arg2", "--arg1\n--arg2")]
        [TestCase("--arg1\n--arg2", "--arg1\n--arg2")]
        [TestCase("", "")]
        [TestCase("   ", "")]
        [TestCase("--arg1 --arg2", "--arg1\n--arg2")]
        [TestCase("\"--arg 1\" --arg2", "--arg 1\n--arg2")]
        [TestCase("\"--arg 1\"\n--arg2", "--arg 1\n--arg2")]
        [TestCase("--arg1 \"--arg 2\"", "--arg1\n--arg 2")]
        [TestCase("\"--arg 1\" \"--arg 2\"", "--arg 1\n--arg 2")]
        [TestCase("\"--arg 1\" \"--arg 2\" arg3 \"arg 4\"", "--arg 1\n--arg 2\narg3\narg 4")]
        [TestCase("--arg1 \"--arg 2\" arg3 \"arg 4\"", "--arg1\n--arg 2\narg3\narg 4")]
        [TestCase("\"--arg 1\" \"--arg 2\" arg3 \"arg 4\"\n\"--arg 1\" \"--arg 2\" arg3 \"arg 4\"", "--arg 1\n--arg 2\narg3\narg 4\n--arg 1\n--arg 2\narg3\narg 4")]
        [TestCase("\"--arg\"", "--arg")]
        [TestCase("\"--arg 1\"", "--arg 1")]
        [TestCase("\"--arg\"1\"", "--arg\n1")]
        public void ShouldParseArgumentsFromLines(string linesStr, string expectedArgsStr)
        {
            // Given
            var parser = new ArgumentsFileParser();
            var lines = linesStr.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var expectedArgs = expectedArgsStr.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // When
            var actualArgs = parser.Convert(lines);

            // Then
            Assert.AreEqual(expectedArgs, actualArgs);
        }
    }
}
