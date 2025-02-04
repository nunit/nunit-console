// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Drivers;
using System;

namespace NUnit.Engine.Runners
{
    internal class FakeTestAgentRunner : TestAgentRunner
    {
        public FakeTestAgentRunner(TestPackage package, IDriverService? driverService = null) : base(package)
        {
            TestDomain = AppDomain.CurrentDomain;
            DriverService = driverService;
        }

        public new void Load()
        {
            base.Load();
        }
    }
}
