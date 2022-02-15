// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.ComponentModel;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestEngineRunner is the base class for all internal runners
    /// within the NUnit Engine. It extends AbstractTestRunner by
    /// adding a ServiceContext through which engine services are 
    /// made avaialble to the runner.
    /// </summary>
    public abstract class TestEngineRunner : AbstractTestRunner
    {
        public TestEngineRunner(IServiceLocator services, TestPackage package)
            : base(package)
        {
            Services = services;
            TestRunnerFactory = Services.GetService<ITestRunnerFactory>();
        }

        /// <summary>
        /// Our Service Context
        /// </summary>
        protected IServiceLocator Services { get; private set; }

        protected ITestRunnerFactory TestRunnerFactory { get; private set; }

    }
}
