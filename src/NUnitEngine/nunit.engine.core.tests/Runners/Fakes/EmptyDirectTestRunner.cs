// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NUnit.Engine.Tests.Runners.Fakes
{
    internal class EmptyDirectTestRunner : Engine.Runners.DirectTestRunner
    {
        public EmptyDirectTestRunner(TestPackage package) : base(package)
        {
            TestDomain = AppDomain.CurrentDomain;
        }

        public new void Load()
        {
            base.Load();
        }
    }
}
