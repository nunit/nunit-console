// ***********************************************************************
// Copyright (c) 2011-2014 Charlie Poole
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
using System.ComponentModel;
using NUnit.Engine.Services;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Runners
{
    public class MasterTestRunner : ITestRunner
    {
        private const string TEST_RUN_ELEMENT = "test-run";
        private readonly ITestEngineRunner _engineRunner;
        private bool _disposed;

        public MasterTestRunner(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException("package");

            TestPackage = package;

            // Get references to the services we use
            var runnerFactory = new DefaultTestRunnerFactory();
            _engineRunner = runnerFactory.MakeTestRunner(package);

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
        }

        private void ValidatePackageSettings()
        {
            // TODO: Any package settings to validate?
        }

        private TestEngineResult PrepareResult(TestEngineResult result)
        {
            if (result == null) throw new ArgumentNullException("result");

            return result.MakePackageResult(TestPackage.Name, TestPackage.FullName);
        }

        private void EnsurePackagesAreExpanded(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException("package");

            foreach (var subPackage in package.SubPackages)
            {
                EnsurePackagesAreExpanded(subPackage);
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

            IsTestRunning = true;

            eventDispatcher.OnTestEvent(string.Format("<start-run count='{0}'/>", CountTests(filter)));

            DateTime startTime = DateTime.UtcNow;
            long startTicks = Stopwatch.GetTimestamp();

            TestEngineResult result = _engineRunner.Run(eventDispatcher, filter).Aggregate("test-run", TestPackage.Name, TestPackage.FullName);

            // These are inserted in reverse order, since each is added as the first child.
            InsertFilterElement(result.Xml, filter);

            result.Xml.AddAttribute("engine-version", typeof(MasterTestRunner).GetTypeInfo().Assembly.GetName().Version.ToString());
            result.Xml.AddAttribute("clr-version", Microsoft.DotNet.InternalAbstractions.RuntimeEnvironment.GetRuntimeIdentifier());

            double duration = (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
            result.Xml.AddAttribute("start-time", XmlConvert.ToString(startTime, "u"));
            result.Xml.AddAttribute("end-time", XmlConvert.ToString(DateTime.UtcNow, "u"));
            result.Xml.AddAttribute("duration", duration.ToString("0.000000", NumberFormatInfo.InvariantInfo));

            IsTestRunning = false;

            eventDispatcher.OnTestEvent(result.Xml.OuterXml);

            return result;
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
