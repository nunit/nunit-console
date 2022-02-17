// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// LocalTestRunner runs tests in the current application domain.
    /// </summary>
    public class LocalTestRunner : TestAgentRunner
    {
        public LocalTestRunner(TestPackage package) : base(package)
        {
            TestDomain = AppDomain.CurrentDomain;
        }
    }
}
