// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
namespace NUnit.Engine.Runners
{
    /// <summary>
    /// MultipleTestDomainRunner runs tests using separate
    /// application domains for each assembly.
    /// </summary>
    public class MultipleTestDomainRunner : AggregatingTestRunner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleTestDomainRunner"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="package">The package.</param>
        public MultipleTestDomainRunner(IServiceLocator services, TestPackage package) : base(services, package) { }

        protected override ITestEngineRunner CreateRunner(TestPackage package)
        {
            return new TestDomainRunner(Services, package);
        }
    }
}
#endif