// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results
{
#if !NETCOREAPP
    internal static class Net20SingleAssemblyStringCtorExpectedRunnerResults
    {
        private static readonly string ExceptionMessage =
            $"No expected Test result provided for this {nameof(ProcessModel)}/{nameof(DomainUsage)} combination.";

        public static RunnerResult ResultFor(ProcessModel processModel, DomainUsage domainUsage)
        {
            switch (processModel)
            {
                case ProcessModel.Default:
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
                case ProcessModel.InProcess:
                    switch (domainUsage)
                    {
                        case DomainUsage.Default:
                            return RunnerResult.TestDomainRunner;
                        case DomainUsage.None:
                            return RunnerResult.LocalTestRunner;
                        case DomainUsage.Single:
                            return RunnerResult.TestDomainRunner;
                        case DomainUsage.Multiple:
                            return RunnerResult.TestDomainRunner;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
                    }
                case ProcessModel.Separate:
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
                case ProcessModel.Multiple:
                    switch (domainUsage)
                    {
                        case DomainUsage.Default:
                        case DomainUsage.None:
                        case DomainUsage.Single:
                        case DomainUsage.Multiple:
                            return new RunnerResult { TestRunner = typeof(MultipleTestProcessRunner) };
                        default:
                            throw new ArgumentOutOfRangeException(nameof(domainUsage), domainUsage, ExceptionMessage);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(processModel), processModel, ExceptionMessage);
            }
        }
    }
#endif
}