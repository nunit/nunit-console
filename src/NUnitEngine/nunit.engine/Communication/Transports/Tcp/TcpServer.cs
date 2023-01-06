// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    public class TcpServer
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TcpServer));

        private const int GUID_BUFFER_SIZE = 16;

        TcpListener _tcpListener;
        Thread _listenerThread;
        volatile bool _running;

        public delegate void ConnectionEventHandler(Socket clientSocket, Guid id);

        public event ConnectionEventHandler ClientConnected;

        public TcpServer(int port = 0)
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, port);
        }

        public IPEndPoint EndPoint => (IPEndPoint)_tcpListener.LocalEndpoint;

        public void Start()
        {
            _tcpListener.Start();
            _running = true;

            _listenerThread = new Thread(WaitForClientConnections)
            {
                Name = "TCPListenerThread",
                IsBackground = true
            };
            _listenerThread.Start();
        }

        public void Stop()
        {
            try
            {
                _running = false;
                _tcpListener.Stop();
            }
            catch (Exception exception)
            {
                log.Error($"Failed to stop listener socket: {exception}");
            }
        }

        private void WaitForClientConnections()
        {
            while (_running)
            {
                try
                {
                    var clientSocket = _tcpListener.AcceptSocket();
                    if (clientSocket.Connected)
                    {
                        // Upon connection, remote agent must immediately send its Id as identification.
                        // Guid is sent as a raw byte array, without any preceding length specified.
                        byte[] bytes = ReadBytes(clientSocket, GUID_BUFFER_SIZE);

                        Guid id = new Guid(bytes);
                        ClientConnected?.Invoke(clientSocket, id);
                    }
                }
                catch
                {
                    // Two possibilities:
                    //   1. We were trying to stop the socket
                    //   2. The connection was dropped due to some external event
                    // In either case, we stop the socket and wait a while
                    _tcpListener.Stop();

                    // If we were trying to stop, that's all
                    if (!_running)
                        return;

                    // Otherwise, wait and try to restart it. An exception here is simply logged
                    Thread.Sleep(500);
                    try
                    {
                        _tcpListener.Start();
                    }
                    catch (Exception exception)
                    {
                        log.Error($"Unable to restart listener socket: {exception}");
                    }
                }
            }
        }

        private static byte[] ReadBytes(Socket clientSocket, int numBytes)
        {
            var buf = new byte[numBytes];
            var bytes = new byte[numBytes];
            int count = 0;
            while (count < numBytes)
            {
                int n = clientSocket.Receive(buf);
                Array.Copy(buf, 0, bytes, count, n);
                count += n;
            }

            return bytes;
        }
    }
}
