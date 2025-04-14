// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Net;
using System.Threading;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    /// <summary>
    /// TestAgencyTcpTransport uses the TCP protocol to connect a
    /// TestAgency with its agents.
    /// </summary>
    public class TestAgencyTcpTransport : ITestAgencyTransport, ITestAgency, IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgencyTcpTransport));

        private ITestAgency _agency;
        private TcpServer _server;

        private object _theLock = new object();

        public TestAgencyTcpTransport(ITestAgency agency, int port = 0)
        {
            Guard.ArgumentNotNull(agency);
            Guard.ArgumentValid(port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort,
                $"Port number {port} is invalid. Must be a positive integer less than or equal to {IPEndPoint.MaxPort}", nameof(port));

            _agency = agency;
            _server = new TcpServer(port);
        }

        public string ServerUrl => _server.EndPoint.ToString();

        public bool Start()
        {
            _server.ClientConnected += (socket, id) => _agency.Register(new TestAgentTcpProxy(socket, id));
            _server.Start();

            return true;
        }

        public void Stop()
        {
            _server.Stop();
        }

        public void Register(ITestAgent agent)
        {
            _agency.Register(agent);
        }

        public void WaitForStop()
        {
            lock (_theLock)
            {
                Monitor.Wait(_theLock);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Stop();

                _disposed = true;
            }
        }
    }
}
