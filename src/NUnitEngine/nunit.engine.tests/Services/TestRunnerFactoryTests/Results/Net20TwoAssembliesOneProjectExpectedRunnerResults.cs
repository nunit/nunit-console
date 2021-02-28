// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results
{
#if !NETCOREAPP
    internal static class Net20TwoAssembliesOneProjectExpectedRunnerResults
    {
        private static readonly string ExceptionMessage =
            $"No expected Test result provided for this {nameof(ProcessModel)}/{nameof(DomainUsage)} combination.";

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
                    throw new ArgumentOutOfRangeException(nameof(processModel), processModel, ExceptionMessage);
            }
        }

        private static RunnerResult GetProcessModelMultipleResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                case DomainUsage.None:
                case DomainUsage.Single:
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestProcessRunner),
                        SubRunners = new[]
                        {
                            RunnerResult.ProcessRunner,
                            RunnerResult.ProcessRunner,
                            RunnerResult.ProcessRunner
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
            }
        }

        private static RunnerResult GetProcessModelSeparateResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                case DomainUsage.None:
                case DomainUsage.Single:
                case DomainUsage.Multiple:
                    return RunnerResult.ProcessRunner;
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
            }
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
                            RunnerResult.TestDomainRunner,
                            RunnerResult.TestDomainRunner,
                            RunnerResult.TestDomainRunner
                        }
                    };
                case DomainUsage.None:
                    return RunnerResult.LocalTestRunner;
                case DomainUsage.Single:
                    return RunnerResult.TestDomainRunner;
                case DomainUsage.Multiple:
                    return new RunnerResult
                    {
                        TestRunner = typeof(MultipleTestDomainRunner),
                        SubRunners = new[]
                        {
                            RunnerResult.TestDomainRunner,
                            RunnerResult.TestDomainRunner,
                            RunnerResult.TestDomainRunner
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
            }
        }

        private static RunnerResult GetProcessModelDefaultResult(DomainUsage domainUsage)
        {
            switch (domainUsage)
            {
                case DomainUsage.Default:
                case DomainUsage.None:
                case DomainUsage.Single:
                case DomainUsage.Multiple:
                    return new RunnerResult(typeof(AggregatingTestRunner),
                        RunnerResult.ProcessRunner,
                        RunnerResult.ProcessRunner,
                        RunnerResult.MultiRunnerWithTwoSubRunners);
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
            }
        }
    }
#endif
}