// ***********************************************************************
// Copyright (c) 2016 Joseph N. Musser II
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

using NUnit.Engine.Internal;
using NUnit.Framework;
using System.Text;

namespace NUnit.Engine.Tests.Internal
{
    public static class ProcessUtilsTests
    {
        private static string EscapeProcessArgument(string value, bool alwaysQuote = false)
        {
            var builder = new StringBuilder();
            ProcessUtils.EscapeProcessArgument(builder, value, alwaysQuote);
            return builder.ToString();
        }

        [Test]
        public static void EscapeProcessArgument_null()
        {
            Assert.That(EscapeProcessArgument(null), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArgument_null_alwaysQuote()
        {
            Assert.That(EscapeProcessArgument(null, true), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArgument_empty()
        {
            Assert.That(EscapeProcessArgument(string.Empty), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArgument_empty_alwaysQuote()
        {
            Assert.That(EscapeProcessArgument(string.Empty, true), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArgument_simple()
        {
            Assert.That(EscapeProcessArgument("123"), Is.EqualTo("123"));
        }

        [Test]
        public static void EscapeProcessArgument_simple_alwaysQuote()
        {
            Assert.That(EscapeProcessArgument("123", true), Is.EqualTo("\"123\""));
        }

        [Test]
        public static void EscapeProcessArgument_with_ending_backslash()
        {
            Assert.That(EscapeProcessArgument("123\\"), Is.EqualTo("123\\"));
        }

        [Test]
        public static void EscapeProcessArgument_with_ending_backslash_alwaysQuote()
        {
            Assert.That(EscapeProcessArgument("123\\", true), Is.EqualTo("\"123\\\\\""));
        }

        [Test]
        public static void EscapeProcessArgument_with_spaces_and_ending_backslash()
        {
            Assert.That(EscapeProcessArgument(" 1 2 3 \\"), Is.EqualTo("\" 1 2 3 \\\\\""));
        }

        [Test]
        public static void EscapeProcessArgument_with_spaces()
        {
            Assert.That(EscapeProcessArgument(" 1 2 3 "), Is.EqualTo("\" 1 2 3 \""));
        }

        [Test]
        public static void EscapeProcessArgument_with_quotes()
        {
            Assert.That(EscapeProcessArgument("\"1\"2\"3\""), Is.EqualTo("\"\\\"1\\\"2\\\"3\\\"\""));
        }

        [Test]
        public static void EscapeProcessArgument_with_slashes()
        {
            Assert.That(EscapeProcessArgument("1\\2\\\\3\\\\\\"), Is.EqualTo("1\\2\\\\3\\\\\\"));
        }

        [Test]
        public static void EscapeProcessArgument_with_slashes_alwaysQuote()
        {
            Assert.That(EscapeProcessArgument("1\\2\\\\3\\\\\\", true), Is.EqualTo("\"1\\2\\\\3\\\\\\\\\\\\\""));
        }

        [Test]
        public static void EscapeProcessArgument_slashes_followed_by_quotes()
        {
            Assert.That(EscapeProcessArgument("\\\\\""), Is.EqualTo("\"\\\\\\\\\\\"\""));
        }
    }
}