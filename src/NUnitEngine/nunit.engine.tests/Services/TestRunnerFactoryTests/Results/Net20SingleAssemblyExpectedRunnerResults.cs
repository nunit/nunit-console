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
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results
{
#if !NETCOREAPP
    internal static class Net20SingleAssemblyExpectedRunnerResults
    {
        public static RunnerResult ResultFor(ProcessModel processModel, DomainUsage domainUsage)
        {
            if (processModel == ProcessModel.Default && domainUsage == DomainUsage.Default)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Default && domainUsage == DomainUsage.None)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Default && domainUsage == DomainUsage.Multiple)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Default && domainUsage == DomainUsage.Single)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.InProcess && domainUsage == DomainUsage.Default)
            {
                return new RunnerResult { TestRunner = typeof(TestDomainRunner) };
            }
            if (processModel == ProcessModel.InProcess && domainUsage == DomainUsage.None)
            {
                return new RunnerResult { TestRunner = typeof(LocalTestRunner) };
            }
            if (processModel == ProcessModel.InProcess && domainUsage == DomainUsage.Multiple)
            {
                return new RunnerResult { TestRunner = typeof(MultipleTestDomainRunner) };
            }
            if (processModel == ProcessModel.InProcess && domainUsage == DomainUsage.Single)
            {
                return new RunnerResult { TestRunner = typeof(TestDomainRunner) };
            }
            if (processModel == ProcessModel.Separate && domainUsage == DomainUsage.Default)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Separate && domainUsage == DomainUsage.None)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Separate && domainUsage == DomainUsage.Multiple)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Separate && domainUsage == DomainUsage.Single)
            {
                return new RunnerResult { TestRunner = typeof(ProcessRunner) };
            }
            if (processModel == ProcessModel.Multiple && domainUsage == DomainUsage.Default)
            {
                return new RunnerResult { TestRunner = typeof(MultipleTestProcessRunner) };
            }
            if (processModel == ProcessModel.Multiple && domainUsage == DomainUsage.None)
            {
                return new RunnerResult { TestRunner = typeof(MultipleTestProcessRunner) };
            }
            if (processModel == ProcessModel.Multiple && domainUsage == DomainUsage.Multiple)
            {
                return new RunnerResult { TestRunner = typeof(MultipleTestProcessRunner) };
            }
            if (processModel == ProcessModel.Multiple && domainUsage == DomainUsage.Single)
            {
                return new RunnerResult { TestRunner = typeof(MultipleTestProcessRunner) };
            }
            throw new ArgumentOutOfRangeException(
                $"No expected Test result provided for this {nameof(ProcessModel)}/{nameof(DomainUsage)} combination.");
        }
    }
#endif
}