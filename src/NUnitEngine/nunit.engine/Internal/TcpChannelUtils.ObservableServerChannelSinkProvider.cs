// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
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