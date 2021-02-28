// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// A Test Runner factory can supply a suitable test runner for a given package
    /// </summary>
    public interface ITestRunnerFactory
    {
        /// <summary>
        /// Return a suitable runner for the package provided as an argument
        /// </summary>
        /// <param name="package">The test package to be loaded by the runner</param>
        /// <returns>A TestRunner</returns>
        ITestEngineRunner MakeTestRunner(TestPackage package);

        /// <summary>
        /// Return true if the provided runner is suitable for reuse in loading
        /// the test package provided. Otherwise, return false. Runners that
        /// cannot be reused must always return false.
        /// </summary>
        /// <param name="runner">An ITestRunner to possibly be used.</param>
        /// <param name="package">The TestPackage to be loaded.</param>
        /// <returns>True if the runner may be reused for the provided package.</returns>
        bool CanReuse(ITestEngineRunner runner, TestPackage package);
    }
}
