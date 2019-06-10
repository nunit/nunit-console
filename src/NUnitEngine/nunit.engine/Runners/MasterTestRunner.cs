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
        // impement ITestEngineRunner.
        //
        // Explore and execution results from MasterTestRunner are
        // returned as XmlNodes, created from the internal 
        // TestEngineResult representation.
        // 
        // MasterTestRUnner is responsible for creating the test-run
        // element, which wraps all the individual assembly and project
        // results.

        private ITestEngineRunner _engineRunner;
        private readonly IServiceLocator _services;
#if !NETSTANDARD1_6
        private readonly ExtensionService _extensionService;
#if !NETSTANDARD2_0
        private readonly IRuntimeFrameworkService _runtimeService;
#endif
#endif
        private readonly IProjectService _projectService;
        private ITestRunnerFactory _testRunnerFactory;
        private bool _disposed;

        public MasterTestRunner(IServiceLocator services, TestPackage package)
        {
            if (services == null) throw new ArgumentNullException("services");
            if (package == null) throw new ArgumentNullException("package");

            _services = services;
            TestPackage = package;

            // Get references to the services we use
            _projectService = _services.GetService<IProjectService>();
            _testRunnerFactory = _services.GetService<ITestRunnerFactory>();

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
            _runtimeService = _services.GetService<IRuntimeFrameworkService>();
#endif
#if !NETSTANDARD1_6
            _extensionService = _services.GetService<ExtensionService>();
#endif

            // Last chance to catch invalid settings in package,
            // in case the client runner missed them.
            ValidatePackageSettings();
        }

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
        public XmlNode Run(ITestEventListener listener, TestFilter filter)
        {
            return RunTests(listener, filter).Xml;
        }


#if !NETSTANDARD1_6
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
#endif

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
                if (disposing && _engineRunner != null)
                    _engineRunner.Dispose();

                _disposed = true;
            }
        }

        //Exposed for testing
        internal ITestEngineRunner GetEngineRunner()
        {
            if (_engineRunner == null)
            {
                // Some files in the top level package may be projects.
                // Expand them so that they contain subprojects for
                // each contained assembly.
                EnsurePackagesAreExpanded(TestPackage);

                // Use SelectRuntimeFramework for its side effects.
                // Info will be left behind in the package about
                // each contained assembly, which will subsequently
                // be used to determine how to run the assembly.
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
                _runtimeService.SelectRuntimeFramework(TestPackage);
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
            if (result == null) throw new ArgumentNullException("result");

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
            if (package == null) throw new ArgumentNullException("package");

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
            if (package == null) throw new ArgumentNullException("package");

            return
                _projectService != null
                && !string.IsNullOrEmpty(package.FullName)
                && _projectService.CanLoadFrom(package.FullName);
        }

        // Any Errors thrown from this method indicate that the client
        // runner is putting invalid values into the package.
        private void ValidatePackageSettings()
        {
#if !NETSTANDARD1_6 && !NETSTANDARD2_0  // TODO: How do we validate runtime framework for .NET Standard 2.0?
            var processModel = TestPackage.GetSetting(EnginePackageSettings.ProcessModel, "Default").ToLower();
            var runningInProcess = processModel == "single" || processModel == "inprocess";
            var frameworkSetting = TestPackage.GetSetting(EnginePackageSettings.RuntimeFramework, "");
            var runAsX86 = TestPackage.GetSetting(EnginePackageSettings.RunAsX86, false);

            if (frameworkSetting.Length > 0)
            {
                // Check requested framework is actually available
                var runtimeService = _services.GetService<IRuntimeFrameworkService>();
                if (!runtimeService.IsAvailable(frameworkSetting))
                    throw new NUnitEngineException(string.Format("The requested framework {0} is unknown or not available.", frameworkSetting));

                // If running in process, check requested framework is compatible
                if (runningInProcess)
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

            if (runningInProcess && runAsX86 && IntPtr.Size == 8)
                throw new NUnitEngineException("Cannot run tests in process - a 32 bit process is required.");
#endif
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

            return GetEngineRunner().CountTestCases(filter);
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
#if !NETSTANDARD1_6
            foreach (var extension in _extensionService.GetExtensions<ITestEventListener>())
                eventDispatcher.Listeners.Add(extension);
#endif

            IsTestRunning = true;
            
            string clrVersion;
            string engineVersion;

#if !NETSTANDARD1_6
            clrVersion = Environment.Version.ToString();
            engineVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#else
            clrVersion =  Microsoft.DotNet.InternalAbstractions.RuntimeEnvironment.GetRuntimeIdentifier();
            engineVersion = typeof(MasterTestRunner).GetTypeInfo().Assembly.GetName().Version.ToString();
#endif

            var startTime = DateTime.UtcNow;
            var startRunNode = XmlHelper.CreateTopLevelElement("start-run");
            startRunNode.AddAttribute("count", CountTests(filter).ToString());
            startRunNode.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            startRunNode.AddAttribute("engine-version", engineVersion);
            startRunNode.AddAttribute("clr-version", clrVersion);

#if !NETSTANDARD1_6
            InsertCommandLineElement(startRunNode);
#endif

            eventDispatcher.OnTestEvent(startRunNode.OuterXml);

            long startTicks = Stopwatch.GetTimestamp();

            TestEngineResult result = PrepareResult(GetEngineRunner().Run(eventDispatcher, filter)).MakeTestRunResult(TestPackage);

            // These are inserted in reverse order, since each is added as the first child.
            InsertFilterElement(result.Xml, filter);

#if !NETSTANDARD1_6
            InsertCommandLineElement(result.Xml);
#endif

            result.Xml.AddAttribute("engine-version", engineVersion);
            result.Xml.AddAttribute("clr-version", clrVersion);
            double duration = (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
            result.Xml.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            result.Xml.AddAttribute("end-time", XmlConvert.ToString(DateTime.UtcNow, "u"));
            result.Xml.AddAttribute("duration", duration.ToString("0.000000", NumberFormatInfo.InvariantInfo));

            IsTestRunning = false;

            eventDispatcher.OnTestEvent(result.Xml.OuterXml);

            return result;
        }

#if !NETSTANDARD1_6
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
#endif

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
    }
}
