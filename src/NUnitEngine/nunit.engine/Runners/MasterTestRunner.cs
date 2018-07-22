// ***********************************************************************
// Copyright (c) 2011-2014 Charlie Poole, Rob Prouse
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;
using System.ComponentModel;

namespace NUnit.Engine.Runners
{
    public class MasterTestRunner : ITestRunner
    {
        private const string TEST_RUN_ELEMENT = "test-run";
        private readonly ITestEngineRunner _engineRunner;
        private readonly IServiceLocator _services;
        private readonly IRuntimeFrameworkService _runtimeService;
        private readonly ExtensionService _extensionService;
        private readonly IProjectService _projectService;
        private bool _disposed;

        public MasterTestRunner(IServiceLocator services, TestPackage package)
        {
            if (services == null) throw new ArgumentNullException("services");
            if (package == null) throw new ArgumentNullException("package");

            _services = services;
            TestPackage = package;

            // Get references to the services we use
            _projectService = _services.GetService<IProjectService>();
            _runtimeService = _services.GetService<IRuntimeFrameworkService>();
            _extensionService = _services.GetService<ExtensionService>();
            _engineRunner = _services.GetService<ITestRunnerFactory>().MakeTestRunner(package);

            InitializePackage();
        }

        #region Properties

        /// <summary>
        /// The TestPackage for which this is the runner
        /// </summary>
        protected TestPackage TestPackage { get; set; }

        /// <summary>
        /// The result of the last call to LoadPackage
        /// </summary>
        protected TestEngineResult LoadResult { get; set; }

        /// <summary>
        /// Gets an indicator of whether the package has been loaded.
        /// </summary>
        protected bool IsPackageLoaded
        {
            get { return LoadResult != null; }
        }

        #endregion

        #region ITestRunner Members

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
            LoadResult = _engineRunner.Load();
            return LoadResult.IsSingle ? LoadResult.Xml : null;
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
            LoadResult = _engineRunner.Reload();
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
            return _engineRunner.CountTestCases(filter);
        }

        /// <summary>
        /// Run the tests in a loaded TestPackage. The explicit
        /// implementation returns an ITestEngineResult for use
        /// by external clients.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An XmlNode giving the result of the test execution</returns>
        public XmlNode Run(ITestEventListener listener, TestFilter filter)
        {
            return PrepareResult(RunTests(listener, filter)).Xml;
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">The listener that is notified as the run progresses</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns></returns>
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
            _engineRunner.StopRun(force);
        }

        /// <summary>
        /// Explore a loaded TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An XmlNode representing the tests found.</returns>
        public XmlNode Explore(TestFilter filter)
        {
            LoadResult = PrepareResult(_engineRunner.Explore(filter))
                .Aggregate(TEST_RUN_ELEMENT, TestPackage.Name, TestPackage.FullName);

            return LoadResult.Xml;
        }

        #endregion

        #region IDisposable

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
                if (disposing && _engineRunner != null)
                    _engineRunner.Dispose();

                _disposed = true;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check the package settings, expand projects and
        /// determine what runtimes may be needed.
        /// </summary>
        private void InitializePackage()
        {
            // Last chance to catch invalid settings in package,
            // in case the client runner missed them.
            ValidatePackageSettings();

            // Some files in the top level package may be projects.
            // Expand them so that they contain subprojects for
            // each contained assembly.
            EnsurePackagesAreExpanded(TestPackage);

            // Use SelectRuntimeFramework for its side effects.
            // Info will be left behind in the package about
            // each contained assembly, which will subsequently
            // be used to determine how to run the assembly.
            _runtimeService.SelectRuntimeFramework(TestPackage);

            var processModel = TestPackage.GetSetting(EnginePackageSettings.ProcessModel, "").ToLower();

            if (IntPtr.Size == 8 && (processModel == "inprocess" || processModel == "single")  &&
                TestPackage.GetSetting(EnginePackageSettings.RunAsX86, false))
            {
                throw new NUnitEngineException("Cannot run tests in process - a 32 bit process is required.");
            }
        }

        private TestEngineResult PrepareResult(TestEngineResult result)
        {
            if (result == null) throw new ArgumentNullException("result");

            if (!IsProjectPackage(TestPackage))
            {
                return result;
            }

            return result.MakePackageResult(TestPackage.Name, TestPackage.FullName);
        }

