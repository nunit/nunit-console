// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// InProcessTestRunnerFactory handles creation of a suitable test 
    /// runner for a given package to be loaded and run within the
    /// same process.
    /// </summary>
    public class InProcessTestRunnerFactory : Service, ITestRunnerFactory
    {
        private readonly ILogger _log = InternalTrace.GetLogger(typeof(InProcessTestRunnerFactory));

        /// <summary>
        /// Returns a test runner based on the settings in a TestPackage.
        /// Any setting that is "consumed" by the factory is removed, so
        /// that downstream runners using the factory will not repeatedly
        /// create the same type of runner.
        /// </summary>
        /// <param name="package">The TestPackage to be loaded and run</param>
        /// <returns>An ITestEngineRunner</returns>
        public virtual ITestEngineRunner MakeTestRunner(TestPackage package)
        {
            var projectService = ServiceContext.GetService<IProjectService>();
            var projectCount = 0;

            foreach (var subPackage in package.SubPackages)
            {
                if (projectService.CanLoadFrom(subPackage.FullName))
                    projectCount++;
            }

            DomainUsage domainUsage = (DomainUsage)System.Enum.Parse(
                typeof(DomainUsage),
                package.GetSetting(EnginePackageSettings.DomainUsage, "Default"));

            switch (domainUsage)
            {
                default:
                case DomainUsage.Default:
                    if (package.SubPackages.Count > 1 || projectCount > 0)
                    {
                        _log.Debug($"Selecting {nameof(MultipleTestDomainRunner)} for {package.Name}");
                        return new MultipleTestDomainRunner(ServiceContext, package);
                    }
                    else
                    {
                        _log.Debug($"Selecting {nameof(TestDomainRunner)} for {package.Name}");
                        return new TestDomainRunner(ServiceContext, package);
                    }

                case DomainUsage.Multiple:
                    _log.Debug($"Selecting {nameof(MultipleTestDomainRunner)} for {package.Name}");
                    return new MultipleTestDomainRunner(ServiceContext, package);

                case DomainUsage.None:
                    _log.Debug($"Selecting {nameof(LocalTestRunner)} for {package.Name}");
                    return new LocalTestRunner(ServiceContext, package);

                case DomainUsage.Single:
                    _log.Debug($"Selecting {nameof(TestDomainRunner)} for {package.Name}");
                    return new TestDomainRunner(ServiceContext, package);
            }
        }

        public virtual bool CanReuse(ITestEngineRunner runner, TestPackage package)
        {
            return false;
        }
    }
}
