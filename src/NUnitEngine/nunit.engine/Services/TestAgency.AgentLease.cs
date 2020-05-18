// ***********************************************************************
// Copyright (c) 2020 Charlie Poole, Rob Prouse
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
using NUnit.Engine.Communication;
using NUnit.Engine.Communication.Model;
using System;
using System.Threading;

namespace NUnit.Engine.Services
{
    public partial class TestAgency
    {
        private sealed class AgentLease : IAgentLease, ITestEngineRunner
        {
            private readonly TestAgency _agency;
            private readonly ITestAgent _remoteAgent;
            private TestEngineResult _createRunnerLoadResult;

            public AgentLease(TestAgency agency, Guid id, ITestAgent remoteAgent)
            {
                _agency = agency;
                Id = id;
                _remoteAgent = remoteAgent;
            }

            public Guid Id { get; }

            public void Dispose()
            {
                _agency.Release(Id, _remoteAgent);
            }

            public ITestEngineRunner CreateRunner(TestPackage package)
            {
                var response = CommunicationUtils.HandleMessageResponse(
                    CommunicationUtils.SendMessage(_remoteAgent, new LoadRequest(package).Write),
                    LoadResponse.ReadBody);

                _createRunnerLoadResult = response.EngineResult;
                return this;
            }

            public int CountTestCases(TestFilter filter)
            {
                var response = CommunicationUtils.HandleMessageResponse(
                    CommunicationUtils.SendMessage(_remoteAgent, new CountTestCasesRequest(filter).Write),
                    CountTestCasesResponse.ReadBody);

                return response.Count;
            }

            public TestEngineResult Explore(TestFilter filter)
            {
                var response = CommunicationUtils.HandleMessageResponse(
                    CommunicationUtils.SendMessage(_remoteAgent, new ExploreRequest(filter).Write),
                    ExploreResponse.ReadBody);

                return response.EngineResult;
            }

            public TestEngineResult Load()
            {
                return Interlocked.Exchange(ref _createRunnerLoadResult, null)
                    ?? Reload();
            }

            public void Unload()
            {
                CommunicationUtils.HandleMessageResponse(
                    CommunicationUtils.SendMessage(_remoteAgent, new UnloadRequest().Write));
            }

            public TestEngineResult Reload()
            {
                var response = CommunicationUtils.HandleMessageResponse(
                    CommunicationUtils.SendMessage(_remoteAgent, new ReloadRequest().Write),
                    ReloadResponse.ReadBody);

                return response.EngineResult;
            }

            public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
            {
                return _remoteAgent.Run(listener, filter);
            }

            public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
            {
                return AsyncTestEngineResult.RunAsync(() => Run(listener, filter));
            }

            public void StopRun(bool force)
            {
                _remoteAgent.StopRun(force);
            }
        }
    }
}
#endif