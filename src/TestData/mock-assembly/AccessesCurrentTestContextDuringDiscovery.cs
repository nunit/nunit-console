// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;

namespace NUnit.TestData
{
    public class AccessesCurrentTestContextDuringDiscovery
    {
        public const int Tests = 2;
        public const int Suites = 1;

        public static int[] TestCases()
        {
            var _ = TestContext.CurrentContext;
            return new[] { 0 };
        }

        [TestCaseSource(nameof(TestCases))]
        public void Access_by_TestCaseSource(int arg)
        {
        }

        [Test]
        public void Access_by_ValueSource([ValueSource(nameof(TestCases))] int arg)
        {
        }
    }
}
