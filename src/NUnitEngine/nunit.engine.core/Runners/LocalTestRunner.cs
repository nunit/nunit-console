// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// LocalTestRunner runs tests in the current application domain.
    /// </summary>
    public class LocalTestRunner : DirectTestRunner
    {
        public LocalTestRunner(IServiceLocator services, ITestPackage package) : base(services, package)
        {
            TestDomain = AppDomain.CurrentDomain;
        }
    }
}
