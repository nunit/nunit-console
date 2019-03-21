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

#if !NETSTANDARD1_6 // Dependency on AgentServerConnection
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NUnit.Engine.Agent
{
    public sealed class AgentServer : IDisposable
    {
        private readonly Socket _listeningSocket;
        private readonly ITestRunnerFactory _testRunnerFactory;
        private volatile bool isDisposed;

        public IPEndPoint ListeningOn => (IPEndPoint)_listeningSocket.LocalEndPoint;

        public static AgentServer Start(IPAddress bindTo, ITestRunnerFactory testRunnerFactory)
        {
            if (bindTo is null) throw new ArgumentNullException(nameof(bindTo));
            if (testRunnerFactory is null) throw new ArgumentNullException(nameof(testRunnerFactory));

            var listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(bindTo, port: 0));

            const int maxWaitingConnections = 10;
            listeningSocket.Listen(backlog: maxWaitingConnections);

            return new AgentServer(listeningSocket, testRunnerFactory);
        }

        private AgentServer(Socket listeningSocket, ITestRunnerFactory testRunnerFactory)
        {
            _listeningSocket = listeningSocket;
            _testRunnerFactory = testRunnerFactory;

            SubscribeToNextConnection();
        }

        public void Dispose()
        {
            isDisposed = true;

#if NET20
            _listeningSocket.Close();
#else
            _listeningSocket.Dispose();
#endif
        }

        private void SubscribeToNextConnection()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += OnConnectionAccepted;
            _listeningSocket.AcceptAsync(args);
        }

        private void OnConnectionAccepted(object sender, SocketAsyncEventArgs e)
        {
            if (isDisposed) return;

            SubscribeToNextConnection();

            ThreadPool.QueueUserWorkItem(RunConnectionSynchronously, state: e.AcceptSocket);
        }

        // Ideally this would be async so as not to block a thread, but we support .NET Framework versions earlier than 4.5.
        private void RunConnectionSynchronously(object state)
        {
            using (var stream = new NetworkStream((Socket)state, ownsSocket: true))
            using (var connection = new AgentServerConnection(stream))
            {
                connection.Connect(_testRunnerFactory);
                connection.Run();
            }
        }
    }
}
#endif
