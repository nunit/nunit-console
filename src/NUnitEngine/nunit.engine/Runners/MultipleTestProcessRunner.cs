﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using NUnit.Common;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// MultipleTestProcessRunner runs tests using separate
    /// Processes for each assembly.
    /// </summary>
    public class MultipleTestProcessRunner : AggregatingTestRunner
    {
        private int _processorCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleTestProcessRunner"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="package">The package.</param>
        /// <param name="processorCount">Processor count (used for testing)</param>
        public MultipleTestProcessRunner(IServiceLocator services, TestPackage package, int processorCount = 0)
            : base(services, package)
        {
            _processorCount = processorCount <= 0
                ? Environment.ProcessorCount
                : processorCount;
        }

        public override int LevelOfParallelism
        {
            get
            {
                var maxAgents = TestPackage.Settings.HasSetting(SettingDefinitions.MaxAgents)
                    ? TestPackage.Settings.GetValueOrDefault(SettingDefinitions.MaxAgents)
                    : _processorCount;
                return Math.Min(maxAgents, TestPackages.Count);
            }
        }

        protected override ITestEngineRunner CreateRunner(TestPackage package)
        {
            return new ProcessRunner(Services, package);
        }
    }
}
#endif
