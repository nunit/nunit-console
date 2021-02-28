// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace NUnit.Engine.Internal
{
    partial class TcpChannelUtils
    {
        private sealed class ObservableServerChannelSinkProvider : IServerChannelSinkProvider
        {
            private readonly CurrentMessageCounter _currentMessageCounter;

            public ObservableServerChannelSinkProvider(CurrentMessageCounter currentMessageCounter)
            {
                if (currentMessageCounter == null) throw new ArgumentNullException(nameof(currentMessageCounter));
                _currentMessageCounter = currentMessageCounter;
            }

            public void GetChannelData(IChannelDataStore channelData)
            {
            }

            public IServerChannelSink CreateSink(IChannelReceiver channel)
            {
                if (Next == null)
                    throw new InvalidOperationException("Cannot create a sink without setting the next provider.");
                return new ObservableServerChannelSink(_currentMessageCounter, Next.CreateSink(channel));
            }

            public IServerChannelSinkProvider Next { get; set; }


            private sealed class ObservableServerChannelSink : IServerChannelSink
            {
                private readonly IServerChannelSink _next;
                private readonly CurrentMessageCounter _currentMessageCounter;

                public ObservableServerChannelSink(CurrentMessageCounter currentMessageCounter, IServerChannelSink next)
                {
                    if (next == null) throw new ArgumentNullException(nameof(next));
                    _currentMessageCounter = currentMessageCounter;
                    _next = next;
                }

                public IDictionary Properties => _next.Properties;

                public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack, IMessage requestMsg,
                    ITransportHeaders requestHeaders, Stream requestStream, out IMessage responseMsg,
                    out ITransportHeaders responseHeaders, out Stream responseStream)
                {
                    _currentMessageCounter.OnMessageStart();
                    var isAsync = false;
                    try
                    {
                        var processing = _next.ProcessMessage(sinkStack, requestMsg, requestHeaders, requestStream,
                            out responseMsg, out responseHeaders, out responseStream);
                        isAsync = processing == ServerProcessing.Async;
                        return processing;
                    }
                    finally
                    {
                        if (!isAsync) _currentMessageCounter.OnMessageEnd();
                    }
                }

                public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg,
                    ITransportHeaders headers, Stream stream)
                {
                    try
                    {
                        _next.AsyncProcessResponse(sinkStack, state, msg, headers, stream);
                    }
                    finally
                    {
                        _currentMessageCounter.OnMessageEnd();
                    }
                }

                public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack, object state, IMessage msg,
                    ITransportHeaders headers)
                {
                    return _next.GetResponseStream(sinkStack, state, msg, headers);
                }

                public IServerChannelSink NextChannelSink => _next.NextChannelSink;
            }
        }
    }
}
#endif