        private void EnsurePackagesAreExpanded(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException("package");

            foreach (var subPackage in package.SubPackages)
            {
                EnsurePackagesAreExpanded(subPackage);
            }

            if (package.SubPackages.Count == 0 && IsProjectPackage(package))
            {
                ExpandProjects(package);
            }
        }

        private bool IsProjectPackage(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException("package");

            return 
                _projectService != null
                && !string.IsNullOrEmpty(package.FullName)
                && _projectService.CanLoadFrom(package.FullName);
        }

        private void ExpandProjects(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException("package");

            string packageName = package.FullName;
            if (File.Exists(packageName) && _projectService.CanLoadFrom(packageName))
            {
                _projectService.ExpandProjectPackage(package);
            }
        }

        // Any Errors thrown from this method indicate that the client
        // runner is putting invalid values into the package.
        private void ValidatePackageSettings()
        {
            var frameworkSetting = TestPackage.GetSetting(EnginePackageSettings.RuntimeFramework, "");
            if (frameworkSetting.Length > 0)
            {
                // Check requested framework is actually available
                var runtimeService = _services.GetService<IRuntimeFrameworkService>();
                if (!runtimeService.IsAvailable(frameworkSetting))
                    throw new NUnitEngineException(string.Format("The requested framework {0} is unknown or not available.", frameworkSetting));

                // If running in process, check requested framework is compatible
                var processModel = TestPackage.GetSetting(EnginePackageSettings.ProcessModel, "Default").ToLower();
                if (processModel == "single" || processModel == "inprocess")
                {
                    var currentFramework = RuntimeFramework.CurrentFramework;

                    RuntimeFramework requestedFramework;
                    if (!RuntimeFramework.TryParse(frameworkSetting, out requestedFramework))
                        throw new NUnitEngineException("Invalid or unknown framework requested: " + frameworkSetting);

                    if (!currentFramework.Supports(requestedFramework))
                        throw new NUnitEngineException(string.Format(
                            "Cannot run {0} framework in process already running {1}.", frameworkSetting, currentFramework));
                }
            }
        }

        /// <summary>
        /// Unload any loaded TestPackage.
        /// </summary>
        private void UnloadPackage()
        {
            LoadResult = null;
            if (_engineRunner != null)
                _engineRunner.Unload();
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
            if (!IsPackageLoaded) return 0;

            return _engineRunner.CountTestCases(filter);
        }

        /// <summary>
        /// Run the tests in the loaded TestPackage and return a test result. The tests
        /// are run synchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        private TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
        {
            var eventDispatcher = new TestEventDispatcher();
            if (listener != null)
                eventDispatcher.Listeners.Add(listener);
            foreach (var extension in _extensionService.GetExtensions<ITestEventListener>())
                eventDispatcher.Listeners.Add(extension);

            IsTestRunning = true;

            eventDispatcher.OnTestEvent(string.Format("<start-run count='{0}'/>", CountTests(filter)));

            DateTime startTime = DateTime.UtcNow;
            long startTicks = Stopwatch.GetTimestamp();

            TestEngineResult result = _engineRunner.Run(eventDispatcher, filter).Aggregate("test-run", TestPackage.Name, TestPackage.FullName);

            // These are inserted in reverse order, since each is added as the first child.
            InsertFilterElement(result.Xml, filter);
            InsertCommandLineElement(result.Xml);

            result.Xml.AddAttribute("engine-version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            result.Xml.AddAttribute("clr-version", Environment.Version.ToString());

            double duration = (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
            result.Xml.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            result.Xml.AddAttribute("end-time", XmlConvert.ToString(DateTime.UtcNow, "u"));
            result.Xml.AddAttribute("duration", duration.ToString("0.000000", NumberFormatInfo.InvariantInfo));

            IsTestRunning = false;

            eventDispatcher.OnTestEvent(result.Xml.OuterXml);

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

            if (doc == null)
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
            if (doc == null)
            {
                return;
            }

            var filterElement = doc.ImportNode(tempNode, true);
            resultNode.InsertAfter(filterElement, null);
        }

        #endregion
    }
}
