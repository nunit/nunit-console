// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Exercises provider callbacks during return using only registered substitutes.
    /// No remote implementation, process or agent-exit reporting is required.
    /// </summary>
    public class TestAgentServiceReturnCallbackTests
    {
        /// <summary>
        /// A callback must be able to observe a rejected overlapping return before
        /// the original return completes, including when that return subsequently fails.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void OverlappingReturnIsRejectedWithoutBlockingProviderCallback(bool failFirstReturn)
        {
            var agent = Substitute.For<ITestAgent>();
            agent.Id.Returns(Guid.NewGuid());
            var provider = Substitute.For<ITestAgentProvider, IService>();
            provider.IsAgentAvailable(Arg.Any<TestPackage>()).Returns(true);
            provider.GetAgent(Arg.Any<TestPackage>()).Returns(agent);
            var services = new ServiceContext();
            services.Add((IService)provider);
            using var agency = new TestAgentService();
            services.Add(agency);
            services.ServiceManager.StartServices();
            Task<Exception?>? overlapping = null;
            bool completedInsideCallback = false;
            int returns = 0;
            try
            {
                var package = new TestPackage("offline.dll");
                package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), true);
                agency.GetAgent(package);
                provider.When(value => value.ReturnAgent(agent)).Do(_ =>
                {
                    if (Interlocked.Increment(ref returns) != 1)
                        return;
                    overlapping = Task.Run(() => Capture(() => agency.ReturnAgent(agent)));
                    completedInsideCallback = overlapping.Wait(TimeSpan.FromSeconds(5));
                    if (failFirstReturn)
                        throw new IOException("Controlled provider return failure");
                });

                Exception? firstFailure = Capture(() => agency.ReturnAgent(agent));

                Assert.That(overlapping, Is.Not.Null);
                Assert.That(overlapping!.Wait(TimeSpan.FromSeconds(5)), Is.True,
                    "The overlapping return must finish before the fixture releases its services.");
                Assert.That(completedInsideCallback, Is.True,
                    "Provider callbacks must not wait on a coordinator ownership lock.");
                Assert.That(overlapping.Result, Is.TypeOf<InvalidOperationException>());
                provider.Received(1).ReturnAgent(agent);
                if (failFirstReturn)
                {
                    Assert.That(firstFailure, Is.TypeOf<IOException>());
                    Assert.That(agency.IsAgentActive(agent.Id), Is.True);
                    agency.ReturnAgent(agent);
                    provider.Received(2).ReturnAgent(agent);
                }
                else
                    Assert.That(firstFailure, Is.Null);
                Assert.That(agency.IsAgentActive(agent.Id), Is.False);
            }
            finally
            {
                // The old locking implementation completes only after the callback
                // returns; join that bounded control even when an assertion fails.
                overlapping?.Wait(TimeSpan.FromSeconds(5));
                services.ServiceManager.Dispose();
            }
        }

        private static Exception? Capture(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
