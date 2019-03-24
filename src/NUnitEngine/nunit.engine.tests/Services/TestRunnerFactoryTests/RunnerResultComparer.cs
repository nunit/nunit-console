// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
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
using System.Linq;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    internal class RunnerResultComparer : IEqualityComparer<RunnerResult>
    {
        public static readonly RunnerResultComparer Instance = new RunnerResultComparer();

        public bool Equals(RunnerResult x, RunnerResult y)
        {
            x = x ?? throw new ArgumentNullException(nameof(x));
            y = y ?? throw new ArgumentNullException(nameof(y));

            return x.TestRunner == y.TestRunner &&
                   x.SubRunners.SequenceEqual(y.SubRunners, Instance);
        }

        public int GetHashCode(RunnerResult obj)
        {
            throw new NotImplementedException();
        }
    }
}