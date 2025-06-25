// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NSubstitute;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// FakeServiceContext, in spite of it's name, is derived from the
    /// real ServiceContext but allows various components to be replaced.
    /// </summary>
    /// <remarks>
    /// If any of the services is not initialized by the calling code,
    /// a Substitute is automatically created. ExtensionService is
    /// currently an exception: the actual class is used because the
    /// current engine code relies on several non-interface methods.
    /// </remarks>
    public class FakeServiceContext : ServiceContext
    {
        public ITestFilterService? TestFilterService { get; set; }
        // TODO: Try to eliminate the need for using the ExtensionService class
        public ExtensionService? ExtensionService { get; set; }
        public IProjectService? ProjectService { get; set; }
#if NETFRAMEWORK
        public IRuntimeFrameworkService? RuntimeFrameworkService { get; set; }
        public ITestAgency? TestAgency { get; set; }
#endif

        private readonly Lazy<IResultService> _resultService =
            new(() => Substitute.For<IResultService, IService>());
        public IResultService ResultService => _resultService.Value;

        private readonly Lazy<ITestRunnerFactory> _testRunnerFactory =
            new(() => Substitute.For<ITestRunnerFactory, IService>());
        public ITestRunnerFactory TestRunnerFactory => _testRunnerFactory.Value;

#pragma warning disable CS0436 // Type conflicts with imported type
        [MemberNotNull(
#pragma warning restore CS0436 // Type conflicts with imported type
#if NETFRAMEWORK
            nameof(TestFilterService), nameof(ExtensionService), nameof(ProjectService),
            nameof(RuntimeFrameworkService), nameof(TestAgency))]
#else
            nameof(TestFilterService), nameof(ExtensionService), nameof(ProjectService))]
#endif
        public void Initialize()
        {
            if (TestFilterService is null)
                TestFilterService = Substitute.For<ITestFilterService, IService>();
            Add((IService)TestFilterService);
            if (ExtensionService is null)
                ExtensionService = new ExtensionService();
            Add(ExtensionService);
            if (ProjectService is null)
                ProjectService = Substitute.For<IProjectService, IService>();
            Add((IService)ProjectService);
#if NETFRAMEWORK
            if (RuntimeFrameworkService is null)
                RuntimeFrameworkService = Substitute.For<IRuntimeFrameworkService, IService>();
            Add((IService)RuntimeFrameworkService);
            if (TestAgency is null)
                TestAgency = Substitute.For<ITestAgency, IAvailableRuntimes, IService>();
            Add((IService)TestAgency);
#endif
            Add((IService)ResultService);
            Add((IService)TestRunnerFactory);

            ServiceManager.StartServices();
        }
    }
}