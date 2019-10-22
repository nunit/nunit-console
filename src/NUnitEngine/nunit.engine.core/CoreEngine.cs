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
#if NETSTANDARD1_6
            WorkDirectory = NUnitConfiguration.ApplicationDirectory;
#else
            WorkDirectory = Environment.CurrentDirectory;
#endif
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
#if !NETSTANDARD1_6
                Services.Add(new ExtensionService());
#if !NETSTANDARD2_0
                Services.Add(new DomainManager());
#endif
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
