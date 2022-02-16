// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Runners
{
    internal class FakeTestAgentRunner : Engine.Runners.TestAgentRunner
    {
        public FakeTestAgentRunner(TestPackage package) : base(package)
        {
            TestDomain = AppDomain.CurrentDomain;
        }

        public new void Load()
        {
            base.Load();
        }
    }
}
