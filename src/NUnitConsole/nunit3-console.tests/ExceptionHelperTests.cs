// ***********************************************************************
// Copyright (c) 2017 Charlie Poole, Rob Prouse
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Internal;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    class ExceptionHelperTests
    {
        [TestCaseSource(nameof(TestCases))]
        public void TestMessageContainsAllInnerExceptions(Exception ex, params Type[] expectedExceptions)
        {
            var message = ExceptionHelper.BuildMessage(ex);

            Assert.Multiple(() =>
            {
                foreach (var expectedException in expectedExceptions)
                    Assert.That(message, Contains.Substring(expectedException.Name));
            });
        }

        [TestCaseSource(nameof(TestCases))]
        public void StackTraceContainsAllInnerExceptions(Exception ex, params Type[] expectedExceptions)
        {
            var stackTrace = ExceptionHelper.BuildMessageAndStackTrace(ex);

            Assert.Multiple(() =>
            {
                foreach (var expectedException in expectedExceptions)
                    Assert.That(stackTrace, Contains.Substring(expectedException.Name));
            });
        }

        private static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                yield return new TestCaseData(new FileNotFoundException(), new[] { typeof(FileNotFoundException) })
                    .SetName("{m}(Simple Exception)");

                var innerException = new NUnitEngineException("message", new InvalidOperationException());
                yield return new TestCaseData(innerException, new[] { typeof(NUnitEngineException), typeof(InvalidOperationException) })
                    .SetName("{m}(Single InnerException)");

                var exception1 = new InvalidOperationException();
                var exception2 = new FileNotFoundException("message", exception1);
                var exception3 = new AccessViolationException("message", exception2);
                yield return new TestCaseData(exception3, new[] { typeof(InvalidOperationException), typeof(FileNotFoundException), typeof(AccessViolationException) })
                    .SetName("{m}(Multiple InnerExceptions)");

                var relfectionException = new ReflectionTypeLoadException(new[] { typeof(ExceptionHelperTests) }, new[] { new FileNotFoundException() });
                yield return new TestCaseData(relfectionException, new[] { typeof(ReflectionTypeLoadException), typeof(FileNotFoundException) })
                    .SetName("{m}(LoaderException)");
            }
        }

    }
}
