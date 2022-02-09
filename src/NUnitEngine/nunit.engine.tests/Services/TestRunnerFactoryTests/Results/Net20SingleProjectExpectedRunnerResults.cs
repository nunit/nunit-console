// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results
{
#if !NETCOREAPP
    internal static class Net20SingleProjectExpectedRunnerResults
    {
        private static readonly string ExceptionMessage =
            $"No expected Test result provided for {nameof(ProcessModel)}.";

        public static RunnerResult ResultFor(ProcessModel processModel)
        {
            switch (processModel)
            {
                case ProcessModel.Default:
                    return GetProcessModelDefaultResult();
                case ProcessModel.InProcess:
                    return GetProcessModelInProcessResult();
                case ProcessModel.Separate:
                    return GetProcessModelSeparateResult();
                case ProcessModel.Multiple:
                    return GetProcessModelMultipleResult();
                default:
                    throw new ArgumentOutOfRangeException(nameof(processModel), processModel, ExceptionMessage);
            }
        }

        private static RunnerResult GetProcessModelMultipleResult()
        {
            return new RunnerResult
            {
                TestRunner = typeof(MultipleTestProcessRunner),
                SubRunners = new[]
                {
                    RunnerResult.ProcessRunner,
                    RunnerResult.ProcessRunner
                }
            };
        }

        private static RunnerResult GetProcessModelSeparateResult()
        {
            return RunnerResult.ProcessRunner;
        }

        private static RunnerResult GetProcessModelInProcessResult()
        {
            return RunnerResult.TestDomainRunner;
        }

        private static RunnerResult GetProcessModelDefaultResult()
        {
            return new RunnerResult()
            {
                TestRunner = typeof(AggregatingTestRunner),
                SubRunners = new[]
                {
                    RunnerResult.ProcessRunner,
                    RunnerResult.ProcessRunner
                }
            };
        }
    }
#endif
}