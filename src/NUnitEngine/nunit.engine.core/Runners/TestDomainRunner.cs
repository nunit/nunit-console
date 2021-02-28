// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestDomainRunner loads and runs tests in a separate
    /// domain whose lifetime it controls.
    /// </summary>
    public class TestDomainRunner : DirectTestRunner
    {
        private DomainManager _domainManager;

        public TestDomainRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
            _domainManager = Services.GetService<DomainManager>();
        }

        protected override TestEngineResult LoadPackage()
        {
            TestDomain = _domainManager.CreateDomain(TestPackage);

            return base.LoadPackage();
        }

        /// <summary>
        /// Unload any loaded TestPackage as well as the application domain.
        /// </summary>
        public override void UnloadPackage()
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