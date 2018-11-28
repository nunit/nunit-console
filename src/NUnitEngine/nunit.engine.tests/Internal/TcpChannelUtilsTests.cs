// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1 && !NETCOREAPP2_0
using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using NUnit.Engine.Tests.Helpers;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NUnit.Engine.Internal.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)] // GetTcpChannel affects the whole AppDomain
    public class TcpChannelUtilsTests
    {
        [Test]
        public void GetTcpChannelReturnsSameChannelForSameNameDifferentPort()
        {
            var first = TcpChannelUtils.GetTcpChannel("test", 1234);
            using (CleanUpOnDispose(first))
            {
                var second = TcpChannelUtils.GetTcpChannel("test", 4321);
                Assert.That(second, Is.SameAs(first));
            }
        }

        [Test]
        public void GetTcpChannelReturnsSameChannelForSameNameUnspecifiedPorts()
        {
            var first = TcpChannelUtils.GetTcpChannel("test", 0);
            using (CleanUpOnDispose(first))
            {
                var second = TcpChannelUtils.GetTcpChannel("test", 0);
                Assert.That(second, Is.SameAs(first));
            }
        }

        [Test]
        public void GetTcpChannelReturnsChannelWithCorrectName()
        {
            var channel = TcpChannelUtils.GetTcpChannel("test", 1234);
            using (CleanUpOnDispose(channel))
            {
                Assert.That(channel, HasChannelName().EqualTo("test"));
            }
        }

        [Test]
        public void GetTcpChannelReturnsChannelWithCorrectNameForUnspecifiedPort()
        {
            var channel = TcpChannelUtils.GetTcpChannel("test", 0);
            using (CleanUpOnDispose(channel))
            {
                Assert.That(channel, HasChannelName().EqualTo("test"));
            }
        }

        [Test]
        public void GetTcpChannelReturnsChannelWithCorrectURI()
        {
            var channel = TcpChannelUtils.GetTcpChannel("test", 1234);
            using (CleanUpOnDispose(channel))
            {
                Assert.That(channel, HasChannelUris().EqualTo(new[] { "tcp://127.0.0.1:1234" }));
            }
        }


        private static ConstraintExpression HasChannelName() =>
            Has.Property(nameof(TcpChannel.ChannelName));

        private static ConstraintExpression HasChannelUris() =>
            Has.Property(nameof(TcpChannel.ChannelData))
            .With.Property(nameof(ChannelDataStore.ChannelUris));

        private static IDisposable CleanUpOnDispose(IChannelReceiver channel) => On.Dispose(() =>
        {
            channel.StopListening(null);
            ChannelServices.UnregisterChannel(channel);
        });
    }
}
#endif