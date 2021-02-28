// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using NUnit.Common;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// A collection of utility methods used to create, retrieve
    /// and release <see cref="TcpChannel"/>s.
    /// </summary>
    public static partial class TcpChannelUtils
    {
        private static readonly Logger Log = InternalTrace.GetLogger(typeof(TcpChannelUtils));

        /// <summary>
        /// Create a <see cref="TcpChannel"/> with a given name on a given port.
        /// </summary>
        /// <param name="name">The name of the channel to create.</param>
        /// <param name="port">The port number of the channel to create.</param>
        /// <param name="limit">The rate limit of the channel to create.</param>
        /// <param name="currentMessageCounter">An optional counter to provide the ability to wait for all current messages.</param>
        /// <returns>A <see cref="TcpChannel"/> configured with the given name and port.</returns>
        private static TcpChannel CreateTcpChannel(string name, int port, int limit, CurrentMessageCounter currentMessageCounter = null)
        {
            var props = new Dictionary<string, object>
            {
                { "port", port },
                { "name", name },
                { "bindTo", "127.0.0.1" },
                //{ "clientConnectionLimit", limit }
            };

            var serverProvider = new BinaryServerFormatterSinkProvider
            {
                TypeFilterLevel = TypeFilterLevel.Full
            };

            var clientProvider = new BinaryClientFormatterSinkProvider();

            return new TcpChannel(
                props,
                clientProvider,
                currentMessageCounter != null
                    ? new ObservableServerChannelSinkProvider(currentMessageCounter) { Next = serverProvider }
                    : (IServerChannelSinkProvider)serverProvider);
        }

        /// <summary>
        /// Get a default channel. If one does not exist, then one is created and registered.
        /// </summary>
        /// <param name="currentMessageCounter">An optional counter to provide the ability to wait for all current messages.</param>
        /// <returns>The specified <see cref="TcpChannel"/> or <see langword="null"/> if it cannot be found and created.</returns>
        public static TcpChannel GetTcpChannel(CurrentMessageCounter currentMessageCounter = null)
        {
            return GetTcpChannel("", 0, 2, currentMessageCounter);
        }

        /// <summary>
        /// Get a channel by name, casting it to a <see cref="TcpChannel"/>.
        /// Otherwise, create, register and return a <see cref="TcpChannel"/> with
        /// that name, on the port provided as the second argument.
        /// </summary>
        /// <param name="name">The name of the channel.</param>
        /// <param name="port">The port to use if the channel must be created.</param>
        /// <param name="currentMessageCounter">An optional counter to provide the ability to wait for all current messages.</param>
        /// <returns>The specified <see cref="TcpChannel"/> or <see langword="null"/> if it cannot be found and created.</returns>
        public static TcpChannel GetTcpChannel(string name, int port, CurrentMessageCounter currentMessageCounter = null)
        {
            return GetTcpChannel(name, port, 2, currentMessageCounter);
        }

        /// <summary>
        /// Get a channel by name, casting it to a <see cref="TcpChannel"/>.
        /// Otherwise, create, register and return a <see cref="TcpChannel"/> with
        /// that name, on the port provided as the second argument.
        /// </summary>
        /// <param name="name">The name of the channel</param>
        /// <param name="port">The port to use if the channel must be created.</param>
        /// <param name="limit">The client connection limit or negative for the default.</param>
        /// <param name="currentMessageCounter">An optional counter to provide the ability to wait for all current messages.</param>
        /// <returns>The specified <see cref="TcpChannel"/> or <see langword="null"/> if it cannot be found and created.</returns>
        public static TcpChannel GetTcpChannel(string name, int port, int limit, CurrentMessageCounter currentMessageCounter = null)
        {
            var existingChannel = ChannelServices.GetChannel(name) as TcpChannel;
            if (existingChannel != null) return existingChannel;

            // NOTE: Retries are normally only needed when rapidly creating
            // and destroying channels, as in running the NUnit tests.
            for (var retries = 0; retries < 10; retries++)
                try
                {
                    var newChannel = CreateTcpChannel(name, port, limit, currentMessageCounter);
                    ChannelServices.RegisterChannel(newChannel, false);
                    return newChannel;
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to create/register channel." + Environment.NewLine + ExceptionHelper.BuildMessageAndStackTrace(ex));
                    Thread.Sleep(300);
                }

            return null;
        }
    }
}
#endif