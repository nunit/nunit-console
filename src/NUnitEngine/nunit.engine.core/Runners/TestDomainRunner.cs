// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestDomainRunner loads and runs tests in a separate
    /// domain whose lifetime it controls.
    /// </summary>
    public class TestDomainRunner : DirectTestRunner
    {
        private DomainManager _domainManager;

        public TestDomainRunner(TestPackage package) : base(package)
        {
            _domainManager = new DomainManager();
        }

        public override TestEngineResult Load()
        {
            TestDomain = _domainManager.CreateDomain(TestPackage);

            return base.Load();
        }

        /// <summary>
        /// Unload any loaded TestPackage as well as the application domain.
        /// </summary>
        public override void Unload()
        {
            if (this.TestDomain != null)
            {
                _domainManager.Unload(this.TestDomain);
                this.TestDomain = null;
            }
        }
    }
}
#endif