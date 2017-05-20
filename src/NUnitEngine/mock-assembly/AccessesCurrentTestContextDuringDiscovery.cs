using NUnit.Framework;

namespace NUnit.Tests
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
        public void Access_by_TestCaseSource(int arg) { }

        [Test]
        public void Access_by_ValueSource([ValueSource(nameof(TestCases))] int arg) { }
    }
}
