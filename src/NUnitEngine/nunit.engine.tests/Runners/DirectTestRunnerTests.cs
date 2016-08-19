using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.Engine.Runners.Tests
{
    public class DirectTestRunnerTests : AbstractTestRunner
    {
        public DirectTestRunnerTests()
            : base(new ServiceContext(), new TestPackage(null))
        {
            
        }
        #region AbstractTestRunner Overrides

        protected override int CountTests(TestFilter filter)
        {
            throw new NotImplementedException();
        }

        protected override TestEngineResult ExploreTests(TestFilter filter)
        {
            throw new NotImplementedException();
        }

        protected override TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
        {
            throw new NotImplementedException();
        }

        public override void StopRun(bool force)
        {
            throw new NotImplementedException();
        }

        protected override TestEngineResult LoadPackage()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
