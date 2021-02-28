// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;

namespace NUnit.Engine
{
    /// <summary>
    /// ITestEngine represents an instance of the test engine.
    /// Clients wanting to discover, explore or run tests start
    /// require an instance of the engine, which is generally
    /// acquired from the TestEngineActivator class.
    /// </summary>
    public interface ITestEngine : IDisposable
    {
        /// <summary>
        /// Gets the IServiceLocator interface, which gives access to
        /// certain services provided by the engine.
        /// </summary>
        IServiceLocator Services { get; }

        /// <summary>
        /// Gets and sets the directory path used by the engine for saving files.
        /// Some services may ignore changes to this path made after initialization.
        /// The default value is the current directory.
        /// </summary>
        string WorkDirectory { get; set;  }

        /// <summary>
        /// Gets and sets the InternalTraceLevel used by the engine. Changing this
        /// setting after initialization will have no effect. The default value
        /// is the value saved in the NUnit settings.
        /// </summary>
        InternalTraceLevel InternalTraceLevel { get; set; }

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
        void Initialize();

        /// <summary>
        /// Returns a test runner instance for use by clients in discovering,
        /// exploring and executing tests.
        /// </summary>
        /// <param name="package">The TestPackage for which the runner is intended.</param>
        /// <returns>An ITestRunner.</returns>
        ITestRunner GetRunner(TestPackage package);
    }
}
