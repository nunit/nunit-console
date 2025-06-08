// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Xml;
using NUnit.Engine.Services;
using System.ComponentModel;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// MasterTestRunner implements the ITestRunner interface, which
    /// is the user-facing representation of a test runner. It uses
    /// various internal runners to load and run tests for the user.
    /// </summary>
    public class MasterTestRunner : ITestRunner
    {
        // MasterTestRunner is the only runner that is passed back
        // to users asking for an ITestRunner. The actual details of
        // execution are handled by various internal runners, which
        // implement ITestEngineRunner.
        //
        // Explore and execution results from MasterTestRunner are
        // returned as XmlNodes, created from the internal
        // TestEngineResult representation.
        //
        // MasterTestRunner is responsible for creating the test-run
        // element, which wraps all the individual assembly and project
        // results.

        private ITestEngineRunner? _engineRunner;
        private readonly IServiceLocator _services;
        private readonly ExtensionService _extensionService;
#if NETFRAMEWORK
        private readonly IRuntimeFrameworkService _runtimeService;
#endif
        private readonly IProjectService _projectService;
        private ITestRunnerFactory _testRunnerFactory;
        private bool _disposed;

        private TestEventDispatcher _eventDispatcher = new TestEventDispatcher();
        private WorkItemTracker _workItemTracker = new WorkItemTracker();

        private const int WAIT_FOR_CANCEL_TO_COMPLETE = 5000;

        public MasterTestRunner(IServiceLocator services, TestPackage package)
        {
            Guard.ArgumentNotNull(services);
            Guard.ArgumentNotNull(package);

            _services = services;

            // Get references to the services we use
            _projectService = _services.GetService<IProjectService>();
            _testRunnerFactory = _services.GetService<ITestRunnerFactory>();
#if NETFRAMEWORK
            _runtimeService = _services.GetService<IRuntimeFrameworkService>();
#endif
            _extensionService = _services.GetService<ExtensionService>();

            // Some files in the top level package may be projects.
            // Expand them so that they contain subprojects for
            // each contained assembly.
            EnsurePackagesAreExpanded(package);

            TestPackage = package;
            TestPackages = package.Select(p => !p.HasSubPackages());

            // Last chance to catch invalid settings in package,
            // in case the client runner missed them.
            ValidatePackageSettings();
        }

        /// <summary>
        /// The Top-level TestPackage for which this is the runner.
        /// </summary>
        protected TestPackage TestPackage { get; }

        /// <summary>
        /// A list of all leaf packages, i.e. packages contained
        /// under the top-level package, which have no subpackages.
        /// </summary>
        protected IList<TestPackage> TestPackages { get; }

        /// <summary>
        /// The result of the last call to LoadPackage
        /// </summary>
        protected TestEngineResult? LoadResult { get; set; }

        /// <summary>
        /// Gets an indicator of whether the package has been loaded.
        /// </summary>
        protected bool IsPackageLoaded
        {
            get { return LoadResult is not null; }
        }

        /// <summary>
        /// Get a flag indicating whether a test is running
        /// </summary>
        public bool IsTestRunning { get; private set; }

        /// <summary>
        /// Load a TestPackage for possible execution. The
        /// explicit implementation returns an ITestEngineResult
        /// for consumption by clients.
        /// </summary>
        /// <returns>An XmlNode representing the loaded assembly.</returns>
        public XmlNode Load()
        {
            LoadResult = PrepareResult(GetEngineRunner().Load()).MakeTestRunResult(TestPackage);

            return LoadResult.Xml;
        }

        /// <summary>
        /// Unload any loaded TestPackage. If none is loaded,
        /// the call is ignored.
        /// </summary>
        public void Unload()
        {
            UnloadPackage();
        }

        /// <summary>
        /// Reload the currently loaded test package.
        /// </summary>
        /// <returns>An XmlNode representing the loaded package</returns>
        /// <exception cref="InvalidOperationException">If no package has been loaded</exception>
        public XmlNode Reload()
        {
            LoadResult = PrepareResult(GetEngineRunner().Reload()).MakeTestRunResult(TestPackage);

            return LoadResult.Xml;
        }

        /// <summary>
        /// Count the test cases that would be run under the specified
        /// filter, loading the TestPackage if it is not already loaded.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases.</returns>
        public int CountTestCases(TestFilter filter)
        {
            return GetEngineRunner().CountTestCases(filter);
        }

        /// <summary>
        /// Run the tests in a loaded TestPackage. The explicit
        /// implementation returns an ITestEngineResult for use
        /// by external clients.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An XmlNode giving the result of the test execution</returns>
        public XmlNode Run(ITestEventListener? listener, TestFilter filter)
        {
            return RunTests(listener, filter).Xml;
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">The listener that is notified as the run progresses</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        public ITestRun RunAsync(ITestEventListener listener, TestFilter filter)
        {
            return RunTestsAsync(listener, filter);
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            if (_engineRunner is null)
                return; // No test is was even started.

            if (!force)
                _engineRunner.RequestStop();
            else
            {
                _engineRunner.ForcedStop();

                // Frameworks should handle StopRun(true) by cancelling all tests and notifying
                // us of the completion of any tests that were running. However, this feature
                // may be absent in some frameworks or may be broken and we may not pass on the
                // notifications needed by some runners. In fact, such a bug is present in the
                // NUnit framework through release 3.12 and motivated the following code.
                //
                // We try to make up for the potential problem here by notifying the listeners
                // of the completion of every pending WorkItem, that is, one that started but
                // never sent a completion event.

                if (!_workItemTracker.WaitForCompletion(WAIT_FOR_CANCEL_TO_COMPLETE))
                {
                    _workItemTracker.SendPendingTestCompletionEvents(_eventDispatcher);

                    // Indicate we are no longer running
                    IsTestRunning = false;

                    // Signal completion of the run
                    _eventDispatcher.OnTestEvent($"<test-run id='{TestPackage.ID}' result='Failed' label='Cancelled' />");

                    // Since we were not notified of the completion of some items, we can't trust
                    // that they were actually stopped by the framework. To make sure nothing is
                    // left running, we unload the tests. By unloading only the lower-level engine
                    // runner and not the MasterTestRunner itself, we allow the tests to be loaded

                    _engineRunner.Unload();
                }
            }
        }

        /// <summary>
        /// Explore a loaded TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An XmlNode representing the tests found.</returns>
        public XmlNode Explore(TestFilter filter)
        {
            LoadResult = PrepareResult(GetEngineRunner().Explore(filter))
                .MakeTestRunResult(TestPackage);

            return LoadResult.Xml;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose of this object.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _engineRunner is not null)
                    _engineRunner.Dispose();

                _disposed = true;
            }
        }

        //Exposed for testing
        internal ITestEngineRunner GetEngineRunner()
        {
            if (_engineRunner is null)
            {
                // Analyze each TestPackage, adding settings that describe
                // each contained assembly, including it's target runtime.
#if NETFRAMEWORK
                foreach (var assemblyPackage in TestPackages)
                    _runtimeService.SelectRuntimeFramework(assemblyPackage);
#endif

                _engineRunner = _testRunnerFactory.MakeTestRunner(TestPackage);
            }

            return _engineRunner;
        }

        // The TestEngineResult returned to MasterTestRunner contains no info
        // about projects. At this point, if there are any projects, the result
        // needs to be modified to include info about them. Doing it this way
        // allows the lower-level runners to be completely ignorant of projects
        private TestEngineResult PrepareResult(TestEngineResult result)
        {
            Guard.ArgumentNotNull(result);

            // See if we have any projects to deal with. At this point,
            // any subpackage, which itself has subpackages, is a project
            // we expanded.
            bool hasProjects = false;
            foreach (var p in TestPackage.SubPackages)
                hasProjects |= p.HasSubPackages();

            // If no Projects, there's nothing to do
            if (!hasProjects)
                return result;

            // If there is just one subpackage, it has to be a project and we don't
            // need to rebuild the XML but only wrap it with a project result.
            if (TestPackage.SubPackages.Count == 1)
                return result.MakeProjectResult(TestPackage.SubPackages[0]);

            // Most complex case - we need to work with the XML in order to
            // examine and rebuild the result to include project nodes.
            // NOTE: The algorithm used here relies on the ordering of nodes in the
            // result matching the ordering of subpackages under the top-level package.
            // If that should change in the future, then we would need to implement
            // identification and summarization of projects into each of the lower-
            // level TestEngineRunners. In that case, we will be warned by failures
            // of some of the MasterTestRunnerTests.

            // Start a fresh TestEngineResult for top level
            var topLevelResult = new TestEngineResult();
            int nextTest = 0;

            foreach (var subPackage in TestPackage.SubPackages)
            {
                if (subPackage.HasSubPackages())
                {
                    // This is a project, create an intermediate result
                    var projectResult = new TestEngineResult();

                    // Now move any children of this project under it. As noted
                    // above, we must rely on ordering here because (1) the
                    // fullname attribute is not reliable on all nunit framework
                    // versions, (2) we may have duplicates of the same assembly
                    // and (3) we have no info about the id of each assembly.
                    int numChildren = subPackage.SubPackages.Count;
                    while (numChildren-- > 0)
                        projectResult.Add(result.XmlNodes[nextTest++]);

                    topLevelResult.Add(projectResult.MakeProjectResult(subPackage).Xml);
                }
                else
                {
                    // Add the next assembly package to our new result
                    topLevelResult.Add(result.XmlNodes[nextTest++]);
                }
            }

            return topLevelResult;
        }

        private void EnsurePackagesAreExpanded(TestPackage package)
        {
            Guard.ArgumentNotNull(package);

            foreach (var subPackage in package.SubPackages)
            {
                EnsurePackagesAreExpanded(subPackage);
            }

            if (package.SubPackages.Count == 0 && IsProjectPackage(package))
            {
                _projectService.ExpandProjectPackage(package);
            }
        }

        private bool IsProjectPackage(TestPackage package)
        {
            Guard.ArgumentNotNull(package);

            return
                _projectService is not null
                && !string.IsNullOrEmpty(package.FullName)
                && _projectService.CanLoadFrom(package.FullName);
        }

        // Any Errors thrown from this method indicate that the client
        // runner is putting invalid values into the package.
        private static void ValidatePackageSettings()
        {
            // No settings to validate at this time
        }

        /// <summary>
        /// Unload any loaded TestPackage.
        /// </summary>
        private void UnloadPackage()
        {
            LoadResult = null;
            _engineRunner?.Unload();
        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter. Returns zero if the
        /// package has not yet been loaded.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        private int CountTests(TestFilter filter)
        {
            if (!IsPackageLoaded)
                return 0;

            return GetEngineRunner().CountTestCases(filter);
        }

        /// <summary>
        /// Run the tests in the loaded TestPackage and return a test result. The tests
        /// are run synchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        private TestEngineResult RunTests(ITestEventListener? listener, TestFilter filter)
        {
            _workItemTracker.Clear();
            _eventDispatcher.Listeners.Clear();
            _eventDispatcher.Listeners.Add(_workItemTracker);

            if (listener is not null)
                _eventDispatcher.Listeners.Add(listener);

            foreach (var extension in _extensionService.GetExtensions<ITestEventListener>())
                _eventDispatcher.Listeners.Add(extension);

            IsTestRunning = true;

            string clrVersion;
            string engineVersion;

            clrVersion = Environment.Version.ToString();
            engineVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";

            var startTime = DateTime.UtcNow;
            var startRunNode = XmlHelper.CreateTopLevelElement("start-run");
            startRunNode.AddAttribute("count", CountTests(filter).ToString());
            startRunNode.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            startRunNode.AddAttribute("engine-version", engineVersion);
            startRunNode.AddAttribute("clr-version", clrVersion);

            InsertCommandLineElement(startRunNode);

            _eventDispatcher.OnTestEvent(startRunNode.OuterXml);

            long startTicks = Stopwatch.GetTimestamp();

            TestEngineResult result = PrepareResult(GetEngineRunner().Run(_eventDispatcher, filter)).MakeTestRunResult(TestPackage);

            // These are inserted in reverse order, since each is added as the first child.
            InsertFilterElement(result.Xml, filter);

            InsertCommandLineElement(result.Xml);

            result.Xml.AddAttribute("engine-version", engineVersion);
            result.Xml.AddAttribute("clr-version", clrVersion);
            double duration = (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
            result.Xml.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            result.Xml.AddAttribute("end-time", XmlConvert.ToString(DateTime.UtcNow, "u"));
            result.Xml.AddAttribute("duration", duration.ToString("0.000000", NumberFormatInfo.InvariantInfo));

            IsTestRunning = false;

            _eventDispatcher.OnTestEvent(result.Xml.OuterXml);

            return result;
        }

        private AsyncTestEngineResult RunTestsAsync(ITestEventListener listener, TestFilter filter)
        {
            var testRun = new AsyncTestEngineResult();

            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (s, ea) =>
                {
                    var result = RunTests(listener, filter);
                    testRun.SetResult(result);
                };

                worker.RunWorkerAsync();
            }

            return testRun;
        }

        private static void InsertCommandLineElement(XmlNode resultNode)
        {
            var doc = resultNode.OwnerDocument;

            if (doc is null)
            {
                return;
            }

            XmlNode cmd = doc.CreateElement("command-line");
            resultNode.InsertAfter(cmd, null);

            var cdata = doc.CreateCDataSection(Environment.CommandLine);
            cmd.AppendChild(cdata);
        }

        private static void InsertFilterElement(XmlNode resultNode, TestFilter filter)
        {
            // Convert the filter to an XmlNode
            var tempNode = XmlHelper.CreateXmlNode(filter.Text);

            // Don't include it if it's an empty filter
            if (tempNode.ChildNodes.Count <= 0)
            {
                return;
            }

            var doc = resultNode.OwnerDocument;
            if (doc is null)
            {
                return;
            }

            var filterElement = doc.ImportNode(tempNode, true);
            resultNode.InsertAfter(filterElement, null);
        }
    }
}
