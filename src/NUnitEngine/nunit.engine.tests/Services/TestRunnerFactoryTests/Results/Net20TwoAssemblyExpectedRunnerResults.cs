﻿// ***********************************************************************
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
    internal static class Net20TwoAssemblyExpectedRunnerResults
    {
        public static RunnerResult ResultFor(ProcessModel processModel, DomainUsage domainUsage)
        {
            switch (processModel)
            {
                case ProcessModel.Default:
                    return GetProcessModelDefaultResult(domainUsage);
                case ProcessModel.InProcess:
                    return GetProcessModelInProcessResult(domainUsage);
                case ProcessModel.Separate:
                    return GetProcessModelSeparateResult(domainUsage);
                case ProcessModel.Multiple:
                    return GetProcessModelMultipleResult(domainUsage);
                default:
                    ThrowOutOfRange(processModel);
                    break;
            }

            throw new ArgumentOutOfRangeException(
                $"No expected Test result provided for this {nameof(ProcessModel)}/{nameof(DomainUsage)} combination.");
        }

        private static RunnerResult GetProcessModelMultipleResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.None:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.Single:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                default:
                    ThrowOutOfRange(domainUsage);
                    break;
            }
            return null;
        }

        private static RunnerResult GetProcessModelSeparateResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                    return new RunnerResult
                    {
                        TestRunner = typeof(ProcessRunner)
                    };
                case DomainUsage.None:
                    return new RunnerResult
                    {
                        TestRunner = typeof(ProcessRunner)
                    };
                case DomainUsage.Single:
                    return new RunnerResult
                    {
                        TestRunner = typeof(ProcessRunner)
                    };
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(ProcessRunner)
                    };
                default:
                    ThrowOutOfRange(domainUsage);
                    break;
            }

            return null;
        }

        private static RunnerResult GetProcessModelInProcessResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestDomainRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(TestDomainRunner)},
                            new RunnerResult {TestRunner = typeof(TestDomainRunner)}
                        }
                    };
                case DomainUsage.None:
                    return new RunnerResult
                    {
                        TestRunner = typeof(LocalTestRunner)
                    };
                case DomainUsage.Single:
                    return new RunnerResult
                    {
                        TestRunner = typeof(TestDomainRunner)
                    };
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestDomainRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(TestDomainRunner)},
                            new RunnerResult {TestRunner = typeof(TestDomainRunner)}
                        }
                    };
                default:
                    ThrowOutOfRange(domainUsage);
                    break;
            }

            return null;
        }

        private static RunnerResult GetProcessModelDefaultResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.None:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.Single:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            new RunnerResult {TestRunner = typeof(ProcessRunner)},
                            new RunnerResult {TestRunner = typeof(ProcessRunner)}
                        }
                    };
                default:
                    ThrowOutOfRange(domainUsage);
                    break;
            }

            return null;
        }

        private static void ThrowOutOfRange<T>(T domainUsage)
        {
            throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage,
                $"No expected Test result provided for this {nameof(ProcessModel)}/{nameof(DomainUsage)} combination.");
        }
    }
#endif
}