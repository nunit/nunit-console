// ***********************************************************************
// Copyright (c) 2019 Charlie Poole, Rob Prouse
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

#if !NETSTANDARD // Dependency on RuntimeFrameworkService
using NUnit.Engine.Services;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NUnit.Engine.Agent
{
    internal sealed partial class AgentProcess : IDisposable
    {
        private readonly Process _process;
        private readonly IPEndPoint _listeningOn;

        public AgentClient Connect(TestPackage package)
        {
            if (package is null) throw new ArgumentNullException(nameof(package));

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_listeningOn);

            var client = new AgentClient(new NetworkStream(socket, ownsSocket: true));
            client.Connect(package);

            return client;
        }

        public static AgentProcess Start(TestPackage package, RuntimeFrameworkService runtimeFrameworkService)
        {
            var startInfo = CreateAgentProcessStartInfo(package, runtimeFrameworkService);

            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;

            var process = Process.Start(startInfo);

            while (true)
            {
                var line = process.StandardOutput.ReadLine()
                    ?? throw new NUnitEngineException("Agent process did not report that it was listening on a port.");

                const string prefix = "Listening on ";
                if (line.StartsWith(prefix))
                {
                    var port = int.Parse(line.Substring(line.LastIndexOf(':') + 1));

                    return new AgentProcess(process, new IPEndPoint(IPAddress.Loopback, port));
                }
            }
        }

        private AgentProcess(Process process, IPEndPoint listeningOn)
        {
            _process = process;
            _listeningOn = listeningOn;
        }

        public void Dispose()
        {
            using (_process)
            {
                _process.StandardInput.WriteLine("stop");

                if (!_process.WaitForExit(10_000))
                {
                    throw new NUnitEngineException("The agent did not shut down within ten seconds of receiving the stop command.");
                }
            }
        }
    }
}
#endif
