﻿// ***********************************************************************
// Copyright (c) 2010 Charlie Poole
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
using NUnit.Framework.Api;

namespace NUnit.Framework.Internal
{
    /// <summary>
    /// TODO: Documentation needed for class
    /// </summary>
    public class CommandBuilder
    {
        /// <summary>
        /// TODO: Documentation needed for method
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static TestCommand MakeTestCommand(Test test)
        {
            if (test.RunState != RunState.Runnable && test.RunState != RunState.Explicit)
                return new SkipCommand(test);

            TestSuite suite = test as TestSuite;
            if (suite != null)
                return MakeTestCommand(suite);

            return MakeTestCommand(test as TestMethod);
        }

        /// <summary>
        /// TODO: Documentation needed for method
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static TestCommand MakeTestCommand(TestMethod test)
        {
            Guard.ArgumentNotNull(test, "test");

            TestCommand command = new TestCaseCommand(test);

            if (test.exceptionProcessor != null)
                command = new ExpectedExceptionCommand(command);

            command = new SetUpTearDownCommand(command);

            if (test.Properties.ContainsKey(PropertyNames.MaxTime))
                command = new MaxTimeCommand(command);

#if !NUNITLITE
            if (test.ShouldRunOnOwnThread)
                command = new ThreadedTestCommand(command);
#endif

            if (test.Properties.ContainsKey(PropertyNames.RepeatCount))
                command = new RepeatedTestCommand(command);

            command = new TestExecutionContextCommand(
                new TestMethodCommand(command));

            return command;
        }

        /// <summary>
        /// TODO: Documentation needed for method
        /// </summary>
        /// <param name="suite"></param>
        /// <returns></returns>
        public static TestCommand MakeTestCommand(TestSuite suite)
        {
            Guard.ArgumentNotNull(suite, "suite");

            TestCommand command = new TestSuiteCommand(suite);

            foreach (Test childTest in suite.Tests)
                //if (suite.Filter.Pass(childTest))
                    command.Children.Add(MakeTestCommand(childTest));

#if !NUNITLITE
            if (suite.ShouldRunOnOwnThread)
                command = new ThreadedTestCommand(command);
#endif

            command = new TestExecutionContextCommand(command);

            return command;
        }
    }
}
