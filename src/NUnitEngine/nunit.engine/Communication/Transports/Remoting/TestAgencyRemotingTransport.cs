// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Engine.Communication.Transports.Remoting
{
    /// <summary>
    /// TestAgencyRemotingTransport uses the remoting to connect a
    /// TestAgency with its agents.
    /// </summary>
    public class TestAgencyRemotingTransport : MarshalByRefObject, ITestAgencyTransport, ITestAgency, IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgencyRemotingTransport));

        private ITestAgency _agency;
        private string _uri;
        private int _port;

        private TcpChannel? _channel;
        private bool _isMarshalled;

        private object _theLock = new object();

        public TestAgencyRemotingTransport(ITestAgency agency, string uri, int port)
        {
            Guard.ArgumentNotNull(agency, nameof(agency));
            Guard.ArgumentNotNullOrEmpty(uri, nameof(uri));

            _agency = agency;
            _uri = uri;
            _port = port;
        }

        public string ServerUrl => string.Format("tcp://127.0.0.1:{0}/{1}", _port, _uri);

        public bool Start()
        {
            lock (_theLock)
            {
                _channel = TcpChannelUtils.GetTcpChannel(_uri + "Channel", _port, 100);

                RemotingServices.Marshal(this, _uri);
                _isMarshalled = true;
            }

            if (_port == 0)
            {
                ChannelDataStore? store = _channel!.ChannelData as ChannelDataStore;
                if (store != null)
                {
                    string channelUri = store.ChannelUris[0];
                    _port = int.Parse(channelUri.Substring(channelUri.LastIndexOf(':') + 1));
                }
            }

            return true;
        }

        [System.Runtime.Remoting.Messaging.OneWay]
        public void Stop()
        {
            lock( _theLock )
            {
                if ( this._isMarshalled )
                {
                    RemotingServices.Disconnect( this );
                    this._isMarshalled = false;
                }

                if ( this._channel != null )
                {
                    try
                    {
                        ChannelServices.UnregisterChannel(this._channel);
                        this._channel = null;
                    }
                    catch (RemotingException)
                    {
                        // Mono 4.4 appears to unregister the channel itself
                        // so don't do anything here.
                    }
                }

                Monitor.PulseAll( _theLock );
            }
        }

        public void Register(ITestAgent agent)
        {
            _agency.Register(agent);
        }

        public void WaitForStop()
        {
            lock( _theLock )
            {
                Monitor.Wait( _theLock );
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

        /// <summary>
        /// Overridden to cause object to live indefinitely
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null!;
        }
    }
}
#endif
