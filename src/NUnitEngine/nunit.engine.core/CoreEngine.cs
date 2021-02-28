// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Engine
{
    /// <summary>
    /// The CoreEngine provides services that are used by both
    /// the TestEngine and agents.
    /// </summary>
    public class CoreEngine
    {
        public CoreEngine()
        {
            Services = new ServiceContext();
            WorkDirectory = Environment.CurrentDirectory;
            InternalTraceLevel = InternalTraceLevel.Default;
        }

        #region Public Properties

        public ServiceContext Services { get; private set; }

        public string WorkDirectory { get; set; }

        public InternalTraceLevel InternalTraceLevel { get; set; }

        #endregion

        #region ITestEngine Members

        ///// <summary>
        ///// Access the public IServiceLocator, first initializing
        ///// the services if that has not already been done.
        ///// </summary>
        //IServiceLocator ITestEngine.Services
        //{
        //    get
        //    {
        //        if(!Services.ServiceManager.ServicesInitialized)
        //            Initialize();

        //        return Services;
        //    }
        //}

        /// <summary>
        /// Initialize the engine. This includes initializing mono addins,
        /// setting the trace level and creating the standard set of services
        /// used in the Engine.
        ///
        /// This interface is not normally called by user code. Programs linking
        /// only to the nunit.engine.api assembly are given a
        /// pre-initialized instance of TestEngine. Programs
        /// that link directly to nunit.engine usually do so
        /// in order to perform custom initialization.
        /// </summary>
        public void InitializeServices()
        {
            if(InternalTraceLevel != InternalTraceLevel.Off && !InternalTrace.Initialized)
            {
                var logName = string.Format("InternalTrace.{0}.log", Process.GetCurrentProcess().Id);
                InternalTrace.Initialize(Path.Combine(WorkDirectory, logName), InternalTraceLevel);
            }

            // If caller added services beforehand, we don't add any
            if (Services.ServiceCount == 0)
            {
                // Services that depend on other services must be added after their dependencies
                // For example, ResultService uses ExtensionService, so ExtensionService is added
                // later.
                Services.Add(new DriverService());
                Services.Add(new ExtensionService());
#if NETFRAMEWORK
                Services.Add(new DomainManager());
#endif
                Services.Add(new InProcessTestRunnerFactory());
            }

            Services.ServiceManager.StartServices();
        }

        /// <summary>
        /// Returns a test runner for use by clients that need to load the
        /// tests once and run them multiple times. If necessary, the
        /// services are initialized first.
        /// </summary>
        /// <returns>An ITestRunner.</returns>
        //public ITestRunner GetRunner(TestPackage package)
        //{
        //    if(!Services.ServiceManager.ServicesInitialized)
        //        Initialize();

        //    return new Runners.MasterTestRunner(Services, package);
        //}

        #endregion

        #region IDisposable Members

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            Services.ServiceManager.StopServices();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Services.ServiceManager.Dispose();

                _disposed = true;
            }
        }

        #endregion
    }
}
