// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// MultipleTestProcessRunner runs tests using separate
    /// Processes for each assembly.
    /// </summary>
    public class MultipleTestProcessRunner : AggregatingTestRunner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleTestProcessRunner"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="package">The package.</param>
        public MultipleTestProcessRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
        }

        public override int LevelOfParallelism
        {
            get
            {
                var maxAgents = TestPackage.GetSetting(EnginePackageSettings.MaxAgents, Environment.ProcessorCount);
                return Math.Min(maxAgents, TestPackage.SubPackages.Count);
            }
        }

        protected override ITestEngineRunner CreateRunner(TestPackage package)
        {
            return new ProcessRunner(Services, package);
        }
    }
}
#endif