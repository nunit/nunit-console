// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    public class RunnerResult
    {
#if !NETCOREAPP
        public static RunnerResult TestDomainRunner => new RunnerResult(typeof(TestDomainRunner));
        public static RunnerResult ProcessRunner => new RunnerResult(typeof(ProcessRunner));

        public static RunnerResult MultipleProcessRunner(int numAssemblies)
        {
            return new RunnerResult()
            {
                TestRunner = typeof(MultipleTestProcessRunner),
                SubRunners = GetSubRunners(ProcessRunner, numAssemblies)
            };
        }
#endif
        public static RunnerResult LocalTestRunner => new RunnerResult(typeof(LocalTestRunner));

        public RunnerResult()
        { }

        public RunnerResult(Type testRunner, params RunnerResult[] subRunners)
        {
            TestRunner = testRunner;
            SubRunners = subRunners;
        }

        public Type TestRunner { get; set; }

        public ICollection<RunnerResult> SubRunners { get; set; } = new List<RunnerResult>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"TestRunner: {TestRunner.Name}");

            if (SubRunners.Count == 0)
                return sb.ToString().Trim();

            sb.AppendLine("SubRunners:");
            sb.AppendLine("[");

            foreach (var subRunner in SubRunners)
            {
                sb.AppendLine($"\t{subRunner}");
            }
            sb.AppendLine("]");
            return sb.ToString().Trim();
        }

        private static RunnerResult[] GetSubRunners(RunnerResult subRunner, int count)
        {
            var subRunners = new RunnerResult[count];
            for (int i = 0; i < count; i++)
                subRunners[i] = subRunner;

            return subRunners;
        }
    }
}