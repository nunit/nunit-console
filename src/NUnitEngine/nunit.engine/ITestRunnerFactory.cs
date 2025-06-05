﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
    }
}
