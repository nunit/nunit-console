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

namespace NUnit.Engine
{
    /// <summary>
    /// Defines the current communication protocol between the engine and the agent. This is an implementation detail
    /// which is in the process of changing as the dependency on .NET Framework's remoting is removed.
    /// </summary>
    public interface ITestAgent
    {
        // Once this is the last member of ITestAgent and the same is done for ITestAgency, remoting can be trivially
        // replaced.
        byte[] SendMessage(byte[] message);

        /// <summary>
        /// Unloads any loaded package. If none is loaded, the call is ignored.
        /// </summary>
        void Unload();

        /// <summary>
        /// Reloads the loaded package.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no package is loaded.</exception>
        TestEngineResult Reload();

        /// <summary>
        /// Counts the test cases in the loaded package that would be run under the specified filter.
        /// </summary>
        int CountTestCases(TestFilter filter);

        /// <summary>
        /// Runs the tests in the loaded package. The listener interface is notified as the run progresses.
        /// </summary>
        TestEngineResult Run(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Cancel the current test run. If no test is running, the call is ignored.
        /// </summary>
        /// <param name="force">Indicates whether tests that have not completed should be killed.</param>
        void StopRun(bool force);

        /// <summary>
        /// Returns information about the test cases in the loaded package that would be run under the specified filter.
        /// </summary>
        TestEngineResult Explore(TestFilter filter);
    }
}
