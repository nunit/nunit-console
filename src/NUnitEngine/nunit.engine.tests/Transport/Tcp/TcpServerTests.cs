// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    public class TcpServerTests
    {
        private TcpServer _server;
        private List<Socket> _serverConnections;
        private object _lock = new object();

        [SetUp]
        public void StartServer()
        {
            _serverConnections = new List<Socket>();
            _server = new TcpServer();
            _server.ClientConnected += (c, g) =>
            {
                lock (_lock)
                {
                    _serverConnections.Add(c);
                }
            };

            _server.Start();
        }

        [TearDown]
        public void StopServer()
        {
            _server.Stop();
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        [TestCase(20)]
        public void ClientConnectionTest(int numClients)
        {
            var clients = new TcpClient[numClients];
            var workers = new BackgroundWorker[numClients];

            var allTasksComplete = new ManualResetEvent(false);
            int pendingTasks = numClients;

            for (int i = 0; i < numClients; i++)
            {
                var client = new TcpClient();
                clients[i] = client;

                var worker = new BackgroundWorker();

                worker.DoWork += (s, e) =>
                {
                    client.Connect(_server.EndPoint);
                    client.Client.Send(new Guid().ToByteArray());
                };

                worker.RunWorkerCompleted += (s, e) =>
                {
                    if (Interlocked.Decrement(ref pendingTasks) == 0)
                        allTasksComplete.Set();
                };

                workers[i] = worker;
            }

            foreach (var worker in workers)
                worker.RunWorkerAsync();

            allTasksComplete.WaitOne();

            foreach (var worker in workers)
                worker.Dispose();

            Thread.Sleep(1); // Allow everything to complete

            try
            {
                Assert.That(_serverConnections.Count, Is.EqualTo(numClients), $"Should have received {numClients} connection events");

                for (int i = 0; i < numClients; i++)
                {
                    Assert.That(_serverConnections[i].Connected, $"Server is not connected to client {i + 1}");
                    Assert.That(clients[i].Connected, Is.True, $"Client {i + 1} is not connected to server");
                }
            }
            finally
            {
                foreach (var client in clients)
                    client.Close();
            }
        }
    }
}
