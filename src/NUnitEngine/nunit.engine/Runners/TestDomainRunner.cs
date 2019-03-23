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

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestDomainRunner loads and runs tests in a separate
    /// domain whose lifetime it controls.
    /// </summary>
    public class TestDomainRunner : DirectTestRunner
    {
        private DomainManager _domainManager;

        public TestDomainRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
            _domainManager = Services.GetService<DomainManager>();
        }

        protected override TestEngineResult LoadPackage()
        {
            TestDomain = _domainManager.CreateDomain(TestPackage);

            return base.LoadPackage();
        }

        /// <summary>
        /// Unload any loaded TestPackage as well as the application domain.
        /// </summary>
        public override void UnloadPackage()
        {
            var testDomain = TestDomain;
            if (testDomain != null)
            {
                /* Important: make sure TestDomain is null before unloading it.

                Mono 5.18.0.240 somehow manages reentry *during* this UnloadPackage call and does a second call to _domainManager.Unload before this call returns.

                Note the _ThreadPoolWaitCallback.PerformWaitCallback frame right above the first TestDomainRunner.UnloadPackage frame.

                I ran out of time trying to figure out how this stack trace is even possible, but I did verify that:
                 - it's the same TestDomainRunner instance
                 - it's the same managed thread ID
                 - the reentry isn't happening during the Thread.Join call in DomainUnloader.Unload because the problem still occurs with the same stack trace
                   when it calls UnloadOnThread directly rather than starting and joining a thread.

                The reentry must be caused by the AppDomain.Unload call somehow?
                Looks like Environment.Exit can have a similar reentry effect: https://github.com/SignalR/SignalR/issues/3101
                But I don't understand it at all and can't reproduce it on Windows.

                    at System.Environment.get_StackTrace () [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at NUnit.Engine.Runners.TestDomainRunner.UnloadPackage () [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Unload () [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Dispose (System.Boolean disposing) [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Dispose () [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Agent.AgentServerConnection.Dispose () [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Agent.AgentServer.RunConnectionSynchronously (System.Object state) [0x00000] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at System.Threading.QueueUserWorkItemCallback.WaitCallback_Context (System.Object state) [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ExecutionContext.RunInternal (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ExecutionContext.Run (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem () [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ThreadPoolWorkQueue.Dispatch () [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading._ThreadPoolWaitCallback.PerformWaitCallback () [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at NUnit.Engine.Runners.TestDomainRunner.UnloadPackage () [0x0008b] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Unload () [0x00001] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Dispose (System.Boolean disposing) [0x00014] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Runners.AbstractTestRunner.Dispose () [0x00001] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Agent.AgentServerConnection.Dispose () [0x0000b] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at NUnit.Engine.Agent.AgentServer.RunConnectionSynchronously (System.Object state) [0x00030] in <f769c39a7c7b4dd6a9239413413817b2>:0
                    at System.Threading.QueueUserWorkItemCallback.WaitCallback_Context (System.Object state) [0x00007] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ExecutionContext.RunInternal (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00071] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ExecutionContext.Run (System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, System.Object state, System.Boolean preserveSyncCtx) [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.QueueUserWorkItemCallback.System.Threading.IThreadPoolWorkItem.ExecuteWorkItem () [0x00021] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading.ThreadPoolWorkQueue.Dispatch () [0x00074] in <04750267503a43e5929c1d1ba19daf3e>:0
                    at System.Threading._ThreadPoolWaitCallback.PerformWaitCallback () [0x00000] in <04750267503a43e5929c1d1ba19daf3e>:0
                */

                TestDomain = null;

                _domainManager.Unload(testDomain);
            }
        }
    }
}
#endif