// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
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