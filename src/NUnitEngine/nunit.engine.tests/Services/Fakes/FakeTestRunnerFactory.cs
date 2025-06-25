// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    public class FakeTestRunnerFactory : Service, ITestRunnerFactory
    {
        private ITestEngineRunner _testEngineRunner;

        public FakeTestRunnerFactory(ITestEngineRunner testEngineRunner)
        {
            _testEngineRunner = testEngineRunner;
        }

        public ITestEngineRunner MakeTestRunner(TestPackage package)
        {
            return _testEngineRunner;
        }
    }
}
