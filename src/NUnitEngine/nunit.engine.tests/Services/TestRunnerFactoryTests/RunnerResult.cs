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
using System.Text;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    public class RunnerResult
    {
        public Type TestRunner { get; set; }

        public ICollection<RunnerResult> SubRunners { get; set; } = new List<RunnerResult>();

        public override string ToString()
        {
            return Environment.NewLine + ToString(0) + Environment.NewLine;
        }

        private string ToString(int level)
        {
            var tabs = new string('\t', level);

            var sb = new StringBuilder();
            sb.AppendLine($"{tabs}TestRunner: {TestRunner.Name}");

            if (SubRunners.Count == 0)
                return sb.ToString().Trim();

            level++;
            tabs = new string('\t', level);

            sb.AppendLine(tabs + "SubRunners:");

            level++;
            tabs = new string('\t', level);

            foreach (var subRunner in SubRunners)
            {
                sb.AppendLine($"{tabs}{subRunner.ToString(level)}");
            }
            return sb.ToString().Trim();
        }
    }
}