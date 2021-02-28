// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
